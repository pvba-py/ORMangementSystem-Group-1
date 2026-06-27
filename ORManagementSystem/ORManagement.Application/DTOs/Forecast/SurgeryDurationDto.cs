
namespace ORManagement.Application.DTOs.Forecast;

public class SurgeryDurationDto
{
    public string SurgeryType { get; set; } = string.Empty;

    public int CaseCount { get; set; }

    public decimal AverageDurationMinutes { get; set; }
}
