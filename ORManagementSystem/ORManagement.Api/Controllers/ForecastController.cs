using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/forecast")]
[Authorize(Roles = "ORScheduler")]
public class ForecastController : ApiControllerBase
{
    private readonly IForecastService _forecastService;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(
        IForecastService forecastService,
        ILogger<ForecastController> logger)
    {
        _forecastService = forecastService;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetForecastSummary()
    {
        var result = await _forecastService.GetForecastSummaryAsync(
            GetCurrentHospitalIdOrDefault());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
}