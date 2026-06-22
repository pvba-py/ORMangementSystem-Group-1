namespace ORManagement.Application.DTOs.Utilization;

public class UnderutilizedBlockDto
{
    public int BlockId { get; set; }
    public int? SurgeonId { get; set; }
    public string SurgeonName { get; set; } = string.Empty;

    public int ORRoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public DateTime BlockDate { get; set; }

    public int AllocatedMinutes { get; set; }
    public int UsedMinutes { get; set; }
    public decimal UtilizationPercent { get; set; }

    public string UtilizationStatus { get; set; } = string.Empty;
}