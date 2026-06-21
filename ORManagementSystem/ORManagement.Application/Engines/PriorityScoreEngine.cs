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
        var normalizedPriority = Normalize(priority);
        var normalizedReadiness = Normalize(patientReadiness);

        var waitingDays = Math.Max(
            0,
            (DateTime.UtcNow - createdAt.ToUniversalTime()).Days);

        var cappedWaitingDays = Math.Min(waitingDays, 30);
        var cappedCyclesWaited = Math.Min(Math.Max(cyclesWaited, 0), 5);
        var cappedFitScore = Math.Clamp(durationFitScore ?? 0, 0, 100);

        var priorityScore = GetPriorityScore(normalizedPriority);
        var readinessScore = GetReadinessScore(normalizedReadiness);
        var waitingScore = cappedWaitingDays / 30m * 100m;
        var cycleScore = cappedCyclesWaited / 5m * 100m;
        var fitScore = cappedFitScore;

        var readinessMultiplier = normalizedReadiness switch
        {
            "notready" => 0.25m,
            "pendingclearance" => 0.75m,
            "ready" => 1.0m,
            _ => 0.5m
        };

        var rawScore =
            priorityScore * 0.40m +
            readinessScore * 0.25m +
            waitingScore * 0.15m +
            cycleScore * 0.10m +
            fitScore * 0.10m;

        var finalScore = rawScore * readinessMultiplier;

        return Math.Round(finalScore, 2);
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
}