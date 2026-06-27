using ORManagement.Application.DTOs.Forecast;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IForecastRepository
{
    Task<List<ForecastRecommendationDto>> GetRecommendationsAsync(
        int hospitalId,
        string? status);

    Task<ForecastRecommendationDto?> GetRecommendationByIdAsync(
        int hospitalId,
        int recId);

    Task<List<ForecastDemandSignalDto>> GetDemandSignalsAsync(int hospitalId);

    Task<int> CreateRecommendationAsync(CreateForecastRecommendationDto request);
    Task<List<SurgeryDurationDto>> GetSurgeryDurationAveragesAsync(int hospitalId);
    Task<bool> UpdateRecommendationStatusAsync(
        int hospitalId,
        int recId,
        string status,
        int reviewedByUserId);
    Task<ForecastSummaryDto> GetForecastSummaryAsync(int hospitalId);
}