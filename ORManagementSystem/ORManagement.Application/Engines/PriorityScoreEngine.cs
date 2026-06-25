namespace ORManagement.Application.Engines;

public class PriorityScoreEngine
{
    public decimal CalculateScore(
        string priority,
        string patientReadiness,
        DateTime createdAt,
        int cyclesWaited,
        int? durationFitScore = null)
    {
        var breakdown = CalculateBreakdown(
            priority,
            patientReadiness,
            createdAt,
            cyclesWaited,
            durationFitScore);

        return breakdown.TotalScore;
    }

    public PriorityScoreBreakdown CalculateBreakdown(
        string priority,
        string patientReadiness,
        DateTime createdAt,
        int cyclesWaited,
        int? durationFitScore = null)
    {
        var normalizedPriority = Normalize(priority);
        var normalizedReadiness = Normalize(patientReadiness);

        var waitingDays = Math.Max(
            0,
            (DateTime.UtcNow - createdAt.ToUniversalTime()).Days);

        var cappedWaitingDays = Math.Min(waitingDays, 30);
        var cappedCyclesWaited = Math.Min(Math.Max(cyclesWaited, 0), 5);
        var cappedFitScore = Math.Clamp(durationFitScore ?? 0, 0, 100);

        var basePriorityScore = GetPriorityScore(normalizedPriority);
        var baseReadinessScore = GetReadinessScore(normalizedReadiness);
        var baseWaitingScore = cappedWaitingDays / 30m * 100m;
        var baseCycleScore = cappedCyclesWaited / 5m * 100m;
        var baseFitScore = cappedFitScore;

        var readinessMultiplier = normalizedReadiness switch
        {
            "notready" => 0.25m,
            "pendingclearance" => 0.75m,
            "ready" => 1.0m,
            _ => 0.5m
        };

        var priorityContribution = basePriorityScore * 0.40m * readinessMultiplier;
        var readinessContribution = baseReadinessScore * 0.25m * readinessMultiplier;
        var waitingContribution = baseWaitingScore * 0.15m * readinessMultiplier;
        var cycleContribution = baseCycleScore * 0.10m * readinessMultiplier;
        var fitContribution = baseFitScore * 0.10m * readinessMultiplier;

        var totalScore =
            priorityContribution +
            readinessContribution +
            waitingContribution +
            cycleContribution +
            fitContribution;

        return new PriorityScoreBreakdown
        {
            PriorityScore = Math.Round(priorityContribution, 2),
            ReadinessScore = Math.Round(readinessContribution, 2),
            WaitingScore = Math.Round(waitingContribution, 2),
            CycleWaitScore = Math.Round(cycleContribution, 2),
            DurationFitScore = Math.Round(fitContribution, 2),
            TotalScore = Math.Round(Math.Clamp(totalScore, 0, 100), 2)
        };
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace(" ", string.Empty).ToLowerInvariant();
    }

    private static decimal GetPriorityScore(string priority)
    {
        return priority switch
        {
            "emergency" => 100m,
            "urgent" => 75m,
            "elective" => 40m,
            _ => 30m
        };
    }

    private static decimal GetReadinessScore(string readiness)
    {
        return readiness switch
        {
            "ready" => 100m,
            "pendingclearance" => 50m,
            "notready" => 0m,
            _ => 25m
        };
    }
    public decimal CalculateHybridScore(
    decimal ruleBasedScore,
    decimal clinicalTextScore,
    decimal ruleWeight = 0.70m,
    decimal clinicalWeight = 0.30m)
    {
        var normalizedRuleScore = Math.Clamp(ruleBasedScore, 0, 100);
        var normalizedClinicalScore = Math.Clamp(clinicalTextScore, 0, 100);

        var finalScore =
            (normalizedRuleScore * ruleWeight) +
            (normalizedClinicalScore * clinicalWeight);

        return Math.Round(Math.Clamp(finalScore, 0, 100), 2);
    }
}


public class PriorityScoreBreakdown
{
    public decimal PriorityScore { get; set; }
    public decimal WaitingScore { get; set; }
    public decimal ReadinessScore { get; set; }
    public decimal CycleWaitScore { get; set; }
    public decimal DurationFitScore { get; set; }
    public decimal TotalScore { get; set; }
}