using System.Text.Json;
using ORManagement.Application.DTOs.Forecast;

namespace ORManagement.Application.Engines;

public class ForecastRecommendationEngine
{
    public List<CreateForecastRecommendationDto> GenerateRecommendations(
        List<ForecastDemandSignalDto> demandSignals)
    {
        var recommendations = new List<CreateForecastRecommendationDto>();

        foreach (var signal in demandSignals)
        {
            if (signal.WaitlistedRequests >= 3 && signal.AverageUtilizationPercent >= 80)
            {
                recommendations.Add(new CreateForecastRecommendationDto
                {
                    HospitalId = signal.HospitalId,
                    RuleId = "R2",
                    Description = $"High demand detected for {signal.Specialty}. Consider increasing future block allocation.",
                    EvidenceJson = JsonSerializer.Serialize(new
                    {
                        Rule = "R2",
                        RuleName = "HighDemandIncreaseBlockTime",
                        signal.Specialty,
                        signal.WaitlistedRequests,
                        signal.AverageUtilizationPercent,
                        signal.TotalBlocks,
                        RecommendationType = "IncreaseBlockTime",
                        Trigger = "WaitlistedRequests >= 3 and AverageUtilizationPercent >= 80"
                    })
                });
            }

            if (signal.AverageUtilizationPercent < 50 && signal.TotalBlocks >= 2)
            {
                recommendations.Add(new CreateForecastRecommendationDto
                {
                    HospitalId = signal.HospitalId,
                    RuleId = "R1",
                    Description = $"Low utilization detected for {signal.Specialty}. Consider reducing or reallocating future block time.",
                    EvidenceJson = JsonSerializer.Serialize(new
                    {
                        Rule = "R1",
                        RuleName = "UnderutilizedReduceBlockTime",
                        signal.Specialty,
                        signal.WaitlistedRequests,
                        signal.AverageUtilizationPercent,
                        signal.TotalBlocks,
                        RecommendationType = "ReduceBlockTime",
                        Trigger = "AverageUtilizationPercent < 50 and TotalBlocks >= 2"
                    })
                });
            }

            if (signal.WaitlistedRequests >= 3 && signal.AverageUtilizationPercent < 50)
            {
                recommendations.Add(new CreateForecastRecommendationDto
                {
                    HospitalId = signal.HospitalId,
                    RuleId = "R3",
                    Description = $"Demand exists for {signal.Specialty}, but current utilization is low. Review scheduling efficiency before adding more block time.",
                    EvidenceJson = JsonSerializer.Serialize(new
                    {
                        Rule = "R3",
                        RuleName = "DemandUtilizationMismatch",
                        signal.Specialty,
                        signal.WaitlistedRequests,
                        signal.AverageUtilizationPercent,
                        signal.TotalBlocks,
                        RecommendationType = "ReviewBeforeReallocation",
                        Trigger = "WaitlistedRequests >= 3 and AverageUtilizationPercent < 50"
                    })
                });
            }
        }

        return recommendations;
    }
}