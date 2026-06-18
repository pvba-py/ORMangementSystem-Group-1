using Azure.Core;
using Microsoft.EntityFrameworkCore;
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
        var entity = new ORRequest
        {
            HospitalId = hospitalId,
            SurgeonId = surgeonId,
            PatientId = request.PatientId,
            CycleId = request.Priority == "Emergency" ? null : request.CycleId,
            OriginalCycleId = request.Priority == "Emergency" ? null : request.CycleId,
            CyclesWaited = 0,
            PreferredDate = DateOnly.FromDateTime(request.PreferredDate.Date),
            PreferredQuarter = request.PreferredQuarter.Trim(),
            EstimatedDurationMin = request.EstimatedDurationMin,
            SurgeryType = request.SurgeryType.Trim(),
            Priority = request.Priority.Trim(),
            PatientReadiness = request.PatientReadiness.Trim(),
            Remarks = request.Remarks,
            RequestStatus = "PendingReview",
            AvailableDaysMask = request.AvailableDaysMask,
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
            .FirstOrDefaultAsync(request =>
                request.HospitalId == hospitalId &&
                request.RequestId == requestId);

        if (entity is null)
        {
            return false;
        }

        if (entity.RequestStatus != "PendingReview" && entity.RequestStatus != "Modified")
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

    public async Task<bool> PatientExistsAsync(int hospitalId, int patientId)
    {
        return await _dbContext.Patients
            .AnyAsync(patient =>
                patient.HospitalId == hospitalId &&
                patient.PatientId == patientId &&
                patient.IsActive);
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

}