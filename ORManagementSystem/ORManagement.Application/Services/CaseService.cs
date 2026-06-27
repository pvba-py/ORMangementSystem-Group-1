using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Engines;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;
using System.Net;

namespace ORManagement.Application.Services;

public class CaseService : ICaseService
{
    private readonly ICaseRepository _caseRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly AvailabilityWindowEngine _availabilityWindowEngine;
    private readonly ILogger<CaseService> _logger;

    private static readonly HashSet<string> AllowedStatuses = new()
    {
        "Scheduled", "InProgress", "Completed", "Cancelled"
    };

    private static readonly HashSet<string> AllowedCancellationReasons = new()
    {
        "SurgeonCancelled",
        "PatientNoShow",
        "PatientNotCleared",
        "EmergencyBump",
        "Other"
    };

    public CaseService(
        ICaseRepository caseRepository,
        IAuditRepository auditRepository,
        AvailabilityWindowEngine availabilityWindowEngine,
        ILogger<CaseService> logger)
    {
        _caseRepository = caseRepository;
        _auditRepository = auditRepository;
        _availabilityWindowEngine = availabilityWindowEngine;
        _logger = logger;
    }
    private static ServiceResultDto ValidateBlockTypeForRequest(
    BlockBoundaryDto block,
    int requestSurgeonId,
    string requestPriority)
    {
        if (block.BlockType == "Recurring")
        {
            if (block.SurgeonId != requestSurgeonId)
            {
                return ServiceResultDto.Fail(
                    "BLOCK_SURGEON_MISMATCH",
                    "Recurring block can only be used for the assigned surgeon.");
            }
        }

        if (block.BlockType == "Emergency")
        {
            if (requestPriority != "Emergency")
            {
                return ServiceResultDto.Fail(
                    "EMERGENCY_BLOCK_REQUIRED_PRIORITY",
                    "Emergency blocks can only be used for emergency requests.");
            }
        }

        if (block.BlockType == "AdHoc")
        {
            if (block.SurgeonId.HasValue &&
                block.SurgeonId.Value != requestSurgeonId)
            {
                return ServiceResultDto.Fail(
                    "BLOCK_SURGEON_MISMATCH",
                    "This ad-hoc block is assigned to a different surgeon.");
            }
        }

        // Open blocks allow any approved/modified request if normal validations pass.
        return ServiceResultDto.Ok();
    }
    public async Task<ServiceResultDto<List<SurgicalCaseDto>>> GetCasesAsync(
    int hospitalId,
    int userId,
    string roleName,
    string? status,
    string? ipAddress,
    string? userAgent)
    {
        var cases = await _caseRepository.GetCasesAsync(hospitalId, status);
        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CaseListViewed",
            Entity = "SurgicalCases",
            EntityId = null,
            NewValue = cases.Count.ToString(),
            Remarks = $"Surgical case list viewed. Returned {cases.Count} records.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<List<SurgicalCaseDto>>.Ok(cases);
    }

    public async Task<ServiceResultDto<List<SurgicalCaseDto>>> GetMyCasesAsync(
    int hospitalId,
    int surgeonId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent)
    {
        var cases = await _caseRepository.GetMyCasesAsync(hospitalId, surgeonId);
        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "MyCaseListViewed",
            Entity = "SurgicalCases",
            EntityId = null,
            NewValue = cases.Count.ToString(),
            Remarks = $"Surgeon case list viewed. Returned {cases.Count} records.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });
        return ServiceResultDto<List<SurgicalCaseDto>>.Ok(cases);
    }

    public async Task<ServiceResultDto<SurgicalCaseDto>> GetCaseByIdAsync(
        int hospitalId,
        int surgeryId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var surgicalCase = await _caseRepository.GetCaseByIdAsync(hospitalId, surgeryId);

        if (surgicalCase is null)
        {
            return ServiceResultDto<SurgicalCaseDto>.Fail("CASE_NOT_FOUND", "Surgical case was not found.");
        }

        await _auditRepository.AddPhiAccessLogAsync(new CreatePhiAccessLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            PatientId = surgicalCase.PatientId,
            AccessType = "View",
            Context = $"Surgical case {surgeryId} viewed.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<SurgicalCaseDto>.Ok(surgicalCase);
    }

    public async Task<ServiceResultDto<int>> CreateCaseAsync(
    int hospitalId,
    int userId,
    string roleName,
    CreateCaseRequestDto request,
    string? ipAddress,
    string? userAgent)
    {
        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_CASE_TIME",
                "Scheduled end must be after scheduled start.");
        }

