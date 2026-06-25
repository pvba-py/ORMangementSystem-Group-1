using ORManagement.Application.DTOs.Requests;

namespace ORManagement.Application.Interfaces.Services;

public interface IClinicalTextScoringService
{
    Task<ClinicalTextScoreResultDto> ScoreAsync(
        string? remarks,
        string? surgeryType,
        string? priority,
        string? patientReadiness);
}
