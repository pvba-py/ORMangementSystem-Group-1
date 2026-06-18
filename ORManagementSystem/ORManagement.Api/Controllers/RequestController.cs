using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/requests")]
[Authorize]
public class RequestsController : ApiControllerBase
{
    private readonly IRequestService _requestService;
    private readonly ILogger<RequestsController> _logger;

    public RequestsController(
        IRequestService requestService,
        ILogger<RequestsController> logger)
    {
        _requestService = requestService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string? status,
        [FromQuery] int? cycleId)
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

        var result = await _requestService.GetRequestsAsync(
            GetCurrentHospitalIdOrDefault(),
            userId.Value,
            GetCurrentRoleName(),
            status,
            cycleId,
            GetIpAddress(),
            GetUserAgent());


        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("my")]
    [Authorize(Roles = "Surgeon")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = GetCurrentUserId();
        var surgeonId = GetCurrentSurgeonId();

        if (userId is null || surgeonId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid surgeon token."
            });
        }

        var result = await _requestService.GetMyRequestsAsync(
            GetCurrentHospitalIdOrDefault(),
            surgeonId.Value,
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRequestById(int id)
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

        var result = await _requestService.GetRequestByIdAsync(
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

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Surgeon")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateOrRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid request data.",
                errors = ModelState
            });
        }

        var userId = GetCurrentUserId();
        var surgeonId = GetCurrentSurgeonId();

        if (userId is null || surgeonId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid surgeon token."
            });
        }

        var result = await _requestService.CreateRequestAsync(
            GetCurrentHospitalIdOrDefault(),
            surgeonId.Value,
            userId.Value,
            GetCurrentRoleName(),
            request,
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
            requestId = result.Data
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateOrRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid request update data.",
                errors = ModelState
            });
        }

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

        var result = await _requestService.UpdateRequestAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            request,
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRequest(int id)
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

        var result = await _requestService.DeleteRequestAsync(
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

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] UpdateRequestStatusDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid status update request.",
                errors = ModelState
            });
        }

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

        var result = await _requestService.UpdateRequestStatusAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            request,
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

    [HttpGet("{id:int}/score")]
    public async Task<IActionResult> GetRequestScore(int id)
    {
        var result = await _requestService.GetRequestScoreAsync(
            GetCurrentHospitalIdOrDefault(),
            id);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
}