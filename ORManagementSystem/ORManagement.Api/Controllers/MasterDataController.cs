using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api")]
[Authorize]
public class MasterDataController : ApiControllerBase
{
    private readonly IMasterDataService _masterDataService;
    private readonly ILogger<MasterDataController> _logger;

    public MasterDataController(
        IMasterDataService masterDataService,
        ILogger<MasterDataController> logger)
    {
        _masterDataService = masterDataService;
        _logger = logger;
    }

    [HttpGet("hospitals")]
    public async Task<IActionResult> GetHospitals()
    {
        var result = await _masterDataService.GetHospitalsAsync();

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("users")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetUsers()
    {
        var hospitalId = GetCurrentHospitalIdOrDefault();

        var result = await _masterDataService.GetUsersAsync(hospitalId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("surgeons")]
    public async Task<IActionResult> GetSurgeons()
    {
        var hospitalId = GetCurrentHospitalIdOrDefault();

        var result = await _masterDataService.GetSurgeonsAsync(hospitalId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("patients")]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var hospitalId = GetCurrentHospitalIdOrDefault();

        var result = await _masterDataService.GetPatientsAsync(
            hospitalId,
            userId.Value,
            GetCurrentRoleName(),
            search,
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("patients/{id:int}")]
    public async Task<IActionResult> GetPatientById(int id)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var hospitalId = GetCurrentHospitalIdOrDefault();

        var result = await _masterDataService.GetPatientByIdAsync(
            hospitalId,
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
}