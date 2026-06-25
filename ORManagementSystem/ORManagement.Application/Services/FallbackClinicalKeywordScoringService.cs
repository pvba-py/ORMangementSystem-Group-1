using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class FallbackClinicalKeywordScoringService : IClinicalTextScoringService
{
    public Task<ClinicalTextScoreResultDto> ScoreAsync(
        string? remarks,
        string? surgeryType,
        string? priority,
        string? patientReadiness)
    {
        var text = $"{remarks} {surgeryType} {priority} {patientReadiness}"
            .ToLowerInvariant();

        decimal score = 30;
        var reasons = new List<string>();

        if (text.Contains("emergency") ||
            text.Contains("life threatening") ||
            text.Contains("unstable") ||
            text.Contains("bleeding") ||
            text.Contains("sepsis") ||
            text.Contains("shock"))
        {
            score += 35;
            reasons.Add("Critical clinical terms detected.");
        }

        if (text.Contains("progressive") ||
            text.Contains("worsening") ||
            text.Contains("neurological deficit") ||
            text.Contains("severe pain") ||
            text.Contains("deterioration") ||
            text.Contains("loss of function"))
        {
            score += 25;
            reasons.Add("Progressive or severe symptoms detected.");
        }

        if (text.Contains("urgent") ||
            text.Contains("delay") ||
            text.Contains("risk") ||
            text.Contains("complication") ||
            text.Contains("high risk"))
        {
            score += 15;
            reasons.Add("Delay-risk language detected.");
        }

        if (string.Equals(priority, "Emergency", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
            reasons.Add("Emergency priority provided.");
        }

        if (string.Equals(patientReadiness, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
            reasons.Add("Patient is ready for surgery.");
        }

        score = Math.Min(score, 100);

        return Task.FromResult(new ClinicalTextScoreResultDto
        {
            ClinicalScore = Math.Round(score, 2),
            Explanation = reasons.Count > 0
                ? string.Join(" ", reasons)
                : "No high-severity clinical language detected.",
            ModelName = "FallbackClinicalKeywordScorer",
            UsedFallback = true
        });
    }
}