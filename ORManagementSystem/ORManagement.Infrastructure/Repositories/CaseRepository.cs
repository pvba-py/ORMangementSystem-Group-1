using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class CaseRepository : ICaseRepository
{
    private readonly ORManagementDbContext _dbContext;

    public CaseRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SurgicalCaseDto>> GetCasesAsync(int hospitalId, string? status)
    {
        var query = GetCaseQuery(hospitalId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(surgicalCase => surgicalCase.CaseStatus == status);
        }

        return await query
            .OrderBy(surgicalCase => surgicalCase.ScheduledStart)
            .ToListAsync();
    }

    public async Task<bool> SurgeonCaseConflictExistsAsync(
     int hospitalId,
     int surgeonId,
     DateTime scheduledStart,
     DateTime scheduledEnd,
     int? excludeSurgeryId = null)
    {
        var query =
            from surgicalCase in _dbContext.SurgicalCases
            join orRequest in _dbContext.ORRequests
                on surgicalCase.RequestId equals orRequest.RequestId
            where surgicalCase.HospitalId == hospitalId
                  && orRequest.SurgeonId == surgeonId
                  && surgicalCase.CaseStatus != "Cancelled"
                  && scheduledStart <
                        (
                            surgicalCase.CaseStatus == "Completed" &&
                            surgicalCase.ActualEnd.HasValue
                                ? surgicalCase.ActualEnd.Value
                                : surgicalCase.ScheduledEnd
                        )
                  && scheduledEnd > surgicalCase.ScheduledStart
            select surgicalCase;

        if (excludeSurgeryId.HasValue)
        {
            query = query.Where(surgicalCase =>
                surgicalCase.SurgeryId != excludeSurgeryId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<BlockBoundaryDto?> GetBlockBoundaryAsync(
    int hospitalId,
    int blockId)
    {
        return await _dbContext.BlockAllocations
            .Where(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId &&
                block.BlockStatus != "Cancelled")
            .Select(block => new BlockBoundaryDto
            {
                BlockId = block.BlockId,
                SurgeonId = block.SurgeonId,
                BlockType = block.BlockType,
                BlockDate = block.BlockDate.ToDateTime(TimeOnly.MinValue),
                StartTime = block.StartTime,
                EndTime = block.EndTime
            })
            .FirstOrDefaultAsync();
    }
    public async Task<List<SurgicalCaseDto>> GetMyCasesAsync(int hospitalId, int surgeonId)
    {
        return await GetCaseQuery(hospitalId)
            .Where(surgicalCase => surgicalCase.SurgeonId == surgeonId)
            .OrderBy(surgicalCase => surgicalCase.ScheduledStart)
            .ToListAsync();
    }

    public async Task<SurgicalCaseDto?> GetCaseByIdAsync(int hospitalId, int surgeryId)
    {
        return await GetCaseQuery(hospitalId)
            .FirstOrDefaultAsync(surgicalCase => surgicalCase.SurgeryId == surgeryId);
    }

    private IQueryable<SurgicalCaseDto> GetCaseQuery(int hospitalId)
    {
        return
            from surgicalCase in _dbContext.SurgicalCases
            join request in _dbContext.ORRequests on surgicalCase.RequestId equals request.RequestId
            join surgeon in _dbContext.Surgeons on surgicalCase.SurgeonId equals surgeon.SurgeonId
            join user in _dbContext.Users on surgeon.UserId equals user.UserId
            join room in _dbContext.OperatingRooms on surgicalCase.ORRoomId equals room.ORRoomId
            join patient in _dbContext.Patients on request.PatientId equals patient.PatientId
            where surgicalCase.HospitalId == hospitalId
            select new SurgicalCaseDto
            {
                SurgeryId = surgicalCase.SurgeryId,
                HospitalId = surgicalCase.HospitalId,
                RequestId = surgicalCase.RequestId,
                BlockId = surgicalCase.BlockId,
                SurgeonId = surgicalCase.SurgeonId,
                ORRoomId = surgicalCase.ORRoomId,

                SurgeonName = user.FullName,
                RoomName = room.RoomName,

                PatientId = patient.PatientId,
                PatientName = patient.FullName,
                PatientMrn = patient.MRN,

                SurgeryType = request.SurgeryType,

                ScheduledStart = surgicalCase.ScheduledStart,
                ScheduledEnd = surgicalCase.ScheduledEnd,
                ActualStart = surgicalCase.ActualStart,
                ActualEnd = surgicalCase.ActualEnd,

                CaseStatus = surgicalCase.CaseStatus,
                CancellationReason = surgicalCase.CancellationReason
            };
    }

    public async Task<OrRequestResponseDto?> GetRequestForSchedulingAsync(int hospitalId, int requestId)
    {
        return await
            (
                from request in _dbContext.ORRequests
                join surgeon in _dbContext.Surgeons on request.SurgeonId equals surgeon.SurgeonId
                join user in _dbContext.Users on surgeon.UserId equals user.UserId
                join patient in _dbContext.Patients on request.PatientId equals patient.PatientId
                where request.HospitalId == hospitalId && request.RequestId == requestId
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
                    CreatedAt = request.CreatedAt
                }
            )
            .FirstOrDefaultAsync();
    }

    public async Task<bool> BlockExistsForRequestAsync(int hospitalId, int blockId, int surgeonId)
    {
        return await _dbContext.BlockAllocations
            .AnyAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId &&
                block.SurgeonId == surgeonId &&
                block.BlockStatus != "Cancelled" &&
                block.BlockStatus != "Released");
    }

    public async Task<bool> HasCaseConflictAsync(
     int hospitalId,
     int blockId,
     DateTime scheduledStart,
     DateTime scheduledEnd,
     int? excludeSurgeryId = null)
    {
        var query = _dbContext.SurgicalCases
            .Where(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                surgicalCase.BlockId == blockId &&
                surgicalCase.CaseStatus != "Cancelled" &&
                scheduledStart <
                    (
                        surgicalCase.CaseStatus == "Completed" &&
                        surgicalCase.ActualEnd.HasValue
                            ? surgicalCase.ActualEnd.Value
                            : surgicalCase.ScheduledEnd
                    ) &&
                scheduledEnd > surgicalCase.ScheduledStart);

        if (excludeSurgeryId.HasValue)
        {
            query = query.Where(surgicalCase =>
                surgicalCase.SurgeryId != excludeSurgeryId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int?> GetBlockRoomIdAsync(int hospitalId, int blockId)
    {
        return await _dbContext.BlockAllocations
            .Where(block => block.HospitalId == hospitalId && block.BlockId == blockId)
            .Select(block => (int?)block.ORRoomId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateCaseAsync(
        int hospitalId,
        int requestId,
        int blockId,
        int surgeonId,
        int orRoomId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        int modifiedByUserId)
    {
        var entity = new SurgicalCase
        {
            HospitalId = hospitalId,
            RequestId = requestId,
            BlockId = blockId,
            SurgeonId = surgeonId,
            ORRoomId = orRoomId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            CaseStatus = "Scheduled",
            ModifiedByUserId = modifiedByUserId
        };

        await _dbContext.SurgicalCases.AddAsync(entity);

        var block = await _dbContext.BlockAllocations
            .FirstOrDefaultAsync(block => block.HospitalId == hospitalId && block.BlockId == blockId);

        if (block is not null && block.BlockStatus == "Allocated")
        {
            block.BlockStatus = "PartiallyBooked";
            block.ModifiedByUserId = modifiedByUserId;
        }

        await _dbContext.SaveChangesAsync();

        return entity.SurgeryId;
    }

    public async Task<bool> UpdateCaseAsync(
        int hospitalId,
        int surgeryId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        int modifiedByUserId)
    {
        var entity = await _dbContext.SurgicalCases
            .FirstOrDefaultAsync(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                surgicalCase.SurgeryId == surgeryId);

        if (entity is null)
        {
            return false;
        }

        if (entity.CaseStatus != "Scheduled")
        {
            return false;
        }

        entity.ScheduledStart = scheduledStart;
        entity.ScheduledEnd = scheduledEnd;
        entity.ModifiedByUserId = modifiedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateCaseStatusAsync(
        int hospitalId,
        int surgeryId,
        string status,
        DateTime? actualStart,
        DateTime? actualEnd,
        string? cancellationReason,
        int modifiedByUserId)
    {
        var entity = await _dbContext.SurgicalCases
            .FirstOrDefaultAsync(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                surgicalCase.SurgeryId == surgeryId);

        if (entity is null)
        {
            return false;
        }

        entity.CaseStatus = status;
        entity.ModifiedByUserId = modifiedByUserId;

        if (status == "InProgress")
        {
            entity.ActualStart = actualStart ?? DateTime.UtcNow;
        }

        if (status == "Completed")
        {
            entity.ActualEnd = actualEnd ?? DateTime.UtcNow;

            if (entity.ActualEnd.Value < entity.ScheduledEnd)
            {
                var releaseStartTime = TimeOnly.FromDateTime(entity.ActualEnd.Value);
                var releaseEndTime = TimeOnly.FromDateTime(entity.ScheduledEnd);

                if (releaseEndTime > releaseStartTime)
                {
                    var existingReleasedSlot = await _dbContext.ReleasedSlots
                        .AnyAsync(slot =>
                            slot.HospitalId == hospitalId &&
                            slot.BlockId == entity.BlockId &&
                            slot.SlotState != "Cancelled" &&
                            releaseStartTime < slot.EndTime &&
                            releaseEndTime > slot.StartTime);

                    if (!existingReleasedSlot)
                    {
                        var releasedSlot = new ReleasedSlot
                        {
                            HospitalId = entity.HospitalId,
                            BlockId = entity.BlockId,
                            ORRoomId = entity.ORRoomId,
                            SlotDate = DateOnly.FromDateTime(entity.ScheduledStart.Date),
                            StartTime = releaseStartTime,
                            EndTime = releaseEndTime,
                            Source = "BlockRelease",
                            ReleasedByUserId = modifiedByUserId,
                            SlotState = "Available",
                            CreatedAt = DateTime.UtcNow
                        };

                        await _dbContext.ReleasedSlots.AddAsync(releasedSlot);

                        var block = await _dbContext.BlockAllocations
                            .FirstOrDefaultAsync(block =>
                                block.HospitalId == hospitalId &&
                                block.BlockId == entity.BlockId);

                        if (block is not null && block.BlockStatus == "FullyBooked")
                        {
                            block.BlockStatus = "PartiallyBooked";
                            block.ModifiedByUserId = modifiedByUserId;
                        }
                    }
                }
            }
        }

        if (status == "Cancelled")
        {
            entity.CancellationReason = cancellationReason;
        }

        await _dbContext.SaveChangesAsync();

        if (status == "Completed")
        {
            await CalculateUtilizationForBlockAsync(entity.BlockId);
        }

        return true;
    }

    public async Task<bool> MarkRequestScheduledAsync(int hospitalId, int requestId, int modifiedByUserId)
    {
        var request = await _dbContext.ORRequests
            .FirstOrDefaultAsync(request =>
                request.HospitalId == hospitalId &&
                request.RequestId == requestId);

        if (request is null)
        {
            return false;
        }

        request.RequestStatus = "Scheduled";
        request.ModifiedByUserId = modifiedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task CalculateUtilizationForBlockAsync(int blockId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC analytics.usp_CalculateBlockUtilization @BlockId",
            new SqlParameter("@BlockId", blockId));
    }
    
}
