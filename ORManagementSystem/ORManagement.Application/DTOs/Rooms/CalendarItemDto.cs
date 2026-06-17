namespace ORManagement.Application.DTOs.Rooms;

public class CalendarItemDto
{
    public int BlockId { get; set; }
    public int ORRoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;
    public DateTime BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string BlockStatus { get; set; } = string.Empty;
    public string SurgeonName { get; set; } = string.Empty;

    public int? SurgeryId { get; set; }
    public DateTime? ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public string? CaseStatus { get; set; }
}
