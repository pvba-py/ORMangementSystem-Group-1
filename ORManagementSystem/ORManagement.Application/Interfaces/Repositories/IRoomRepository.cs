using ORManagement.Application.DTOs.Rooms;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<List<RoomDto>> GetRoomsAsync(int hospitalId);
    Task<RoomDto?> GetRoomByIdAsync(int hospitalId, int roomId);
    Task<bool> RoomNameExistsAsync(int hospitalId, string roomName, int? excludeRoomId = null);

    Task<int> CreateRoomAsync(int hospitalId, CreateRoomRequestDto request);
    Task<bool> UpdateRoomAsync(int hospitalId, int roomId, UpdateRoomRequestDto request, int? modifiedByUserId);
    Task<bool> SoftDeleteRoomAsync(int hospitalId, int roomId);

    Task<List<CalendarItemDto>> GetCalendarAsync(int hospitalId, DateTime fromDate, DateTime toDate, int? roomId);
}