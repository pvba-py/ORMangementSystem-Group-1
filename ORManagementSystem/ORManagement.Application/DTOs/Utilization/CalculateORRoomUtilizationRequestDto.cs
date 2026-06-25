namespace ORManagement.Application.DTOs.Utilization;

public class CalculateORRoomUtilizationRequestDto
{
    public int? ORRoomId { get; set; }

    public DateTime? WeekStartDate { get; set; }
}
