using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Rooms;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly ORManagementDbContext _dbContext;

    public RoomRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RoomDto>> GetRoomsAsync(int hospitalId)
    {
        return await _dbContext.OperatingRooms
            .Where(room => room.HospitalId == hospitalId)
            .OrderBy(room => room.RoomName)
            .Select(room => new RoomDto
            {
                ORRoomId = room.ORRoomId,
                HospitalId = room.HospitalId,
                RoomName = room.RoomName,
                RoomType = room.RoomType,
                Location = room.Location,
                IsActive = room.IsActive
            })
            .ToListAsync();
    }

    public async Task<RoomDto?> GetRoomByIdAsync(int hospitalId, int roomId)
    {
        return await _dbContext.OperatingRooms
            .Where(room => room.HospitalId == hospitalId && room.ORRoomId == roomId)
            .Select(room => new RoomDto
            {
                ORRoomId = room.ORRoomId,
                HospitalId = room.HospitalId,
                RoomName = room.RoomName,
                RoomType = room.RoomType,
                Location = room.Location,
                IsActive = room.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> RoomNameExistsAsync(int hospitalId, string roomName, int? excludeRoomId = null)
    {
        var query = _dbContext.OperatingRooms
            .Where(room =>
                room.HospitalId == hospitalId &&
                room.RoomName == roomName);

        if (excludeRoomId.HasValue)
        {
            query = query.Where(room => room.ORRoomId != excludeRoomId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CreateRoomAsync(int hospitalId, CreateRoomRequestDto request)
    {
        var room = new OperatingRoom
        {
            HospitalId = hospitalId,
            RoomName = request.RoomName.Trim(),
            RoomType = request.RoomType.Trim(),
            Location = request.Location.Trim(),
            IsActive = true
        };

        await _dbContext.OperatingRooms.AddAsync(room);
        await _dbContext.SaveChangesAsync();

        return room.ORRoomId;
    }

    public async Task<bool> UpdateRoomAsync(
        int hospitalId,
        int roomId,
        UpdateRoomRequestDto request,
        int? modifiedByUserId)
    {
        var room = await _dbContext.OperatingRooms
            .FirstOrDefaultAsync(room =>
                room.HospitalId == hospitalId &&
                room.ORRoomId == roomId);

        if (room is null)
        {
            return false;
        }

        room.RoomName = request.RoomName.Trim();
        room.RoomType = request.RoomType.Trim();
        room.Location = request.Location.Trim();
        room.IsActive = request.IsActive;

        if (!request.IsActive)
        {
            room.DeactivatedAt = DateTime.UtcNow;
        }
        else
        {
            room.DeactivatedAt = null;
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SoftDeleteRoomAsync(int hospitalId, int roomId)
    {
        var room = await _dbContext.OperatingRooms
            .FirstOrDefaultAsync(room =>
                room.HospitalId == hospitalId &&
                room.ORRoomId == roomId);

        if (room is null)
        {
            return false;
        }

        room.IsActive = false;
        room.DeactivatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<CalendarItemDto>> GetCalendarAsync(
        int hospitalId,
        DateTime fromDate,
        DateTime toDate,
        int? roomId)
    {
        var query =
            from room in _dbContext.OperatingRooms
            join block in _dbContext.BlockAllocations on room.ORRoomId equals block.ORRoomId
            join surgeon in _dbContext.Surgeons on block.SurgeonId equals surgeon.SurgeonId
            join user in _dbContext.Users on surgeon.UserId equals user.UserId
            join surgicalCase in _dbContext.SurgicalCases
                on block.BlockId equals surgicalCase.BlockId into caseJoin
            from surgicalCase in caseJoin.DefaultIfEmpty()
            where room.HospitalId == hospitalId
                  && room.IsActive
                  && block.BlockDate >= DateOnly.FromDateTime(fromDate.Date)
                  && block.BlockDate <= DateOnly.FromDateTime(toDate.Date)
                  && block.BlockStatus != "Cancelled"
            select new
            {
                room,
                block,
                user,
                surgicalCase
            };

        if (roomId.HasValue)
        {
            query = query.Where(item => item.room.ORRoomId == roomId.Value);
        }

        return await query
            .OrderBy(item => item.block.BlockDate)
            .ThenBy(item => item.block.StartTime)
            .Select(item => new CalendarItemDto
            {
                BlockId = item.block.BlockId,
                ORRoomId = item.room.ORRoomId,
                RoomName = item.room.RoomName,
                BlockDate = item.block.BlockDate.ToDateTime(TimeOnly.MinValue),
                StartTime = item.block.StartTime,
                EndTime = item.block.EndTime,
                BlockStatus = item.block.BlockStatus,
                SurgeonName = item.user.FullName,
                SurgeryId = item.surgicalCase != null ? item.surgicalCase.SurgeryId : null,
                ScheduledStart = item.surgicalCase != null ? item.surgicalCase.ScheduledStart : null,
                ScheduledEnd = item.surgicalCase != null ? item.surgicalCase.ScheduledEnd : null,
                CaseStatus = item.surgicalCase != null ? item.surgicalCase.CaseStatus : null
            })
            .ToListAsync();
    }
}