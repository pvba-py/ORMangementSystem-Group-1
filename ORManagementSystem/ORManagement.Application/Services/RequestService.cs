using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Engines;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;
using System.Net;

namespace ORManagement.Application.Services;

public class RequestService : IRequestService
{
    private readonly IRequestRepository _requestRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly AvailabilityWindowEngine _availabilityWindowEngine;
    private readonly PriorityScoreEngine _priorityScoreEngine;
    private readonly ILogger<RequestService> _logger;

    private static readonly HashSet<string> AllowedQuarters = new()
    {
        "Q1", "Q2", "Flexible"
    };

    private static readonly HashSet<string> AllowedPriorities = new()
    {
        "Emergency", "Urgent", "Elective"
    };

    private static readonly HashSet<string> AllowedReadiness = new()
    {
        "Ready", "PendingClearance", "NotReady"
    };

    private static readonly HashSet<string> AllowedStatuses = new()
    {
        "Approved", "Rejected", "Modified", "Waitlisted", "Scheduled"
    };

    public RequestService(
        IRequestRepository requestRepository,
        IAuditRepository auditRepository,
        AvailabilityWindowEngine availabilityWindowEngine,
        PriorityScoreEngine priorityScoreEngine,
        ILogger<RequestService> logger)
    {
        _requestRepository = requestRepository;
        _auditRepository = auditRepository;
        _availabilityWindowEngine = availabilityWindowEngine;
        _priorityScoreEngine = priorityScoreEngine;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<OrRequestResponseDto>>> GetRequestsAsync(
    int hospitalId,
    int userId,
    string roleName,
    string? status,
    int? cycleId,
    string? ipAddress,
    string? userAgent)
    {
        var requests = await _requestRepository.GetRequestsAsync(hospitalId, status, cycleId);
        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RequestListViewed",
            Entity = "ORRequests",
            EntityId = null,
            NewValue = requests.Count.ToString(),
            Remarks = $"OR request list viewed. Returned {requests.Count} records.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        foreach (var request in requests)
        {
            request.AvailableDaysDisplay = _availabilityWindowEngine.ToDisplayText(request.AvailableDaysMask);
        }

        return ServiceResultDto<List<OrRequestResponseDto>>.Ok(requests);
    }

    public async Task<ServiceResultDto<List<OrRequestResponseDto>>> GetMyRequestsAsync(
    int hospitalId,
    int surgeonId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent)
    {
        var requests = await _requestRepository.GetMyRequestsAsync(hospitalId, surgeonId);
        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "MyRequestListViewed",
            Entity = "ORRequests",
            EntityId = null,
            NewValue = requests.Count.ToString(),
            Remarks = $"Surgeon request list viewed. Returned {requests.Count} records.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        foreach (var request in requests)
        {
            request.AvailableDaysDisplay = _availabilityWindowEngine.ToDisplayText(request.AvailableDaysMask);
        }

        return ServiceResultDto<List<OrRequestResponseDto>>.Ok(requests);
    }

    public async Task<ServiceResultDto<OrRequestResponseDto>> GetRequestByIdAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var request = await _requestRepository.GetRequestByIdAsync(hospitalId, requestId);

        if (request is null)
        {
            return ServiceResultDto<OrRequestResponseDto>.Fail("REQUEST_NOT_FOUND", "Request was not found.");
        }

        request.AvailableDaysDisplay = _availabilityWindowEngine.ToDisplayText(request.AvailableDaysMask);

        await _auditRepository.AddPhiAccessLogAsync(new CreatePhiAccessLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            PatientId = request.PatientId,
            AccessType = "View",
            Context = $"OR request {requestId} viewed.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<OrRequestResponseDto>.Ok(request);
    }

    public async Task<ServiceResultDto<int>> CreateRequestAsync(
        int hospitalId,
        int surgeonId,
        int userId,
        string roleName,
        CreateOrRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        var validation = await ValidateCreateRequestAsync(hospitalId, request);
        if (!validation.Success)
        {
            return ServiceResultDto<int>.Fail(validation.ErrorCode!, validation.Message!);
        }

        var requestId = await _requestRepository.CreateRequestAsync(hospitalId, surgeonId, request);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RequestSubmitted",
            Entity = "ORRequests",
            EntityId = requestId,
            NewValue = request.RequestStatusText(),
            Remarks = "Surgeon submitted OR time request.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("OR request created. RequestId: {RequestId}, SurgeonId: {SurgeonId}", requestId, surgeonId);

        return ServiceResultDto<int>.Ok(requestId, "Request submitted successfully.");
    }

    public async Task<ServiceResultDto> UpdateRequestAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        UpdateOrRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        var validation = ValidateUpdateRequest(request);
        if (!validation.Success)
        {
            return validation;
        }

        var existingRequest = await _requestRepository.GetRequestByIdAsync(hospitalId, requestId);

        if (existingRequest is null)
        {
            return ServiceResultDto.Fail("REQUEST_NOT_FOUND", "Request was not found.");
        }

        var updated = await _requestRepository.UpdateRequestAsync(
            hospitalId,
            requestId,
            request,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail("REQUEST_UPDATE_FAILED", "Request could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RequestUpdated",
            Entity = "ORRequests",
            EntityId = requestId,
            OldValue = existingRequest.RequestStatus,
            NewValue = "Updated",
            Remarks = "OR request updated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Request updated successfully.");
    }

    public async Task<ServiceResultDto> DeleteRequestAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var existingRequest = await _requestRepository.GetRequestByIdAsync(hospitalId, requestId);

        if (existingRequest is null)
        {
            return ServiceResultDto.Fail("REQUEST_NOT_FOUND", "Request was not found.");
        }

        var deleted = await _requestRepository.DeletePendingRequestAsync(hospitalId, requestId, userId);

        if (!deleted)
        {
            return ServiceResultDto.Fail("REQUEST_CANCEL_FAILED", "Only pending requests can be cancelled.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RequestCancelled",
            Entity = "ORRequests",
            EntityId = requestId,
            OldValue = existingRequest.RequestStatus,
            NewValue = "Rejected",
            Remarks = "Request cancelled by user.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Request cancelled successfully.");
    }

    public async Task<ServiceResultDto> UpdateRequestStatusAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        UpdateRequestStatusDto request,
        string? ipAddress,
        string? userAgent)
    {
        var status = request.Status.Trim();

        if (!AllowedStatuses.Contains(status))
        {
            return ServiceResultDto.Fail("INVALID_REQUEST_STATUS", "Invalid request status.");
        }

        var existingRequest = await _requestRepository.GetRequestByIdAsync(hospitalId, requestId);

        if (existingRequest is null)
        {
            return ServiceResultDto.Fail("REQUEST_NOT_FOUND", "Request was not found.");
        }

        var updated = await _requestRepository.UpdateRequestStatusAsync(
            hospitalId,
            requestId,
            status,
            request.SchedulerRemarks,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail("REQUEST_STATUS_UPDATE_FAILED", "Request status could not be updated.");
        }

        if (status == "Waitlisted")
        {
            await _requestRepository.AddToWaitlistIfNotExistsAsync(requestId);
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = $"Request{status}",
            Entity = "ORRequests",
            EntityId = requestId,
            OldValue = existingRequest.RequestStatus,
            NewValue = status,
            Remarks = request.SchedulerRemarks,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok($"Request status updated to {status}.");
    }

    public async Task<ServiceResultDto<RequestScoreDto>> GetRequestScoreAsync(
        int hospitalId,
        int requestId)
    {
        var request = await _requestRepository.GetRequestByIdAsync(hospitalId, requestId);

        if (request is null)
        {
            return ServiceResultDto<RequestScoreDto>.Fail("REQUEST_NOT_FOUND", "Request was not found.");
        }

        var waitingDays = Math.Min((DateTime.UtcNow - request.CreatedAt).Days, 30);

        var priorityWeight = request.Priority switch
        {
            "Emergency" => 3,
            "Urgent" => 2,
            "Elective" => 1,
            _ => 1
        };

        var readinessWeight = request.PatientReadiness switch
        {
            "Ready" => 1.0m,
            "PendingClearance" => 0.5m,
            _ => 0m
        };

        var priorityScore = priorityWeight * 50;
        var waitingScore = waitingDays * 2;
        var readinessScore = readinessWeight * 20;
        var cycleWaitScore = request.CyclesWaited * 10;

        var total = priorityScore + waitingScore + readinessScore + cycleWaitScore;

        var score = new RequestScoreDto
        {
            RequestId = request.RequestId,
            PriorityScore = priorityScore,
            WaitingScore = waitingScore,
            ReadinessScore = readinessScore,
            CycleWaitScore = cycleWaitScore,
            TotalScore = total,
            IsStarved = request.CyclesWaited >= 3
        };

        return ServiceResultDto<RequestScoreDto>.Ok(score);
    }

    private async Task<ServiceResultDto> ValidateCreateRequestAsync(
        int hospitalId,
        CreateOrRequestDto request)
    {
        if (!AllowedQuarters.Contains(request.PreferredQuarter))
        {
            return ServiceResultDto.Fail("INVALID_QUARTER", "Preferred quarter must be Q1, Q2, or Flexible.");
        }

        if (!AllowedPriorities.Contains(request.Priority))
        {
            return ServiceResultDto.Fail("INVALID_PRIORITY", "Priority must be Emergency, Urgent, or Elective.");
        }

        if (!AllowedReadiness.Contains(request.PatientReadiness))
        {
            return ServiceResultDto.Fail("INVALID_READINESS", "Patient readiness is invalid.");
        }

        if (!_availabilityWindowEngine.IsValidMask(request.AvailableDaysMask))
        {
            return ServiceResultDto.Fail("INVALID_AVAILABLE_DAYS", "Available days selection is invalid.");
        }

        var patientExists = await _requestRepository.PatientExistsAsync(hospitalId, request.PatientId);

        if (!patientExists)
        {
            return ServiceResultDto.Fail("PATIENT_NOT_FOUND", "Patient was not found.");
        }

        return ServiceResultDto.Ok();
    }

    private ServiceResultDto ValidateUpdateRequest(UpdateOrRequestDto request)
    {
        if (!AllowedQuarters.Contains(request.PreferredQuarter))
        {
            return ServiceResultDto.Fail("INVALID_QUARTER", "Preferred quarter must be Q1, Q2, or Flexible.");
        }

        if (!AllowedPriorities.Contains(request.Priority))
        {
            return ServiceResultDto.Fail("INVALID_PRIORITY", "Priority must be Emergency, Urgent, or Elective.");
        }

        if (!AllowedReadiness.Contains(request.PatientReadiness))
        {
            return ServiceResultDto.Fail("INVALID_READINESS", "Patient readiness is invalid.");
        }

        if (!_availabilityWindowEngine.IsValidMask(request.AvailableDaysMask))
        {
            return ServiceResultDto.Fail("INVALID_AVAILABLE_DAYS", "Available days selection is invalid.");
        }

        return ServiceResultDto.Ok();
    }
}

internal static class CreateOrRequestDtoExtensions
{
    public static string RequestStatusText(this CreateOrRequestDto request)
    {
        return $"Priority={request.Priority}; Readiness={request.PatientReadiness}; Duration={request.EstimatedDurationMin}; AvailableDaysMask={request.AvailableDaysMask}";
    }
}