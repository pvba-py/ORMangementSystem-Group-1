using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Utilization;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;

namespace ORManagement.Infrastructure.Repositories;

public class UtilizationRepository : IUtilizationRepository
{
    private readonly ORManagementDbContext _dbContext;

    public UtilizationRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UtilizationRecordDto>> GetUtilizationRecordsAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId,
        string? status)
    {
        var query =
            from utilization in _dbContext.UtilizationRecords
            join block in _dbContext.BlockAllocations
                on utilization.BlockId equals block.BlockId
            join room in _dbContext.OperatingRooms
                on block.ORRoomId equals room.ORRoomId
            where block.HospitalId == hospitalId
            select new
            {
                utilization,
                block,
                room,
                SurgeonName =
                    block.SurgeonId == null
                        ? null
                        : (
                            from surgeon in _dbContext.Surgeons
                            join user in _dbContext.Users
                                on surgeon.UserId equals user.UserId
                            where surgeon.SurgeonId == block.SurgeonId
                            select user.FullName
                        ).FirstOrDefault()
            };

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(item => item.block.BlockDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value.Date);
            query = query.Where(item => item.block.BlockDate <= to);
        }

        if (surgeonId.HasValue)
        {
            query = query.Where(item => item.block.SurgeonId == surgeonId.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(item => item.block.ORRoomId == roomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.utilization.UtilStatus == status);
        }

        return await query
            .OrderByDescending(item => item.block.BlockDate)
            .ThenBy(item => item.block.StartTime)
            .Select(item => new UtilizationRecordDto
            {
                UtilizationId = item.utilization.UtilizationId,
                HospitalId = item.block.HospitalId,
                BlockId = item.utilization.BlockId,

                SurgeonId = item.block.SurgeonId,
                SurgeonName = item.SurgeonName ?? item.block.BlockType + " Capacity",

                ORRoomId = item.block.ORRoomId,
                RoomName = item.room.RoomName,

                BlockDate = item.block.BlockDate.ToDateTime(TimeOnly.MinValue),

                AllocatedMinutes = item.utilization.AllocatedMinutes,
                UsedMinutes = item.utilization.UsedMinutes,
                UtilizationPercent = item.utilization.UtilizationPct ?? 0,
                UtilizationStatus = item.utilization.UtilStatus,

                CalculatedAt = item.utilization.CalculatedAt
            })
            .ToListAsync();
    }

    public async Task<List<ORRoomUtilizationRecordDto>> GetORRoomUtilizationRecordsAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? roomId,
        string? status)
    {
        var query =
            from utilization in _dbContext.ORRoomUtilizationRecords
            join room in _dbContext.OperatingRooms
                on utilization.ORRoomId equals room.ORRoomId
            where utilization.HospitalId == hospitalId
                  && room.HospitalId == hospitalId
            select new
            {
                utilization,
                room
            };

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(item => item.utilization.WeekStartDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value.Date);
            query = query.Where(item => item.utilization.WeekStartDate <= to);
        }

        if (roomId.HasValue)
        {
            query = query.Where(item => item.utilization.ORRoomId == roomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.utilization.UtilStatus == status);
        }

        return await query
            .OrderByDescending(item => item.utilization.WeekStartDate)
            .ThenBy(item => item.room.RoomName)
            .Select(item => new ORRoomUtilizationRecordDto
            {
                ORRoomUtilizationId = item.utilization.ORRoomUtilizationId,
                HospitalId = item.utilization.HospitalId,
                ORRoomId = item.utilization.ORRoomId,
                RoomName = item.room.RoomName,
                WeekStartDate = item.utilization.WeekStartDate.ToDateTime(TimeOnly.MinValue),
                WeekEndDate = item.utilization.WeekEndDate.ToDateTime(TimeOnly.MinValue),
                AllocatedMinutes = item.utilization.AllocatedMinutes,
                UsedMinutes = item.utilization.UsedMinutes,
                UtilizationPercent = item.utilization.UtilizationPct ?? 0,
                UtilizationStatus = item.utilization.UtilStatus,
                CalculatedAt = item.utilization.CalculatedAt
            })
            .ToListAsync();
    }

    public async Task<ORRoomUtilizationSummaryDto> GetORRoomUtilizationSummaryAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var records = await GetORRoomUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            null,
            null);

        if (records.Count == 0)
        {
            return new ORRoomUtilizationSummaryDto();
        }

        var totalAllocatedMinutes = records.Sum(record => record.AllocatedMinutes);
        var totalUsedMinutes = records.Sum(record => record.UsedMinutes);

        return new ORRoomUtilizationSummaryDto
        {
            TotalRooms = records.Count,
            TotalAllocatedMinutes = totalAllocatedMinutes,
            TotalUsedMinutes = totalUsedMinutes,
            AverageUtilizationPercent = totalAllocatedMinutes == 0
                ? 0
                : Math.Round(totalUsedMinutes * 100m / totalAllocatedMinutes, 2),
            GoodRooms = records.Count(record => record.UtilizationStatus == "Good"),
            ModerateRooms = records.Count(record => record.UtilizationStatus == "Moderate"),
            UnderutilizedRooms = records.Count(record => record.UtilizationStatus == "Underutilized"),
            UnusedRooms = records.Count(record => record.UtilizationStatus == "Unused")
        };
    }

    public async Task<List<UnderutilizedORRoomDto>> GetUnderutilizedORRoomsAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var records = await GetORRoomUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            null,
            null);

        return records
            .Where(record =>
                record.UtilizationStatus == "Underutilized" ||
                record.UtilizationStatus == "Unused")
            .OrderBy(record => record.UtilizationPercent)
            .ThenBy(record => record.RoomName)
            .Select(record => new UnderutilizedORRoomDto
            {
                ORRoomUtilizationId = record.ORRoomUtilizationId,
                ORRoomId = record.ORRoomId,
                RoomName = record.RoomName,
                WeekStartDate = record.WeekStartDate,
                WeekEndDate = record.WeekEndDate,
                AllocatedMinutes = record.AllocatedMinutes,
                UsedMinutes = record.UsedMinutes,
                UtilizationPercent = record.UtilizationPercent,
                UtilizationStatus = record.UtilizationStatus
            })
            .ToList();
    }

    public async Task<bool> ORRoomExistsAsync(
        int hospitalId,
        int roomId)
    {
        return await _dbContext.OperatingRooms
            .AnyAsync(room =>
                room.HospitalId == hospitalId &&
                room.ORRoomId == roomId &&
                room.IsActive);
    }

    public async Task<int> CalculateORRoomWeeklyUtilizationAsync(
        int hospitalId,
        int? roomId,
        DateTime weekStartDate)
    {
        var hospitalIdParameter = new SqlParameter("@HospitalId", hospitalId);

        var roomIdParameter = new SqlParameter("@ORRoomId", System.Data.SqlDbType.Int)
        {
            Value = roomId.HasValue ? roomId.Value : DBNull.Value
        };

        var weekStartDateParameter = new SqlParameter("@WeekStartDate", System.Data.SqlDbType.Date)
        {
            Value = weekStartDate.Date
        };

        await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC analytics.usp_CalculateORRoomWeeklyUtilization @HospitalId, @ORRoomId, @WeekStartDate",
            hospitalIdParameter,
            roomIdParameter,
            weekStartDateParameter);

        var weekStartDateOnly = DateOnly.FromDateTime(weekStartDate.Date);

        if (roomId.HasValue)
        {
            return await _dbContext.ORRoomUtilizationRecords
                .CountAsync(record =>
                    record.HospitalId == hospitalId &&
                    record.ORRoomId == roomId.Value &&
                    record.WeekStartDate == weekStartDateOnly);
        }

        return await _dbContext.ORRoomUtilizationRecords
            .Join(
                _dbContext.OperatingRooms,
                utilization => utilization.ORRoomId,
                room => room.ORRoomId,
                (utilization, room) => new
                {
                    utilization,
                    room
                })
            .CountAsync(item =>
                item.utilization.HospitalId == hospitalId &&
                item.utilization.WeekStartDate == weekStartDateOnly &&
                item.room.HospitalId == hospitalId &&
                item.room.IsActive);
    }

    public async Task<UtilizationSummaryDto> GetSummaryAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var records = await GetUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            null,
            null,
            null);

        if (records.Count == 0)
        {
            return new UtilizationSummaryDto();
        }

        return new UtilizationSummaryDto
        {
            TotalBlocks = records.Count,
            TotalAllocatedMinutes = records.Sum(record => record.AllocatedMinutes),
            TotalUsedMinutes = records.Sum(record => record.UsedMinutes),
            AverageUtilizationPercent = Math.Round(records.Average(record => record.UtilizationPercent), 2),

            GoodBlocks = records.Count(record => record.UtilizationStatus == "Good"),
            ModerateBlocks = records.Count(record => record.UtilizationStatus == "Moderate"),
            UnderutilizedBlocks = records.Count(record => record.UtilizationStatus == "Underutilized"),
            UnusedBlocks = records.Count(record => record.UtilizationStatus == "Unused")
        };
    }

    public async Task<List<UnderutilizedBlockDto>> GetUnderutilizedBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var records = await GetUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            null,
            null,
            null);

        return records
            .Where(record =>
                record.UtilizationStatus == "Underutilized" ||
                record.UtilizationStatus == "Unused")
            .Select(record => new UnderutilizedBlockDto
            {
                BlockId = record.BlockId,
                SurgeonId = record.SurgeonId,
                SurgeonName = record.SurgeonName,

                ORRoomId = record.ORRoomId,
                RoomName = record.RoomName,

                BlockDate = record.BlockDate,

                AllocatedMinutes = record.AllocatedMinutes,
                UsedMinutes = record.UsedMinutes,
                UtilizationPercent = record.UtilizationPercent,
                UtilizationStatus = record.UtilizationStatus
            })
            .OrderBy(record => record.UtilizationPercent)
            .ToList();
    }

    public async Task<bool> BlockExistsAsync(int hospitalId, int blockId)
    {
        return await _dbContext.BlockAllocations
            .AnyAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId);
    }

    public async Task CalculateBlockUtilizationAsync(int blockId)
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            "EXEC analytics.usp_CalculateBlockUtilization @BlockId",
            new SqlParameter("@BlockId", blockId));
    }

    public async Task<List<int>> GetBlockIdsForDateRangeAsync(
        int hospitalId,
        DateTime fromDate,
        DateTime toDate)
    {
        var from = DateOnly.FromDateTime(fromDate.Date);
        var to = DateOnly.FromDateTime(toDate.Date);

        return await _dbContext.BlockAllocations
            .Where(block =>
                block.HospitalId == hospitalId &&
                block.BlockDate >= from &&
                block.BlockDate <= to &&
                block.BlockStatus != "Cancelled")
            .Select(block => block.BlockId)
            .ToListAsync();
    }
}