using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Automation;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class RequestRepository : IRequestRepository
{
    private readonly ORManagementDbContext _dbContext;

    public RequestRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrRequestResponseDto>> GetRequestsAsync(
        int hospitalId,
        string? status,
        int? cycleId)
    {
        var query = GetRequestQuery(hospitalId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.RequestStatus == status);
        }

        if (cycleId.HasValue)
        {
            query = query.Where(request => request.CycleId == cycleId.Value);
        }

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<OrRequestResponseDto>> GetMyRequestsAsync(
        int hospitalId,
        int surgeonId)
    {
        return await GetRequestQuery(hospitalId)
            .Where(request => request.SurgeonId == surgeonId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();
    }

    public async Task<OrRequestResponseDto?> GetRequestByIdAsync(
        int hospitalId,
        int requestId)
    {
        return await GetRequestQuery(hospitalId)
            .FirstOrDefaultAsync(request => request.RequestId == requestId);
    }

    public async Task<CurrentSchedulingCycleDto?> GetCurrentCycleAsync(int hospitalId)
    {
        return await _dbContext.SchedulingCycles
            .Where(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleStatus == "Open")
            .OrderBy(cycle => cycle.WeekStartDate)
            .Select(cycle => new CurrentSchedulingCycleDto
            {
                CycleId = cycle.CycleId,
                WeekStartDate = cycle.WeekStartDate.ToDateTime(TimeOnly.MinValue),
                WeekEndDate = cycle.WeekEndDate.ToDateTime(TimeOnly.MinValue),
                CycleStatus = cycle.CycleStatus ?? string.Empty
            })
            .FirstOrDefaultAsync();
    }

    private IQueryable<OrRequestResponseDto> GetRequestQuery(int hospitalId)
    {
        return
            from request in _dbContext.ORRequests
            join surgeon in _dbContext.Surgeons on request.SurgeonId equals surgeon.SurgeonId
            join user in _dbContext.Users on surgeon.UserId equals user.UserId
            join patient in _dbContext.Patients on request.PatientId equals patient.PatientId
            where request.HospitalId == hospitalId
            select new OrRequestResponseDto
            {
                RequestId = request.RequestId,
                HospitalId = request.HospitalId,
                SurgeonId = request.SurgeonId,
                PatientId = request.PatientId,

                SurgeonName = user.FullName,
                PatientName = patient.FullName,
                PatientMrn = patient.MRN,

                CycleId = request.CycleId,
                OriginalCycleId = request.OriginalCycleId,
                CyclesWaited = request.CyclesWaited,

                PreferredDate = request.PreferredDate.ToDateTime(TimeOnly.MinValue),
                PreferredQuarter = request.PreferredQuarter,
                EstimatedDurationMin = request.EstimatedDurationMin,

                SurgeryType = request.SurgeryType,
                Priority = request.Priority,
                PatientReadiness = request.PatientReadiness,

                RequestStatus = request.RequestStatus,
                Remarks = request.Remarks,
                SchedulerRemarks = request.SchedulerRemarks,

                AvailableDaysMask = request.AvailableDaysMask,
                AvailableDaysDisplay = string.Empty,

                CreatedAt = request.CreatedAt
            };
    }

    public async Task<int> CreateRequestAsync(
    int hospitalId,
    int surgeonId,
    CreateOrRequestDto request)
    {
        var preferredDate = request.PreferredDate ?? DateTime.UtcNow.Date;

        var preferredQuarter = string.IsNullOrWhiteSpace(request.PreferredQuarter)
            ? "Flexible"
            : request.PreferredQuarter.Trim();

        var availableDaysMask = request.AvailableDaysMask ?? 31;

        var priority = request.Priority.Trim();
        var patientReadiness = request.PatientReadiness.Trim();

        var entity = new ORRequest
        {
            HospitalId = hospitalId,
            SurgeonId = surgeonId,
            PatientId = request.PatientId,

            CycleId = priority == "Emergency"
                ? null
                : request.CycleId,

            OriginalCycleId = priority == "Emergency"
                ? null
                : request.OriginalCycleId ?? request.CycleId,

            CyclesWaited = 0,

            PreferredDate = DateOnly.FromDateTime(preferredDate.Date),
            PreferredQuarter = preferredQuarter,

            EstimatedDurationMin = request.EstimatedDurationMin,
            SurgeryType = request.SurgeryType.Trim(),
            Priority = priority,
            PatientReadiness = patientReadiness,

            Remarks = request.Remarks,
            RequestStatus = "PendingReview",
            AvailableDaysMask = availableDaysMask,

            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ORRequests.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return entity.RequestId;
    }

    public async Task<bool> UpdateRequestAsync(
        int hospitalId,
        int requestId,
        UpdateOrRequestDto request,
        int modifiedByUserId)
    {
        var entity = await _dbContext.ORRequests
            .FirstOrDefaultAsync(orRequest =>
                orRequest.HospitalId == hospitalId &&
                orRequest.RequestId == requestId);

        if (entity is null)
        {
            return false;
        }

        if (entity.RequestStatus != "PendingReview" &&
            entity.RequestStatus != "Modified")
        {
            return false;
        }

        entity.PreferredDate = DateOnly.FromDateTime(request.PreferredDate.Date);
        entity.PreferredQuarter = request.PreferredQuarter.Trim();
        entity.EstimatedDurationMin = request.EstimatedDurationMin;
        entity.SurgeryType = request.SurgeryType.Trim();
        entity.Priority = request.Priority.Trim();
        entity.PatientReadiness = request.PatientReadiness.Trim();
        entity.Remarks = request.Remarks;
        entity.AvailableDaysMask = request.AvailableDaysMask;
        entity.ModifiedByUserId = modifiedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateRequestStatusAsync(
        int hospitalId,
        int requestId,
        string status,
        string? schedulerRemarks,
        int modifiedByUserId)
    {
        var entity = await _dbContext.ORRequests
            .FirstOrDefaultAsync(request =>
                request.HospitalId == hospitalId &&
                request.RequestId == requestId);

        if (entity is null)
        {
            return false;
        }

        var oldStatus = entity.RequestStatus;

        entity.RequestStatus = status;
        entity.SchedulerRemarks = schedulerRemarks;
        entity.ModifiedByUserId = modifiedByUserId;

        if (oldStatus == "Waitlisted" && status != "Waitlisted")
        {
            var waitlist = await _dbContext.WaitlistRequests
                .FirstOrDefaultAsync(waitlist => waitlist.RequestId == requestId);

            if (waitlist is not null)
            {
                _dbContext.WaitlistRequests.Remove(waitlist);
            }
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }
    public async Task<List<AutoBlockDemandRequestDto>> GetAutoAssignCandidateRequestsForCycleAsync(
    int hospitalId,
    int cycleId)
    {
        return await _dbContext.ORRequests
            .Where(request =>
                request.HospitalId == hospitalId &&
                request.CycleId == cycleId &&
                request.PatientReadiness == "Ready" &&
                (
                    request.RequestStatus == "Approved" ||
                    request.RequestStatus == "Scheduled"
                ))
            .Select(request => new AutoBlockDemandRequestDto
            {
                RequestId = request.RequestId,
                SurgeonId = request.SurgeonId,
                EstimatedDurationMin = request.EstimatedDurationMin,
                Priority = request.Priority,
                PatientReadiness = request.PatientReadiness,
                RequestStatus = request.RequestStatus
            })
            .ToListAsync();
    }
    public async Task<List<AutoBlockDemandRequestDto>> GetApprovedReadyRequestsForCycleAsync(
    int hospitalId,
    int cycleId)
    {
        return await _dbContext.ORRequests
            .Where(request =>
                request.HospitalId == hospitalId &&
                request.CycleId == cycleId &&
                request.RequestStatus == "Approved" &&
                request.PatientReadiness == "Ready")
            .Select(request => new AutoBlockDemandRequestDto
            {
                RequestId = request.RequestId,
                SurgeonId = request.SurgeonId,
                EstimatedDurationMin = request.EstimatedDurationMin,
                Priority = request.Priority,
                PatientReadiness = request.PatientReadiness,
                RequestStatus = request.RequestStatus
            })
            .ToListAsync();
    }

    public async Task<RequestCapacitySummaryDto> GetCapacitySummaryAsync(int hospitalId)
    {
        const decimal defaultSchedulingHours = 100m;

        var hardcodedTopSurgeonIds = new List<int>
        {
            10,
            3
        };

        var settingValue = await _dbContext.SystemSettings
            .Where(setting =>
                setting.SettingKey == "DefaultWeeklySchedulingHours" &&
                (setting.HospitalId == hospitalId || setting.HospitalId == null))
            .OrderByDescending(setting => setting.HospitalId == hospitalId)
            .Select(setting => setting.SettingValue)
            .FirstOrDefaultAsync();

        var schedulingHourCapacity = defaultSchedulingHours;

        if (decimal.TryParse(settingValue, out var parsedHours) && parsedHours > 0)
        {
            schedulingHourCapacity = parsedHours;
        }

        var currentCycle = await _dbContext.SchedulingCycles
            .Where(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleStatus == "Open")
            .OrderBy(cycle => cycle.WeekStartDate)
            .FirstOrDefaultAsync();

        var approvedOrScheduledRequestsQuery = _dbContext.ORRequests
            .Where(request =>
                request.HospitalId == hospitalId &&
                (
                    request.RequestStatus == "Approved" ||
                    request.RequestStatus == "Scheduled"
                ));

        if (currentCycle is not null)
        {
            approvedOrScheduledRequestsQuery = approvedOrScheduledRequestsQuery
                .Where(request =>
                    request.CycleId == currentCycle.CycleId ||
                    request.OriginalCycleId == currentCycle.CycleId ||
                    (
                        request.CycleId == null &&
                        request.Priority == "Emergency"
                    ));
        }

        var approvedOrScheduledRequests = await approvedOrScheduledRequestsQuery
            .Select(request => new
            {
                request.SurgeonId,
                request.EstimatedDurationMin
            })
            .ToListAsync();

        var openAllocatedMinutes = approvedOrScheduledRequests
            .Where(request => !hardcodedTopSurgeonIds.Contains(request.SurgeonId))
            .Sum(request => request.EstimatedDurationMin);

        var allocatedHourCapacity = Math.Round(openAllocatedMinutes / 60m, 2);

        var topSurgeons = await (
            from surgeon in _dbContext.Surgeons
            join user in _dbContext.Users
                on surgeon.UserId equals user.UserId
            where surgeon.HospitalId == hospitalId &&
                  hardcodedTopSurgeonIds.Contains(surgeon.SurgeonId)
            select new
            {
                surgeon.SurgeonId,
                SurgeonName = user.FullName
            })
            .ToListAsync();

        var topRecurringDoctors = hardcodedTopSurgeonIds
            .Select(surgeonId =>
            {
                var surgeon = topSurgeons
                    .FirstOrDefault(item => item.SurgeonId == surgeonId);

                var recurringMinutes = approvedOrScheduledRequests
                    .Where(request => request.SurgeonId == surgeonId)
                    .Sum(request => request.EstimatedDurationMin);

                return new TopDoctorRecurringCapacityDto
                {
                    SurgeonId = surgeonId,
                    SurgeonName = surgeon?.SurgeonName ?? $"Surgeon #{surgeonId}",
                    RecurringHours = Math.Round(recurringMinutes / 60m, 2)
                };
            })
            .ToList();

        var totalTopRecurringHours = topRecurringDoctors
            .Sum(doctor => doctor.RecurringHours);

        var remainingHourCapacity = Math.Round(
            Math.Max(
                schedulingHourCapacity -
                allocatedHourCapacity -
                totalTopRecurringHours,
                0),
            2);

        return new RequestCapacitySummaryDto
        {
            SchedulingHourCapacity = schedulingHourCapacity,
            AllocatedHourCapacity = allocatedHourCapacity,
            RemainingHourCapacity = remainingHourCapacity,
            TopRecurringDoctors = topRecurringDoctors
        };
    }

    public async Task<bool> DeletePendingRequestAsync(
        int hospitalId,
        int requestId,
        int userId)
    {
        var entity = await _dbContext.ORRequests
            .FirstOrDefaultAsync(request =>
                request.HospitalId == hospitalId &&
                request.RequestId == requestId);

        if (entity is null)
        {
            return false;
        }

        if (entity.RequestStatus != "PendingReview")
        {
            return false;
        }

        entity.RequestStatus = "Rejected";
        entity.SchedulerRemarks = "Cancelled by requester.";
        entity.ModifiedByUserId = userId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddToWaitlistIfNotExistsAsync(int requestId)
    {
        var exists = await _dbContext.WaitlistRequests
            .AnyAsync(waitlist => waitlist.RequestId == requestId);

        if (exists)
        {
            return true;
        }

        var waitlistRequest = new WaitlistRequest
        {
            RequestId = requestId,
            WaitingSince = DateTime.UtcNow
        };

        await _dbContext.WaitlistRequests.AddAsync(waitlistRequest);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveFromWaitlistByRequestIdAsync(int requestId)
    {
        var waitlist = await _dbContext.WaitlistRequests
            .FirstOrDefaultAsync(waitlist => waitlist.RequestId == requestId);

        if (waitlist is null)
        {
            return true;
        }

        _dbContext.WaitlistRequests.Remove(waitlist);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PatientExistsAsync(int hospitalId, int patientId)
    {
        return await _dbContext.Patients
            .AnyAsync(patient =>
                patient.HospitalId == hospitalId &&
                patient.PatientId == patientId &&
                patient.IsActive);
    }
}