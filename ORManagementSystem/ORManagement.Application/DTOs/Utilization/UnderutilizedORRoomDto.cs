namespace ORManagement.Application.DTOs.Utilization;

public class UnderutilizedORRoomDto
{
    public int ORRoomUtilizationId { get; set; }

    public int ORRoomId { get; set; }

    public string RoomName { get; set; } = string.Empty;

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public int AllocatedMinutes { get; set; }

    public int UsedMinutes { get; set; }

    public decimal UtilizationPercent { get; set; }

    public string UtilizationStatus { get; set; } = string.Empty;
}