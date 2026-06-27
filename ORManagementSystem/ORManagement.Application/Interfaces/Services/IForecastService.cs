using ORManagement.Application.DTOs.Forecast;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IForecastService
{
    Task<ServiceResultDto<List<ForecastRecommendationDto>>> GetRecommendationsAsync(
        int hospitalId,
        string? status);

    Task<ServiceResultDto<List<ForecastDemandSignalDto>>> GetDemandAsync(int hospitalId);

    Task<ServiceResultDto<int>> GenerateRecommendationsAsync(
        int hospitalId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateRecommendationStatusAsync(
        int hospitalId,
        int recId,
        int userId,
        string roleName,
        UpdateForecastRecommendationStatusDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<List<SurgeryDurationDto>>> GetSurgeryDurationAveragesAsync(
    int hospitalId);
    Task<ServiceResultDto<ForecastSummaryDto>> GetForecastSummaryAsync(int hospitalId);
}