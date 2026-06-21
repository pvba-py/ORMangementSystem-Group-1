using System.Text.Json;
using ORManagement.Application.DTOs.Forecast;

namespace ORManagement.Application.Engines;

public class ForecastRecommendationEngine
{
    private const int HighWaitlistThreshold = 3;
    private const decimal HighUtilizationThreshold = 80m;
    private const decimal LowUtilizationThreshold = 50m;
    private const int MinimumBlocksForReduction = 2;

    public List<CreateForecastRecommendationDto> GenerateRecommendations(
        List<ForecastDemandSignalDto> demandSignals)
    {
        var recommendations = new List<CreateForecastRecommendationDto>();

        if (demandSignals == null || demandSignals.Count == 0)
        {
            return recommendations;
        }

        foreach (var signal in demandSignals)
        {
            var recommendation = GenerateRecommendationForSignal(signal);

            if (recommendation != null)
            {
                recommendations.Add(recommendation);
            }
        }

        return recommendations;
    }

    private CreateForecastRecommendationDto? GenerateRecommendationForSignal(
        ForecastDemandSignalDto signal)
    {
        var waitlistedRequests = Math.Max(signal.WaitlistedRequests, 0);
        var utilization = Math.Clamp(signal.AverageUtilizationPercent, 0, 100);
        var totalBlocks = Math.Max(signal.TotalBlocks, 0);

        var hasHighWaitlist = waitlistedRequests >= HighWaitlistThreshold;
        var hasHighUtilization = utilization >= HighUtilizationThreshold;
        var hasLowUtilization = utilization < LowUtilizationThreshold;
        var hasEnoughBlocksToReduce = totalBlocks >= MinimumBlocksForReduction;

        if (hasHighWaitlist && hasLowUtilization)
        {
            return CreateRecommendation(
                signal,
                ruleId: "R3",
                ruleName: "DemandUtilizationMismatch",
                recommendationType: "ReviewSchedulingEfficiency",
                severity: CalculateSeverity(waitlistedRequests, utilization, totalBlocks),
                description: $"Demand exists for {signal.Specialty}, but utilization is low. Review scheduling efficiency, cancellations, block fragmentation, and case duration estimates before changing block allocation.",
                trigger: "WaitlistedRequests >= 3 and AverageUtilizationPercent < 50"
            );
        }

        if (hasHighWaitlist && hasHighUtilization)
        {
            return CreateRecommendation(
                signal,
                ruleId: "R2",
                ruleName: "HighDemandIncreaseBlockTime",
                recommendationType: "IncreaseBlockTime",
                severity: CalculateSeverity(waitlistedRequests, utilization, totalBlocks),
                description: $"High demand detected for {signal.Specialty}. Consider increasing future block allocation or adding overflow capacity.",
                trigger: "WaitlistedRequests >= 3 and AverageUtilizationPercent >= 80"
            );
        }

        if (hasLowUtilization && hasEnoughBlocksToReduce)
        {
            return CreateRecommendation(
                signal,
                ruleId: "R1",
                ruleName: "UnderutilizedReduceBlockTime",
                recommendationType: "ReduceOrReallocateBlockTime",
                severity: CalculateSeverity(waitlistedRequests, utilization, totalBlocks),
                description: $"Low utilization detected for {signal.Specialty}. Consider reducing or reallocating future block time.",
                trigger: "AverageUtilizationPercent < 50 and TotalBlocks >= 2"
            );
        }

        return null;
    }

    private static CreateForecastRecommendationDto CreateRecommendation(
        ForecastDemandSignalDto signal,
        string ruleId,
        string ruleName,
        string recommendationType,
        decimal severity,
        string description,
        string trigger)
    {
        return new CreateForecastRecommendationDto
        {
            HospitalId = signal.HospitalId,
            RuleId = ruleId,
            Description = description,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                Rule = ruleId,
                RuleName = ruleName,
                signal.Specialty,
                signal.WaitlistedRequests,
                signal.AverageUtilizationPercent,
                signal.TotalBlocks,
                RecommendationType = recommendationType,
                SeverityScore = severity,
                Trigger = trigger,
                GeneratedAtUtc = DateTime.UtcNow
            })
        };
    }

    private static decimal CalculateSeverity(
        int waitlistedRequests,
        decimal utilization,
        int totalBlocks)
    {
        var waitlistPressure = Math.Min(waitlistedRequests, 10) / 10m * 50m;
        var utilizationPressure = Math.Abs(utilization - 65m) / 65m * 30m;
        var blockPressure = Math.Min(totalBlocks, 10) / 10m * 20m;

        var severity = waitlistPressure + utilizationPressure + blockPressure;

        return Math.Round(Math.Clamp(severity, 0, 100), 2);
    }
}