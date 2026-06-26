using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Cycles;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;

namespace ORManagement.Infrastructure.Repositories;

public class CycleRepository : ICycleRepository
{
    private readonly ORManagementDbContext _dbContext;

    public CycleRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SchedulingCycleDto?> GetCurrentCycleAsync(int hospitalId)
    {
        return await _dbContext.SchedulingCycles
            .Where(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleStatus == "Open")
            .OrderBy(cycle => cycle.WeekStartDate)
            .Select(cycle => new SchedulingCycleDto
            {
                CycleId = cycle.CycleId,
                HospitalId = cycle.HospitalId,
                WeekStartDate = cycle.WeekStartDate.ToDateTime(TimeOnly.MinValue),
                WeekEndDate = cycle.WeekEndDate.ToDateTime(TimeOnly.MinValue),
                CutoffAt = cycle.CutoffAt,
                CycleStatus = cycle.CycleStatus
            })
            .FirstOrDefaultAsync();
    }

    public async Task<SchedulingCycleDto?> GetCycleByIdAsync(int hospitalId, int cycleId)
    {
        return await _dbContext.SchedulingCycles
            .Where(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleId == cycleId)
            .Select(cycle => new SchedulingCycleDto
            {
                CycleId = cycle.CycleId,
                HospitalId = cycle.HospitalId,
                WeekStartDate = cycle.WeekStartDate.ToDateTime(TimeOnly.MinValue),
                WeekEndDate = cycle.WeekEndDate.ToDateTime(TimeOnly.MinValue),
                CutoffAt = cycle.CutoffAt,
                CycleStatus = cycle.CycleStatus
            })
            .FirstOrDefaultAsync();
    }
    public async Task<List<SchedulingCycleDto>> GetCyclesAsync(int hospitalId)
    {
        return await _dbContext.SchedulingCycles
            .Where(cycle => cycle.HospitalId == hospitalId)
            .OrderBy(cycle => cycle.WeekStartDate)
            .Select(cycle => new SchedulingCycleDto
            {
                CycleId = cycle.CycleId,
                HospitalId = cycle.HospitalId,
                WeekStartDate = cycle.WeekStartDate.ToDateTime(TimeOnly.MinValue),
                WeekEndDate = cycle.WeekEndDate.ToDateTime(TimeOnly.MinValue),
                CutoffAt = cycle.CutoffAt,
                CycleStatus = cycle.CycleStatus
            })
            .ToListAsync();
    }
    public async Task<List<RankedRequestDto>> GetRankedRequestsAsync(int cycleId)
    {
        var rankedRequests = new List<RankedRequestDto>();

        var connection = _dbContext.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandText = "scheduling.usp_GetCycleRankedRequests";
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@CycleId", cycleId));

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rankedRequests.Add(new RankedRequestDto
            {
                RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                SurgeonId = reader.GetInt32(reader.GetOrdinal("SurgeonId")),
                PatientId = reader.GetInt32(reader.GetOrdinal("PatientId")),

                SurgeryType = reader.GetString(reader.GetOrdinal("SurgeryType")),
                Priority = reader.GetString(reader.GetOrdinal("Priority")),
                PatientReadiness = reader.GetString(reader.GetOrdinal("PatientReadiness")),

                EstimatedDurationMin = reader.GetInt32(reader.GetOrdinal("EstimatedDurationMin")),
                PreferredQuarter = reader.GetString(reader.GetOrdinal("PreferredQuarter")),

                AvailableDaysMask = reader.GetInt32(reader.GetOrdinal("AvailableDaysMask")),
                AvailableDaysDisplay = reader.GetString(reader.GetOrdinal("AvailableDaysDisplay")),

                CyclesWaited = reader.GetInt32(reader.GetOrdinal("CyclesWaited")),
                WaitingDays = reader.GetInt32(reader.GetOrdinal("WaitingDays")),

                RankScore = reader.GetDecimal(reader.GetOrdinal("RankScore")),
                IsStarved = reader.GetInt32(reader.GetOrdinal("IsStarved")) == 1
            });
        }

        return rankedRequests;
    }

    public async Task<bool> CutoffCycleAsync(int hospitalId, int cycleId)
    {
        var cycle = await _dbContext.SchedulingCycles
            .FirstOrDefaultAsync(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleId == cycleId);

        if (cycle is null)
        {
            return false;
        }

        if (cycle.CycleStatus != "Open")
        {
            return false;
        }

        cycle.CycleStatus = "Cutoff";

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> StartCycleAsync(
    int hospitalId,
    int cycleId,
    int createdByUserId)
    {
        var cycle = await _dbContext.SchedulingCycles
            .FirstOrDefaultAsync(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleId == cycleId);

        if (cycle is null)
        {
            return false;
        }

        if (cycle.CycleStatus != "Closed")
        {
            return false;
        }

        cycle.CycleStatus = "Open";

        await _dbContext.SaveChangesAsync();

        return true;
    }
    public async Task<bool> HasOpenCycleAsync(
     int hospitalId,
     int? excludeCycleId = null)
    {
        return await _dbContext.SchedulingCycles
            .AnyAsync(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleStatus == "Open" &&
                (!excludeCycleId.HasValue || cycle.CycleId != excludeCycleId.Value));
    }
    public async Task<bool> CloseCycleAsync(
    int hospitalId,
    int cycleId,
    int modifiedByUserId)
    {
        var cycle = await _dbContext.SchedulingCycles
            .FirstOrDefaultAsync(cycle =>
                cycle.HospitalId == hospitalId &&
                cycle.CycleId == cycleId);

        if (cycle is null)
        {
            return false;
        }

        if (cycle.CycleStatus != "Published")
        {
            return false;
        }

        cycle.CycleStatus = "Closed";

        await _dbContext.SaveChangesAsync();

        return true;
    }
    public async Task<bool> PublishCycleAsync(
    int hospitalId,
    int cycleId, int modifiedByUserId)
    {
        
    var cycle = await _dbContext.SchedulingCycles
        .FirstOrDefaultAsync(cycle =>
            cycle.HospitalId == hospitalId &&
            cycle.CycleId == cycleId);

        if (cycle is null)
        {
            return false;
        }

        if (cycle.CycleStatus != "Cutoff")
        {
            return false;
        }

        cycle.CycleStatus = "Published";

        await _dbContext.SaveChangesAsync();

        return true;
    }



}