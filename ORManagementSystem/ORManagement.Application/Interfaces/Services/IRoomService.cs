using ORManagement.Application.DTOs.Rooms;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IRoomService
{
    Task<ServiceResultDto<List<RoomDto>>> GetRoomsAsync(int hospitalId);

    Task<ServiceResultDto<PagedResultDto<RoomDto>>> GetRoomsPagedAsync(
            int hospitalId,
            bool? isActive,
            int pageNumber,
            int pageSize);

    Task<ServiceResultDto<RoomDto>> GetRoomByIdAsync(int hospitalId, int roomId);

    Task<ServiceResultDto<int>> CreateRoomAsync(
        int hospitalId,
        int userId,
        string roleName,
        CreateRoomRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateRoomAsync(
        int hospitalId,
        int roomId,
        int userId,
        string roleName,
        UpdateRoomRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> DeleteRoomAsync(
        int hospitalId,
        int roomId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<List<CalendarItemDto>>> GetCalendarAsync(
        int hospitalId,
        DateTime fromDate,
        DateTime toDate,
        int? roomId);


    Task<ServiceResultDto<List<MyCalendarDto>>> GetMyCalendarAsync(
        int hospitalId,
        int userId,
        DateTime fromDate,
        DateTime toDate);

}