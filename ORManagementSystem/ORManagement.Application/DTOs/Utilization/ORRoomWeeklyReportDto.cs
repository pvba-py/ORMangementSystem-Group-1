namespace ORManagement.Application.DTOs.Utilization;

public class ORRoomWeeklyReportDto
{
    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public DateTime GeneratedAt { get; set; }

    public int CalculatedRooms { get; set; }

    public ORRoomUtilizationSummaryDto Summary { get; set; } = new();

    public List<ORRoomUtilizationRecordDto> Rooms { get; set; } = new();

    public List<UnderutilizedORRoomDto> UnderutilizedRooms { get; set; } = new();
}