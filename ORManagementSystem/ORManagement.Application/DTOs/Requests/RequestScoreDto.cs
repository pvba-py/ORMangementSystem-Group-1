namespace ORManagement.Application.DTOs.Requests;

public class RequestScoreDto
{
    public int RequestId { get; set; }

    public decimal PriorityScore { get; set; }
    public decimal WaitingScore { get; set; }
    public decimal ReadinessScore { get; set; }
    public decimal CycleWaitScore { get; set; }
    public decimal DurationFitScore { get; set; }

    public decimal TotalScore { get; set; }

    public bool IsStarved { get; set; }
    public decimal RuleBasedScore { get; set; }

    public decimal ClinicalTextScore { get; set; }

    public decimal FinalPriorityScore { get; set; }

    public string ClinicalTextExplanation { get; set; } = string.Empty;

    public string ClinicalScoringModel { get; set; } = string.Empty;

    public bool ClinicalScoringUsedFallback { get; set; }
}