using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Waitlist;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class WaitlistRepository : IWaitlistRepository
{
    private readonly ORManagementDbContext _dbContext;

    public WaitlistRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ReleasedSlotDto>> GetReleasedSlotsAsync(
        int hospitalId,
        string? state,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query =
            from slot in _dbContext.ReleasedSlots
            join room in _dbContext.OperatingRooms on slot.ORRoomId equals room.ORRoomId
            where slot.HospitalId == hospitalId
            select new
            {
                slot,
                room
            };

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(item => item.slot.SlotState == state);
        }

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(item => item.slot.SlotDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value.Date);
            query = query.Where(item => item.slot.SlotDate <= to);
        }

        return await query
            .OrderBy(item => item.slot.SlotDate)
            .ThenBy(item => item.slot.StartTime)
            .Select(item => new ReleasedSlotDto
            {
                SlotId = item.slot.SlotId,
                HospitalId = item.slot.HospitalId,
                BlockId = item.slot.BlockId,
                ORRoomId = item.slot.ORRoomId,
                RoomName = item.room.RoomName,
                SlotDate = item.slot.SlotDate.ToDateTime(TimeOnly.MinValue),
                StartTime = item.slot.StartTime,
                EndTime = item.slot.EndTime,
                Source = item.slot.Source,
                ReleasedByUserId = item.slot.ReleasedByUserId,
                SlotState = item.slot.SlotState,
                CreatedAt = item.slot.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ReleasedSlotDto?> GetReleasedSlotByIdAsync(int hospitalId, int slotId)
    {
        return await
            (
                from slot in _dbContext.ReleasedSlots
                join room in _dbContext.OperatingRooms on slot.ORRoomId equals room.ORRoomId
                where slot.HospitalId == hospitalId && slot.SlotId == slotId
                select new ReleasedSlotDto
                {
                    SlotId = slot.SlotId,
                    HospitalId = slot.HospitalId,
                    BlockId = slot.BlockId,
                    ORRoomId = slot.ORRoomId,
                    RoomName = room.RoomName,
                    SlotDate = slot.SlotDate.ToDateTime(TimeOnly.MinValue),
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Source = slot.Source,
                    ReleasedByUserId = slot.ReleasedByUserId,
                    SlotState = slot.SlotState,
                    CreatedAt = slot.CreatedAt
                }
            )
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateReleasedSlotStatusAsync(
        int hospitalId,
        int slotId,
        string slotState)
    {
        var slot = await _dbContext.ReleasedSlots
            .FirstOrDefaultAsync(slot =>
                slot.HospitalId == hospitalId &&
                slot.SlotId == slotId);

        if (slot is null)
        {
            return false;
        }

        slot.SlotState = slotState;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<WaitlistRequestDto>> GetWaitlistAsync(int hospitalId)
    {
        return await GetWaitlistQuery(hospitalId)
            .OrderByDescending(item => item.CyclesWaited)
            .ThenBy(item => item.WaitingSince)
            .ToListAsync();
    }

    public async Task<WaitlistRequestDto?> GetWaitlistByIdAsync(int hospitalId, int waitlistId)
    {
        return await GetWaitlistQuery(hospitalId)
            .FirstOrDefaultAsync(item => item.WaitlistId == waitlistId);
    }

    public async Task<List<WaitlistRequestDto>> GetStarvedRequestsAsync(
        int hospitalId,
        int starvationThreshold)
    {
        return await GetWaitlistQuery(hospitalId)
            .Where(item => item.CyclesWaited >= starvationThreshold)
            .OrderByDescending(item => item.CyclesWaited)
            .ThenBy(item => item.WaitingSince)
            .ToListAsync();
    }

    private IQueryable<WaitlistRequestDto> GetWaitlistQuery(int hospitalId)
    {
        return
            from waitlist in _dbContext.WaitlistRequests
            join request in _dbContext.ORRequests on waitlist.RequestId equals request.RequestId
            join surgeon in _dbContext.Surgeons on request.SurgeonId equals surgeon.SurgeonId
            join user in _dbContext.Users on surgeon.UserId equals user.UserId
            join patient in _dbContext.Patients on request.PatientId equals patient.PatientId
            where request.HospitalId == hospitalId
                  && request.RequestStatus == "Waitlisted"
            select new WaitlistRequestDto
            {
                WaitlistId = waitlist.WaitlistId,
                RequestId = waitlist.RequestId,

                SurgeonId = request.SurgeonId,
                SurgeonName = user.FullName,

                PatientId = patient.PatientId,
                PatientName = patient.FullName,
                PatientMrn = patient.MRN,

                SurgeryType = request.SurgeryType,
                Priority = request.Priority,
                PatientReadiness = request.PatientReadiness,
                EstimatedDurationMin = request.EstimatedDurationMin,
                CyclesWaited = request.CyclesWaited,

                AvailableDaysMask = request.AvailableDaysMask,
                AvailableDaysDisplay = string.Empty,

                WaitingSince = waitlist.WaitingSince,
                MatchScore = waitlist.MatchScore,
                MatchedSlotId = waitlist.MatchedSlotId
            };
    }

    public async Task<List<WaitlistMatchDto>> GetMatchesAsync(int slotId)
    {
        var matches = new List<WaitlistMatchDto>();

        var connection = _dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = "scheduling.usp_GetWaitlistMatches";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@SlotId", slotId));

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            matches.Add(new WaitlistMatchDto
            {
                WaitlistId = reader.GetInt32(reader.GetOrdinal("WaitlistId")),
                RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                SurgeonId = reader.GetInt32(reader.GetOrdinal("SurgeonId")),
                SurgeryType = reader.GetString(reader.GetOrdinal("SurgeryType")),
                Priority = reader.GetString(reader.GetOrdinal("Priority")),
                PatientReadiness = reader.GetString(reader.GetOrdinal("PatientReadiness")),
                EstimatedDurationMin = reader.GetInt32(reader.GetOrdinal("EstimatedDurationMin")),
                CyclesWaited = reader.GetInt32(reader.GetOrdinal("CyclesWaited")),
                WaitingDays = reader.GetInt32(reader.GetOrdinal("WaitingDays")),
                SlotMin = reader.GetInt32(reader.GetOrdinal("SlotMin")),
                MatchScore = reader.GetDecimal(reader.GetOrdinal("MatchScore"))
            });
        }

        return matches;
    }

    public async Task<bool> AssignWaitlistAsync(
    int hospitalId,
    int waitlistId,
    int slotId,
    decimal? matchScore,
    int modifiedByUserId)
    {
        var waitlist = await _dbContext.WaitlistRequests
            .FirstOrDefaultAsync(waitlist => waitlist.WaitlistId == waitlistId);

        if (waitlist is null)
        {
            return false;
        }

        var request = await _dbContext.ORRequests
            .FirstOrDefaultAsync(request =>
                request.RequestId == waitlist.RequestId &&
                request.HospitalId == hospitalId);

        if (request is null)
        {
            return false;
        }

        if (request.RequestStatus != "Waitlisted")
        {
            return false;
        }

        if (request.PatientReadiness != "Ready")
        {
            return false;
        }

        var slot = await _dbContext.ReleasedSlots
            .FirstOrDefaultAsync(slot =>
                slot.SlotId == slotId &&
                slot.HospitalId == hospitalId);

        if (slot is null)
        {
            return false;
        }

        if (slot.SlotState != "Available" && slot.SlotState != "Matched")
        {
            return false;
        }

        var slotDate = slot.SlotDate.ToDateTime(TimeOnly.MinValue);
        var scheduledStart = slotDate.Add(slot.StartTime.ToTimeSpan());
        var scheduledEnd = scheduledStart.AddMinutes(request.EstimatedDurationMin);
        var slotEnd = slotDate.Add(slot.EndTime.ToTimeSpan());

        if (scheduledEnd > slotEnd)
        {
            return false;
        }

        var block = await _dbContext.BlockAllocations
            .FirstOrDefaultAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == slot.BlockId &&
                block.BlockStatus != "Cancelled");

        if (block is null)
        {
            return false;
        }
        var blockTypeCompatible =
    block.BlockType != "Emergency" ||
    request.Priority == "Emergency";

        if (!blockTypeCompatible)
        {
            return false;
        }
        var caseConflictExists = await _dbContext.SurgicalCases
            .AnyAsync(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                surgicalCase.BlockId == slot.BlockId &&
                surgicalCase.CaseStatus != "Cancelled" &&
                scheduledStart <
                    (
                        surgicalCase.CaseStatus == "Completed" &&
                        surgicalCase.ActualEnd.HasValue
                            ? surgicalCase.ActualEnd.Value
                            : surgicalCase.ScheduledEnd
                    ) &&
                scheduledEnd > surgicalCase.ScheduledStart);

        if (caseConflictExists)
        {
            return false;
        }

        var surgeonConflictExists = await _dbContext.SurgicalCases
            .AnyAsync(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                surgicalCase.SurgeonId == request.SurgeonId &&
                surgicalCase.CaseStatus != "Cancelled" &&
                scheduledStart <
                    (
                        surgicalCase.CaseStatus == "Completed" &&
                        surgicalCase.ActualEnd.HasValue
                            ? surgicalCase.ActualEnd.Value
                            : surgicalCase.ScheduledEnd
                    ) &&
                scheduledEnd > surgicalCase.ScheduledStart);

        if (surgeonConflictExists)
        {
            return false;
        }

        var surgicalCaseEntity = new SurgicalCase
        {
            HospitalId = hospitalId,
            RequestId = request.RequestId,
            BlockId = slot.BlockId,
            SurgeonId = request.SurgeonId,
            ORRoomId = slot.ORRoomId,
            ScheduledStart = scheduledStart,
            ScheduledEnd = scheduledEnd,
            CaseStatus = "Scheduled",
            ModifiedByUserId = modifiedByUserId
        };

        await _dbContext.SurgicalCases.AddAsync(surgicalCaseEntity);

        waitlist.MatchedSlotId = slotId;
        waitlist.MatchScore = matchScore;

        request.RequestStatus = "Scheduled";
        request.ModifiedByUserId = modifiedByUserId;

        slot.SlotState = "Assigned";

        block.BlockStatus = "PartiallyBooked";
        block.ModifiedByUserId = modifiedByUserId;

        _dbContext.WaitlistRequests.Remove(waitlist);

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveWaitlistAsync(int hospitalId, int waitlistId)
    {
        var waitlist = await _dbContext.WaitlistRequests
            .FirstOrDefaultAsync(waitlist => waitlist.WaitlistId == waitlistId);

        if (waitlist is null)
        {
            return false;
        }

        var request = await _dbContext.ORRequests
            .FirstOrDefaultAsync(request =>
                request.RequestId == waitlist.RequestId &&
                request.HospitalId == hospitalId);

        if (request is not null && request.RequestStatus == "Waitlisted")
        {
            request.RequestStatus = "Modified";
            request.SchedulerRemarks = "Removed from waitlist.";
        }

        _dbContext.WaitlistRequests.Remove(waitlist);

        await _dbContext.SaveChangesAsync();

        return true;
    }
}