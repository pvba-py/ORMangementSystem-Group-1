namespace ORManagement.Application.DTOs.Utilization;

public class ORRoomUtilizationSummaryDto
{
    public int TotalRooms { get; set; }

    public int TotalAllocatedMinutes { get; set; }

    public int TotalUsedMinutes { get; set; }

    public decimal AverageUtilizationPercent { get; set; }

    public int GoodRooms { get; set; }

    public int ModerateRooms { get; set; }

    public int UnderutilizedRooms { get; set; }

    public int UnusedRooms { get; set; }
}