        var orRequest = await _caseRepository.GetRequestForSchedulingAsync(
            hospitalId,
            request.RequestId);

        if (orRequest is null)
        {
            return ServiceResultDto<int>.Fail(
                "REQUEST_NOT_FOUND",
                "Request was not found.");
        }

        if (orRequest.RequestStatus != "Approved" &&
            orRequest.RequestStatus != "Modified")
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_REQUEST_STATUS",
                "Only approved or modified requests can be scheduled.");
        }
        var surgeonConflictExists = await _caseRepository.SurgeonCaseConflictExistsAsync(
    hospitalId,
    orRequest.SurgeonId,
    request.ScheduledStart,
    request.ScheduledEnd);

        if (surgeonConflictExists)
        {
            return ServiceResultDto<int>.Fail(
                "SURGEON_CASE_CONFLICT",
                "This surgeon already has another case scheduled during the selected time.");
        }
        var durationValidation = ValidateScheduledDuration(
    request.ScheduledStart,
    request.ScheduledEnd,
    orRequest.EstimatedDurationMin);

        if (!durationValidation.Success)
        {
            return ServiceResultDto<int>.Fail(
                durationValidation.ErrorCode!,
                durationValidation.Message!);
        }
        if (!_availabilityWindowEngine.IsDateAllowed(
                request.ScheduledStart.Date,
                orRequest.AvailableDaysMask))
        {
            return ServiceResultDto<int>.Fail(
                "DATE_OUTSIDE_AVAILABILITY",
                "Scheduled date is outside surgeon availability.");
        }

        var blockBoundary = await _caseRepository.GetBlockBoundaryAsync(
            hospitalId,
            request.BlockId);

        if (blockBoundary is null)
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_BLOCK",
                "Selected block was not found.");
        }

        var boundaryValidation = ValidateCaseWithinBlock(
            blockBoundary,
            request.ScheduledStart,
            request.ScheduledEnd);

        if (!boundaryValidation.Success)
        {
            return ServiceResultDto<int>.Fail(
                boundaryValidation.ErrorCode!,
                boundaryValidation.Message!);
        }

        var blockTypeValidation = ValidateBlockTypeForRequest(
            blockBoundary,
            orRequest.SurgeonId,
            orRequest.Priority);

        if (!blockTypeValidation.Success)
        {
            return ServiceResultDto<int>.Fail(
                blockTypeValidation.ErrorCode!,
                blockTypeValidation.Message!);
        }

        var conflictExists = await _caseRepository.HasCaseConflictAsync(
            hospitalId,
            request.BlockId,
            request.ScheduledStart,
            request.ScheduledEnd);

        if (conflictExists)
        {
            return ServiceResultDto<int>.Fail(
                "CASE_CONFLICT",
                "Selected time conflicts with another surgical case.");
        }

        var roomId = await _caseRepository.GetBlockRoomIdAsync(
            hospitalId,
            request.BlockId);

        if (roomId is null)
        {
            return ServiceResultDto<int>.Fail(
                "ROOM_NOT_FOUND",
                "Room was not found for selected block.");
        }

        var surgeryId = await _caseRepository.CreateCaseAsync(
            hospitalId,
            request.RequestId,
            request.BlockId,
            orRequest.SurgeonId,
            roomId.Value,
            request.ScheduledStart,
            request.ScheduledEnd,
            userId);

        await _caseRepository.MarkRequestScheduledAsync(
            hospitalId,
            request.RequestId,
            userId);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CaseScheduled",
            Entity = "SurgicalCases",
            EntityId = surgeryId,
            NewValue = "Scheduled",
            Remarks = $"Request {request.RequestId} scheduled into block {request.BlockId}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Surgical case created. SurgeryId: {SurgeryId}, RequestId: {RequestId}",
            surgeryId,
            request.RequestId);

        return ServiceResultDto<int>.Ok(
            surgeryId,
            "Surgical case created successfully.");
    }

    private static ServiceResultDto ValidateScheduledDuration(
    DateTime scheduledStart,
    DateTime scheduledEnd,
    int estimatedDurationMin)
    {
        var scheduledDurationMin = (int)Math.Round(
            (scheduledEnd - scheduledStart).TotalMinutes);

        if (scheduledDurationMin < estimatedDurationMin)
        {
            return ServiceResultDto.Fail(
                "CASE_DURATION_TOO_SHORT",
                $"Scheduled duration is {scheduledDurationMin} minutes, but request estimated duration is {estimatedDurationMin} minutes.");
        }

        return ServiceResultDto.Ok();
    }


    public async Task<ServiceResultDto> UpdateCaseStatusAsync(
        int hospitalId,
        int surgeryId,
        int userId,
        string roleName,
        UpdateCaseStatusDto request,
        string? ipAddress,
        string? userAgent)
    {
        var status = request.Status.Trim();

        if (!AllowedStatuses.Contains(status))
        {
            return ServiceResultDto.Fail("INVALID_CASE_STATUS", "Invalid case status.");
        }

        var existingCase = await _caseRepository.GetCaseByIdAsync(hospitalId, surgeryId);

        if (existingCase is null)
        {
            return ServiceResultDto.Fail("CASE_NOT_FOUND", "Surgical case was not found.");
        }

        var transitionValidation = ValidateStatusTransition(existingCase.CaseStatus, status, request);

        if (!transitionValidation.Success)
        {
            return transitionValidation;
        }
        if (status == "Completed" && request.ActualEnd is null)
        {
            return ServiceResultDto.Fail(
                "ACTUAL_END_REQUIRED",
                "Actual end time is required when completing a case.");
        }
        var updated = await _caseRepository.UpdateCaseStatusAsync(
            hospitalId,
            surgeryId,
            status,
            request.ActualStart,
            request.ActualEnd,
            request.CancellationReason,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail("CASE_STATUS_UPDATE_FAILED", "Case status could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = $"Case{status}",
            Entity = "SurgicalCases",
            EntityId = surgeryId,
            OldValue = existingCase.CaseStatus,
            NewValue = status,
            Remarks = status == "Cancelled" ? request.CancellationReason : null,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok($"Case status updated to {status}.");
    }
    public async Task<ServiceResultDto> UpdateCaseAsync(
     int hospitalId,
     int surgeryId,
     int userId,
     string roleName,
     UpdateCaseRequestDto request,
     string? ipAddress,
     string? userAgent)
    {
        if (request.ScheduledEnd <= request.ScheduledStart)
        {
            return ServiceResultDto.Fail(
                "INVALID_CASE_TIME",
                "Scheduled end must be after scheduled start.");
        }

        var existingCase = await _caseRepository.GetCaseByIdAsync(
            hospitalId,
            surgeryId);

        if (existingCase is null)
        {
            return ServiceResultDto.Fail(
                "CASE_NOT_FOUND",
                "Surgical case was not found.");
        }

        var orRequest = await _caseRepository.GetRequestForSchedulingAsync(
            hospitalId,
            existingCase.RequestId);

        if (orRequest is null)
        {
            return ServiceResultDto.Fail(
                "REQUEST_NOT_FOUND",
                "Related OR request was not found.");
        }

        var durationValidation = ValidateScheduledDuration(
            request.ScheduledStart,
            request.ScheduledEnd,
            orRequest.EstimatedDurationMin);

        if (!durationValidation.Success)
        {
            return durationValidation;
        }

        var surgeonConflictExists = await _caseRepository.SurgeonCaseConflictExistsAsync(
            hospitalId,
            orRequest.SurgeonId,
            request.ScheduledStart,
            request.ScheduledEnd,
            surgeryId);

        if (surgeonConflictExists)
        {
            return ServiceResultDto.Fail(
                "SURGEON_CASE_CONFLICT",
                "This surgeon already has another case scheduled during the selected time.");
        }

        var blockBoundary = await _caseRepository.GetBlockBoundaryAsync(
            hospitalId,
            existingCase.BlockId);

        if (blockBoundary is null)
        {
            return ServiceResultDto.Fail(
                "INVALID_BLOCK",
                "Selected block was not found.");
        }

        var boundaryValidation = ValidateCaseWithinBlock(
            blockBoundary,
            request.ScheduledStart,
            request.ScheduledEnd);

        if (!boundaryValidation.Success)
        {
            return boundaryValidation;
        }

        var blockTypeValidation = ValidateBlockTypeForRequest(
            blockBoundary,
            orRequest.SurgeonId,
            orRequest.Priority);

        if (!blockTypeValidation.Success)
        {
            return blockTypeValidation;
        }

        if (!_availabilityWindowEngine.IsDateAllowed(
                request.ScheduledStart.Date,
                orRequest.AvailableDaysMask))
        {
            return ServiceResultDto.Fail(
                "DATE_OUTSIDE_AVAILABILITY",
                "Scheduled date is outside surgeon availability.");
        }

        var conflictExists = await _caseRepository.HasCaseConflictAsync(
            hospitalId,
            existingCase.BlockId,
            request.ScheduledStart,
            request.ScheduledEnd,
            surgeryId);

        if (conflictExists)
        {
            return ServiceResultDto.Fail(
                "CASE_CONFLICT",
                "Updated time conflicts with another surgical case in the selected block.");
        }

        var updated = await _caseRepository.UpdateCaseAsync(
            hospitalId,
            surgeryId,
            request.ScheduledStart,
            request.ScheduledEnd,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail(
                "CASE_UPDATE_FAILED",
                "Only scheduled cases can be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CaseUpdated",
            Entity = "SurgicalCases",
            EntityId = surgeryId,
            OldValue = $"{existingCase.ScheduledStart} - {existingCase.ScheduledEnd}",
            NewValue = $"{request.ScheduledStart} - {request.ScheduledEnd}",
            Remarks = "Surgical case schedule updated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Surgical case updated successfully.");
    }

    private ServiceResultDto ValidateStatusTransition(
        string currentStatus,
        string newStatus,
        UpdateCaseStatusDto request)
    {
        if (currentStatus == "Completed" || currentStatus == "Cancelled")
        {
            return ServiceResultDto.Fail("INVALID_CASE_TRANSITION", "Completed or cancelled cases cannot be changed.");
        }

        if (newStatus == "InProgress" && currentStatus != "Scheduled")
        {
            return ServiceResultDto.Fail("INVALID_CASE_TRANSITION", "Only scheduled cases can be started.");
        }

        if (newStatus == "Completed" && currentStatus != "InProgress")
        {
            return ServiceResultDto.Fail("INVALID_CASE_TRANSITION", "Only in-progress cases can be completed.");
        }

        if (newStatus == "Cancelled")
        {
            if (string.IsNullOrWhiteSpace(request.CancellationReason))
            {
                return ServiceResultDto.Fail("CANCELLATION_REASON_REQUIRED", "Cancellation reason is required.");
            }

            if (!AllowedCancellationReasons.Contains(request.CancellationReason))
            {
                return ServiceResultDto.Fail("INVALID_CANCELLATION_REASON", "Invalid cancellation reason.");
            }
        }

        return ServiceResultDto.Ok();
    }
    private static ServiceResultDto ValidateCaseWithinBlock(
    BlockBoundaryDto block,
    DateTime scheduledStart,
    DateTime scheduledEnd)
    {
        if (scheduledEnd <= scheduledStart)
        {
            return ServiceResultDto.Fail(
                "INVALID_CASE_TIME",
                "Scheduled end must be after scheduled start.");
        }

        if (scheduledStart.Date != block.BlockDate.Date ||
            scheduledEnd.Date != block.BlockDate.Date)
        {
            return ServiceResultDto.Fail(
                "CASE_OUTSIDE_BLOCK",
                "Scheduled case date must match the selected block date.");
        }

        var scheduledStartTime = TimeOnly.FromDateTime(scheduledStart);
        var scheduledEndTime = TimeOnly.FromDateTime(scheduledEnd);

        if (scheduledStartTime < block.StartTime || scheduledEndTime > block.EndTime)
        {
            return ServiceResultDto.Fail(
                "CASE_OUTSIDE_BLOCK",
                "Scheduled case time must be within the selected block time.");
        }

        return ServiceResultDto.Ok();
    }

}