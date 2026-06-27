using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Forecast;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Engines;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class ForecastService : IForecastService
{
    private readonly IForecastRepository _forecastRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ForecastRecommendationEngine _forecastRecommendationEngine;
    private readonly ILogger<ForecastService> _logger;

    private static readonly HashSet<string> AllowedStatuses = new()
    {
        "Pending",
        "Approved",
        "Rejected",
        "Modified"
    };

    public ForecastService(
        IForecastRepository forecastRepository,
        IAuditRepository auditRepository,
        ForecastRecommendationEngine forecastRecommendationEngine,
        ILogger<ForecastService> logger)
    {
        _forecastRepository = forecastRepository;
        _auditRepository = auditRepository;
        _forecastRecommendationEngine = forecastRecommendationEngine;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<ForecastRecommendationDto>>> GetRecommendationsAsync(
        int hospitalId,
        string? status)
    {
        var recommendations = await _forecastRepository.GetRecommendationsAsync(
            hospitalId,
            status);

        return ServiceResultDto<List<ForecastRecommendationDto>>.Ok(recommendations);
    }

    public async Task<ServiceResultDto<List<ForecastDemandSignalDto>>> GetDemandAsync(int hospitalId)
    {
        var demand = await _forecastRepository.GetDemandSignalsAsync(hospitalId);

        return ServiceResultDto<List<ForecastDemandSignalDto>>.Ok(demand);
    }

    public async Task<ServiceResultDto<int>> GenerateRecommendationsAsync(
        int hospitalId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var demandSignals = await _forecastRepository.GetDemandSignalsAsync(hospitalId);

        var recommendations = _forecastRecommendationEngine.GenerateRecommendations(demandSignals);

        var createdCount = 0;

        foreach (var recommendation in recommendations)
        {
            await _forecastRepository.CreateRecommendationAsync(recommendation);
            createdCount++;
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "ForecastRecommendationsGenerated",
            Entity = "ForecastRecommendations",
            EntityId = null,
            NewValue = createdCount.ToString(),
            Remarks = "Rule-based forecast recommendations generated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Forecast recommendations generated. Count: {Count}, UserId: {UserId}",
            createdCount,
            userId);

        return ServiceResultDto<int>.Ok(
            createdCount,
            "Forecast recommendations generated successfully.");
    }

    public async Task<ServiceResultDto> UpdateRecommendationStatusAsync(
        int hospitalId,
        int recId,
        int userId,
        string roleName,
        UpdateForecastRecommendationStatusDto request,
        string? ipAddress,
        string? userAgent)
    {
        var status = request.Status.Trim();

        if (!AllowedStatuses.Contains(status))
        {
            return ServiceResultDto.Fail(
                "INVALID_FORECAST_STATUS",
                "Invalid forecast recommendation status.");
        }

        var existing = await _forecastRepository.GetRecommendationByIdAsync(
            hospitalId,
            recId);

        if (existing is null)
        {
            return ServiceResultDto.Fail(
                "FORECAST_RECOMMENDATION_NOT_FOUND",
                "Forecast recommendation was not found.");
        }

        var updated = await _forecastRepository.UpdateRecommendationStatusAsync(
            hospitalId,
            recId,
            status,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail(
                "FORECAST_STATUS_UPDATE_FAILED",
                "Forecast recommendation status could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = $"ForecastRecommendation{status}",
            Entity = "ForecastRecommendations",
            EntityId = recId,
            OldValue = existing.RecStatus,
            NewValue = status,
            Remarks = request.SchedulerRemarks,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok($"Forecast recommendation updated to {status}.");
    }

    public async Task<ServiceResultDto<List<SurgeryDurationDto>>> GetSurgeryDurationAveragesAsync(
    int hospitalId)
    {
        var averages = await _forecastRepository.GetSurgeryDurationAveragesAsync(
            hospitalId);

        return ServiceResultDto<List<SurgeryDurationDto>>.Ok(averages);
    }
    public async Task<ServiceResultDto<ForecastSummaryDto>> GetForecastSummaryAsync(
        int hospitalId)
    {
        var summary = await _forecastRepository.GetForecastSummaryAsync(hospitalId);

        return ServiceResultDto<ForecastSummaryDto>.Ok(summary);
    }
    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}