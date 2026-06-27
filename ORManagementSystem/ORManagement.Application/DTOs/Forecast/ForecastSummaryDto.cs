namespace ORManagement.Application.DTOs.Forecast;

public class ForecastSummaryDto
{
    public int PredictedCasesNextWeek { get; set; }

    public decimal PredictedOrHoursNextWeek { get; set; }

    public decimal AvailableBlockHoursNextWeek { get; set; }

    public decimal CapacityGapHours { get; set; }

    public List<SurgeryTypeForecastDto> SurgeryTypeForecasts { get; set; } = new();

    public List<WeeklyDemandTrendDto> WeeklyDemandTrend { get; set; } = new();

    public List<SpecialtyCapacityGapDto> SpecialtyCapacityGaps { get; set; } = new();
}

public class SurgeryTypeForecastDto
{
    public string SurgeryType { get; set; } = string.Empty;

    public int HistoricalCaseCount { get; set; }

    public decimal AverageDurationMinutes { get; set; }

    public decimal ForecastedCasesNextWeek { get; set; }

    public decimal ForecastedHoursNextWeek { get; set; }
}

public class WeeklyDemandTrendDto
{
    public DateTime WeekStartDate { get; set; }

    public int ActualCases { get; set; }

    public decimal? ForecastedCases { get; set; }
}

public class SpecialtyCapacityGapDto
{
    public string Specialty { get; set; } = string.Empty;

    public decimal ForecastedHours { get; set; }

    public decimal AvailableBlockHours { get; set; }

    public decimal GapHours { get; set; }
}