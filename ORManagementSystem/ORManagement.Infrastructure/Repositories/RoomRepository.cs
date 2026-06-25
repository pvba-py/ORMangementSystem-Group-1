using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Rooms;
using ORManagement.Application.DTOs.Shared;
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
            .Where(room => room.HospitalId == hospitalId && room.IsActive == true)
            .OrderBy(room => room.ORRoomId)
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

    public async Task<PagedResultDto<RoomDto>> GetRoomsPagedAsync(
    int hospitalId,
    bool? isActive,
    int pageNumber,
    int pageSize)
    {
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _dbContext.OperatingRooms
            .Where(room => room.HospitalId == hospitalId);

        if (isActive.HasValue)
        {
            query = query.Where(room => room.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(room => room.ORRoomId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResultDto<RoomDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
    public async Task<List<MyCalendarDto>> GetMyCalendarAsync(
     int hospitalId,
     int userId,
     DateTime fromDate,
     DateTime toDate)
    {
        var surgeonId = await _dbContext.Surgeons
            .Where(surgeon =>
                surgeon.HospitalId == hospitalId &&
                surgeon.UserId == userId &&
                surgeon.IsActive)
            .Select(surgeon => (int?)surgeon.SurgeonId)
            .FirstOrDefaultAsync();

        if (surgeonId is null)
        {
            return new List<MyCalendarDto>();
        }

        var currentSurgeonId = surgeonId.Value;

        var fromDateOnly = DateOnly.FromDateTime(fromDate.Date);
        var toDateOnly = DateOnly.FromDateTime(toDate.Date);

        var assignedBlockIds = await _dbContext.BlockAllocations
            .Where(block =>
                block.HospitalId == hospitalId &&
                block.SurgeonId == currentSurgeonId &&
                block.BlockDate >= fromDateOnly &&
                block.BlockDate <= toDateOnly &&
                block.BlockStatus != "Cancelled")
            .Select(block => block.BlockId)
            .ToListAsync();

        var caseBlockIds = await _dbContext.SurgicalCases
            .Join(
                _dbContext.ORRequests,
                surgicalCase => surgicalCase.RequestId,
                orRequest => orRequest.RequestId,
                (surgicalCase, orRequest) => new
                {
                    surgicalCase,
                    orRequest
                })
            .Where(item =>
                item.surgicalCase.HospitalId == hospitalId &&
                item.orRequest.SurgeonId == currentSurgeonId &&
                item.surgicalCase.CaseStatus != "Cancelled" &&
                item.surgicalCase.ScheduledStart.Date >= fromDate.Date &&
                item.surgicalCase.ScheduledStart.Date <= toDate.Date)
            .Select(item => item.surgicalCase.BlockId)
            .Distinct()
            .ToListAsync();

        var blockIds = assignedBlockIds
            .Concat(caseBlockIds)
            .Distinct()
            .ToList();

        if (blockIds.Count == 0)
        {
            return new List<MyCalendarDto>();
        }

        var blocks = await _dbContext.BlockAllocations
            .Where(block =>
                block.HospitalId == hospitalId &&
                blockIds.Contains(block.BlockId) &&
                block.BlockDate >= fromDateOnly &&
                block.BlockDate <= toDateOnly &&
                block.BlockStatus != "Cancelled")
            .OrderBy(block => block.BlockDate)
            .ThenBy(block => block.StartTime)
            .ToListAsync();

        var roomIds = blocks
            .Select(block => block.ORRoomId)
            .Distinct()
            .ToList();

        var rooms = await _dbContext.OperatingRooms
            .Where(room =>
                room.HospitalId == hospitalId &&
                roomIds.Contains(room.ORRoomId))
            .ToDictionaryAsync(
                room => room.ORRoomId,
                room => room);

        var cases = await _dbContext.SurgicalCases
            .Where(surgicalCase =>
                surgicalCase.HospitalId == hospitalId &&
                blockIds.Contains(surgicalCase.BlockId) &&
                surgicalCase.CaseStatus != "Cancelled")
            .ToListAsync();

        var requestIds = cases
            .Select(surgicalCase => surgicalCase.RequestId)
            .Distinct()
            .ToList();

        var requests = await _dbContext.ORRequests
            .Where(orRequest => requestIds.Contains(orRequest.RequestId))
            .ToDictionaryAsync(
                orRequest => orRequest.RequestId,
                orRequest => orRequest);

        var currentSurgeonCases = cases
            .Where(surgicalCase =>
                requests.TryGetValue(surgicalCase.RequestId, out var orRequest) &&
                orRequest.SurgeonId == currentSurgeonId)
            .ToList();

        var patientIds = requests.Values
            .Where(orRequest => orRequest.SurgeonId == currentSurgeonId)
            .Select(orRequest => orRequest.PatientId)
            .Distinct()
            .ToList();

        var patients = await _dbContext.Patients
            .Where(patient => patientIds.Contains(patient.PatientId))
            .ToDictionaryAsync(
                patient => patient.PatientId,
                patient => patient);

        var surgeonIds = blocks
            .Where(block => block.SurgeonId.HasValue)
            .Select(block => block.SurgeonId!.Value)
            .Concat(requests.Values.Select(orRequest => orRequest.SurgeonId))
            .Distinct()
            .ToList();

        var surgeonNames = await _dbContext.Surgeons
            .Where(surgeon => surgeonIds.Contains(surgeon.SurgeonId))
            .Join(
                _dbContext.Users,
                surgeon => surgeon.UserId,
                user => user.UserId,
                (surgeon, user) => new
                {
                    surgeon.SurgeonId,
                    SurgeonName = user.FullName
                })
            .ToDictionaryAsync(
                item => item.SurgeonId,
                item => item.SurgeonName);

        var result = new List<MyCalendarDto>();

        foreach (var block in blocks)
        {
            if (!rooms.TryGetValue(block.ORRoomId, out var room))
            {
                continue;
            }

            var blockCases = currentSurgeonCases
                .Where(surgicalCase => surgicalCase.BlockId == block.BlockId)
                .OrderBy(surgicalCase => surgicalCase.ScheduledStart)
                .ToList();

            var blockOwnerName = block.SurgeonId.HasValue &&
                                 surgeonNames.TryGetValue(block.SurgeonId.Value, out var blockSurgeonName)
                ? blockSurgeonName
                : $"{block.BlockType} Capacity";

            if (blockCases.Count == 0)
            {
                result.Add(new MyCalendarDto
                {
                    BlockId = block.BlockId,
                    ORRoomId = room.ORRoomId,
                    RoomName = room.RoomName,
                    BlockDate = block.BlockDate.ToDateTime(TimeOnly.MinValue),
                    StartTime = block.StartTime,
                    EndTime = block.EndTime,
                    BlockType = block.BlockType,
                    BlockStatus = block.BlockStatus,
                    SurgeonId = block.SurgeonId,
                    SurgeonName = blockOwnerName
                });

                continue;
            }

            foreach (var surgicalCase in blockCases)
            {
                var orRequest = requests[surgicalCase.RequestId];

                patients.TryGetValue(orRequest.PatientId, out var patient);

                var caseSurgeonName = surgeonNames.TryGetValue(orRequest.SurgeonId, out var surgeonName)
                    ? surgeonName
                    : blockOwnerName;

                result.Add(new MyCalendarDto
                {
                    BlockId = block.BlockId,
                    ORRoomId = room.ORRoomId,
                    RoomName = room.RoomName,
                    BlockDate = block.BlockDate.ToDateTime(TimeOnly.MinValue),
                    StartTime = block.StartTime,
                    EndTime = block.EndTime,
                    BlockType = block.BlockType,
                    BlockStatus = block.BlockStatus,

                    SurgeonId = orRequest.SurgeonId,
                    SurgeonName = caseSurgeonName,

                    SurgeryId = surgicalCase.SurgeryId,
                    PatientId = orRequest.PatientId,
                    PatientName = patient?.FullName,
                    PatientMrn = patient?.MRN,
                    SurgeryType = orRequest.SurgeryType,

                    ScheduledStart = surgicalCase.ScheduledStart,
                    ScheduledEnd = surgicalCase.ScheduledEnd,
                    CaseStatus = surgicalCase.CaseStatus
                });
            }
        }

        return result
            .OrderBy(item => item.BlockDate)
            .ThenBy(item => item.StartTime)
            .ThenBy(item => item.ScheduledStart)
            .ToList();
    }
    public async Task<List<CalendarItemDto>> GetCalendarAsync(
     int hospitalId,
     DateTime fromDate,
     DateTime toDate,
     int? roomId)
    {
        var query =
            from room in _dbContext.OperatingRooms
            join block in _dbContext.BlockAllocations
                on room.ORRoomId equals block.ORRoomId
            join surgeon in _dbContext.Surgeons
                on block.SurgeonId equals surgeon.SurgeonId into surgeonJoin
            from surgeon in surgeonJoin.DefaultIfEmpty()
            join user in _dbContext.Users
                on surgeon != null ? surgeon.UserId : 0 equals user.UserId into userJoin
            from user in userJoin.DefaultIfEmpty()
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
                SurgeonName = item.user != null
                    ? item.user.FullName
                    : item.block.BlockType + " Capacity",
                SurgeryId = item.surgicalCase != null ? item.surgicalCase.SurgeryId : null,
                ScheduledStart = item.surgicalCase != null ? item.surgicalCase.ScheduledStart : null,
                ScheduledEnd = item.surgicalCase != null ? item.surgicalCase.ScheduledEnd : null,
                CaseStatus = item.surgicalCase != null ? item.surgicalCase.CaseStatus : null
            })
            .ToListAsync();
    }
}