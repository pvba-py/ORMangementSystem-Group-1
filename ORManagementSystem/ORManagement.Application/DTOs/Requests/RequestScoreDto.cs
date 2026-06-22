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
}