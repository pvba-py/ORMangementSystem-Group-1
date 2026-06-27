using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/cases")]
[Authorize]
public class CasesController : ApiControllerBase
{
    private readonly ICaseService _caseService;
    private readonly ILogger<CasesController> _logger;

    public CasesController(
        ICaseService caseService,
        ILogger<CasesController> logger)
    {
        _caseService = caseService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetCases([FromQuery] string? status)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                errorMessage = "Noy Authorized."
            });
        }

        var result = await _caseService.GetCasesAsync(
            GetCurrentHospitalIdOrDefault(),
            userId.Value,
            GetCurrentRoleName(),
            status,
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
    public async Task<IActionResult> GetMyCases()
    {
        var userId = GetCurrentUserId();
        var surgeonId = GetCurrentSurgeonId();

        if (userId is null || surgeonId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "SURGEON_CLAIM_MISSING",
                message = "Surgeon profile was not found in token."
            });
        }


        var result = await _caseService.GetMyCasesAsync(
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
    public async Task<IActionResult> GetCaseById(int id)
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

        var result = await _caseService.GetCaseByIdAsync(
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
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CreateCase([FromBody] CreateCaseRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid case request.",
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

        var result = await _caseService.CreateCaseAsync(
            GetCurrentHospitalIdOrDefault(),
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
            surgeryId = result.Data
        });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateCase(int id, [FromBody] UpdateCaseRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid case update request.",
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

        var result = await _caseService.UpdateCaseAsync(
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

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateCaseStatus(int id, [FromBody] UpdateCaseStatusDto request)
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

        var result = await _caseService.UpdateCaseStatusAsync(
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
}