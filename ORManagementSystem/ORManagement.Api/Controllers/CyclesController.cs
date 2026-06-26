using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/cycles")]
[Authorize]
public class CyclesController : ApiControllerBase
{
    private readonly ISchedulingCycleService _cycleService;
    private readonly ILogger<CyclesController> _logger;
    private readonly IAutoSchedulingService _autoSchedulingService;

    public CyclesController(
     ISchedulingCycleService cycleService,
     IAutoSchedulingService autoSchedulingService,
     ILogger<CyclesController> logger)
    {
        _cycleService = cycleService;
        _autoSchedulingService = autoSchedulingService;
        _logger = logger;
    }

    [HttpPost("{id:int}/auto-build-blocks")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> AutoBuildBlocks(int id)
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

        var result = await _autoSchedulingService.AutoBuildBlocksAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = result.Data
        });
    }
    [HttpPost("{id:int}/auto-assign-cases")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> AutoAssignCases(int id)
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

        var result = await _autoSchedulingService.AutoAssignCasesAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = result.Data
        });
    }
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentCycle()
    {
        var result = await _cycleService.GetCurrentCycleAsync(
            GetCurrentHospitalIdOrDefault());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:int}/ranked-requests")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetRankedRequests(int id)
    {
        var result = await _cycleService.GetRankedRequestsAsync(
            GetCurrentHospitalIdOrDefault(),
            id);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetCycles()
    {
        var result = await _cycleService.GetCyclesAsync(
            GetCurrentHospitalIdOrDefault());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpPut("{id:int}/cutoff")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CutoffCycle(int id)
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

        var result = await _cycleService.CutoffCycleAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    [HttpPut("{id:int}/publish")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> PublishCycle(int id)
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

        var result = await _cycleService.PublishCycleAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }
    [HttpPut("{id:int}/start")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> StartCycle(int id)
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

        var result = await _cycleService.StartCycleAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }
    [HttpPut("{id:int}/close")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CloseCycle(int id)
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

        var result = await _cycleService.CloseCycleAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }
}