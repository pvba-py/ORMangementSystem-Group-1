namespace ORManagement.Application.DTOs.Requests;

public class CurrentSchedulingCycleDto
{
    public int CycleId { get; set; }

    public DateTime WeekStartDate { get; set; }

    public DateTime WeekEndDate { get; set; }

    public string CycleStatus { get; set; } = string.Empty;
}