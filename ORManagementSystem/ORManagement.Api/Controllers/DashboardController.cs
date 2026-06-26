using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ORManagement.Application.DTOs.Dashboard;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/dashboard")]
[Authorize]
public class DashboardController : ApiControllerBase
{
    private const string AccessSourceHeaderName = "X-Data-Source";
    private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromSeconds(60);

    private readonly IDashboardService _dashboardService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        IMemoryCache cache,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet("surgeon")]
    [Authorize(Roles = "Surgeon")]
    public async Task<IActionResult> GetSurgeonDashboard()
    {
        var surgeonId = GetCurrentSurgeonId();

        if (surgeonId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "SURGEON_CLAIM_MISSING",
                message = "Surgeon profile was not found in token."
            });
        }

        var hospitalId = GetCurrentHospitalIdOrDefault();

        var cacheKey = GetSurgeonDashboardCacheKey(
            hospitalId,
            surgeonId.Value);

        if (_cache.TryGetValue(cacheKey, out SurgeonDashboardDto? cachedDashboard) &&
            cachedDashboard is not null)
        {
            Response.Headers[AccessSourceHeaderName] = "cache";

            _logger.LogInformation(
                "Surgeon dashboard served from cache. HospitalId: {HospitalId}, SurgeonId: {SurgeonId}",
                hospitalId,
                surgeonId.Value);

            return Ok(cachedDashboard);
        }

        var result = await _dashboardService.GetSurgeonDashboardAsync(
            hospitalId,
            surgeonId.Value);

        if (!result.Success)
        {
            return MapError(result);
        }

        if (result.Data is not null)
        {
            _cache.Set(
                cacheKey,
                result.Data,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DashboardCacheDuration
                });
        }

        Response.Headers[AccessSourceHeaderName] = "database";

        _logger.LogInformation(
            "Surgeon dashboard served from database and cached. HospitalId: {HospitalId}, SurgeonId: {SurgeonId}",
            hospitalId,
            surgeonId.Value);

        return Ok(result.Data);
    }

    [HttpGet("scheduler")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetSchedulerDashboard()
    {
        var hospitalId = GetCurrentHospitalIdOrDefault();

        var cacheKey = GetSchedulerDashboardCacheKey(hospitalId);

        if (_cache.TryGetValue(cacheKey, out SchedulerDashboardDto? cachedDashboard) &&
            cachedDashboard is not null)
        {
            Response.Headers[AccessSourceHeaderName] = "cache";

            _logger.LogInformation(
                "Scheduler dashboard served from cache. HospitalId: {HospitalId}",
                hospitalId);

            return Ok(cachedDashboard);
        }

        var result = await _dashboardService.GetSchedulerDashboardAsync(hospitalId);

        if (!result.Success)
        {
            return MapError(result);
        }

        if (result.Data is not null)
        {
            _cache.Set(
                cacheKey,
                result.Data,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DashboardCacheDuration
                });
        }

        Response.Headers[AccessSourceHeaderName] = "database";

        _logger.LogInformation(
            "Scheduler dashboard served from database and cached. HospitalId: {HospitalId}",
            hospitalId);

        return Ok(result.Data);
    }

    [HttpGet("today")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetTodaySchedule([FromQuery] DateTime? date)
    {
        /*
            Not caching today's schedule for now because it can change frequently
            when cases are scheduled, updated, cancelled, or moved.
        */

        Response.Headers[AccessSourceHeaderName] = "database";

        var result = await _dashboardService.GetTodayScheduleAsync(
            GetCurrentHospitalIdOrDefault(),
            date);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    private static string GetSurgeonDashboardCacheKey(
        int hospitalId,
        int surgeonId)
    {
        return $"dashboard:surgeon:hospital:{hospitalId}:surgeon:{surgeonId}";
    }

    private static string GetSchedulerDashboardCacheKey(int hospitalId)
    {
        return $"dashboard:scheduler:hospital:{hospitalId}";
    }
}