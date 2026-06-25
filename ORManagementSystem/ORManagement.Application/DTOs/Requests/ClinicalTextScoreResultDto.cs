namespace ORManagement.Application.DTOs.Requests;

public class ClinicalTextScoreResultDto
{
    public decimal ClinicalScore { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public bool UsedFallback { get; set; }
}