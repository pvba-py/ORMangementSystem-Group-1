namespace ORManagement.Application.DTOs.Rooms;

public class RoomDto
{
    public int ORRoomId { get; set; }
    public int HospitalId { get; set; }

    public string RoomName { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}