using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Rooms;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<RoomService> _logger;

    public RoomService(
        IRoomRepository roomRepository,
        IAuditRepository auditRepository,
        ILogger<RoomService> logger)
    {
        _roomRepository = roomRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<RoomDto>>> GetRoomsAsync(int hospitalId)
    {
        var rooms = await _roomRepository.GetRoomsAsync(hospitalId);

        return ServiceResultDto<List<RoomDto>>.Ok(rooms);
    }

    public async Task<ServiceResultDto<RoomDto>> GetRoomByIdAsync(int hospitalId, int roomId)
    {
        var room = await _roomRepository.GetRoomByIdAsync(hospitalId, roomId);

        if (room is null)
        {
            return ServiceResultDto<RoomDto>.Fail("ROOM_NOT_FOUND", "Operating room was not found.");
        }

        return ServiceResultDto<RoomDto>.Ok(room);
    }

    public async Task<ServiceResultDto<PagedResultDto<RoomDto>>> GetRoomsPagedAsync(
    int hospitalId,
    bool? isActive,
    int pageNumber,
    int pageSize)
    {
        var rooms = await _roomRepository.GetRoomsPagedAsync(
            hospitalId,
            isActive,
            pageNumber,
            pageSize);

        return ServiceResultDto<PagedResultDto<RoomDto>>.Ok(rooms);
    }
    public async Task<ServiceResultDto<int>> CreateRoomAsync(
        int hospitalId,
        int userId,
        string roleName,
        CreateRoomRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        var roomName = request.RoomName.Trim();

        var exists = await _roomRepository.RoomNameExistsAsync(hospitalId, roomName);
        if (exists)
        {
            return ServiceResultDto<int>.Fail("ROOM_ALREADY_EXISTS", "A room with this name already exists.");
        }

        var roomId = await _roomRepository.CreateRoomAsync(hospitalId, request);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RoomCreated",
            Entity = "OperatingRooms",
            EntityId = roomId,
            NewValue = roomName,
            Remarks = "Operating room created.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("Room created. RoomId: {RoomId}, UserId: {UserId}", roomId, userId);

        return ServiceResultDto<int>.Ok(roomId, "Room created successfully.");
    }

    public async Task<ServiceResultDto> UpdateRoomAsync(
        int hospitalId,
        int roomId,
        int userId,
        string roleName,
        UpdateRoomRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        var existingRoom = await _roomRepository.GetRoomByIdAsync(hospitalId, roomId);

        if (existingRoom is null)
        {
            return ServiceResultDto.Fail("ROOM_NOT_FOUND", "Operating room was not found.");
        }

        var roomNameExists = await _roomRepository.RoomNameExistsAsync(
            hospitalId,
            request.RoomName.Trim(),
            roomId);

        if (roomNameExists)
        {
            return ServiceResultDto.Fail("ROOM_ALREADY_EXISTS", "Another room with this name already exists.");
        }

        var updated = await _roomRepository.UpdateRoomAsync(
            hospitalId,
            roomId,
            request,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail("ROOM_UPDATE_FAILED", "Room could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RoomUpdated",
            Entity = "OperatingRooms",
            EntityId = roomId,
            OldValue = $"{existingRoom.RoomName}|{existingRoom.RoomType}|{existingRoom.Location}|{existingRoom.IsActive}",
            NewValue = $"{request.RoomName}|{request.RoomType}|{request.Location}|{request.IsActive}",
            Remarks = "Operating room updated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("Room updated. RoomId: {RoomId}, UserId: {UserId}", roomId, userId);

        return ServiceResultDto.Ok("Room updated successfully.");
    }

    public async Task<ServiceResultDto> DeleteRoomAsync(
        int hospitalId,
        int roomId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var existingRoom = await _roomRepository.GetRoomByIdAsync(hospitalId, roomId);

        if (existingRoom is null)
        {
            return ServiceResultDto.Fail("ROOM_NOT_FOUND", "Operating room was not found.");
        }

        var deleted = await _roomRepository.SoftDeleteRoomAsync(hospitalId, roomId);

        if (!deleted)
        {
            return ServiceResultDto.Fail("ROOM_DELETE_FAILED", "Room could not be deactivated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "RoomDeactivated",
            Entity = "OperatingRooms",
            EntityId = roomId,
            OldValue = "Active",
            NewValue = "Inactive",
            Remarks = $"Room {existingRoom.RoomName} was deactivated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("Room deactivated. RoomId: {RoomId}, UserId: {UserId}", roomId, userId);

        return ServiceResultDto.Ok("Room deactivated successfully.");
    }

    public async Task<ServiceResultDto<List<CalendarItemDto>>> GetCalendarAsync(
        int hospitalId,
        DateTime fromDate,
        DateTime toDate,
        int? roomId)
    {
        if (fromDate.Date > toDate.Date)
        {
            return ServiceResultDto<List<CalendarItemDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var items = await _roomRepository.GetCalendarAsync(hospitalId, fromDate, toDate, roomId);

        return ServiceResultDto<List<CalendarItemDto>>.Ok(items);
    }
    public async Task<ServiceResultDto<List<MyCalendarDto>>> GetMyCalendarAsync(
    int hospitalId,
    int userId,
    DateTime fromDate,
    DateTime toDate)
    {
        if (fromDate.Date > toDate.Date)
        {
            return ServiceResultDto<List<MyCalendarDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var items = await _roomRepository.GetMyCalendarAsync(
            hospitalId,
            userId,
            fromDate,
            toDate);

        return ServiceResultDto<List<MyCalendarDto>>.Ok(items);
    }
}