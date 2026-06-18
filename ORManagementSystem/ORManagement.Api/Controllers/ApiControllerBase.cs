using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Shared;
using System.Security.Claims;

namespace ORManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult MapError(ServiceResultDto result)
    {
        return result.ErrorCode switch
        {
            "USER_NOT_FOUND" => NotFound(Error(result)),
            "USERNAME_ALREADY_EXISTS" => Conflict(Error(result)),
            "INVALID_CREDENTIALS" => Unauthorized(Error(result)),
            "INVALID_ROLE" => BadRequest(Error(result)),
            "ROLE_NOT_FOUND" => BadRequest(Error(result)),
            "EMAIL_ALREADY_EXISTS" => Conflict(Error(result)),
            "SPECIALTY_REQUIRED" => BadRequest(Error(result)),
            "HOSPITAL_REQUIRED" => BadRequest(Error(result)),
            "VALIDATION_ERROR" => BadRequest(Error(result)),
            "REFRESH_TOKEN_MISSING" => Unauthorized(Error(result)),
            "INVALID_REFRESH_TOKEN" => Unauthorized(Error(result)),
            "REFRESH_TOKEN_EXPIRED" => Unauthorized(Error(result)),
            "REFRESH_TOKEN_REVOKED" => Unauthorized(Error(result)),
            "PATIENT_NOT_FOUND" => NotFound(Error(result)),
            "ROOM_NOT_FOUND" => NotFound(Error(result)),
            "ROOM_ALREADY_EXISTS" => Conflict(Error(result)),
            "ROOM_UPDATE_FAILED" => BadRequest(Error(result)),
            "ROOM_DELETE_FAILED" => BadRequest(Error(result)),
            "INVALID_DATE_RANGE" => BadRequest(Error(result)),
            "REQUEST_NOT_FOUND" => NotFound(Error(result)),
            "REQUEST_UPDATE_FAILED" => BadRequest(Error(result)),
            "REQUEST_CANCEL_FAILED" => BadRequest(Error(result)),
            "REQUEST_STATUS_UPDATE_FAILED" => BadRequest(Error(result)),
            "INVALID_REQUEST_STATUS" => BadRequest(Error(result)),
            "INVALID_QUARTER" => BadRequest(Error(result)),
            "INVALID_PRIORITY" => BadRequest(Error(result)),
            "INVALID_READINESS" => BadRequest(Error(result)),
            "INVALID_AVAILABLE_DAYS" => BadRequest(Error(result)),
            "CYCLE_NOT_FOUND" => NotFound(Error(result)),
            "INVALID_CYCLE_STATUS" => BadRequest(Error(result)),
            "CYCLE_CUTOFF_FAILED" => BadRequest(Error(result)),
            "CYCLE_PUBLISH_FAILED" => BadRequest(Error(result)),
            "CASE_NOT_FOUND" => NotFound(Error(result)),
            "INVALID_CASE_TIME" => BadRequest(Error(result)),
            "INVALID_BLOCK" => BadRequest(Error(result)),
            "CASE_CONFLICT" => Conflict(Error(result)),
            "CASE_UPDATE_FAILED" => BadRequest(Error(result)),
            "CASE_STATUS_UPDATE_FAILED" => BadRequest(Error(result)),
            "INVALID_CASE_STATUS" => BadRequest(Error(result)),
            "INVALID_CASE_TRANSITION" => BadRequest(Error(result)),
            "CANCELLATION_REASON_REQUIRED" => BadRequest(Error(result)),
            "INVALID_CANCELLATION_REASON" => BadRequest(Error(result)),
            "DATE_OUTSIDE_AVAILABILITY" => BadRequest(Error(result)),
            "INVALID_BLOCK_TIME" => BadRequest(Error(result)),
            "TEMPLATE_NOT_FOUND" => NotFound(Error(result)),
            "TEMPLATE_UPDATE_FAILED" => BadRequest(Error(result)),
            "TEMPLATE_DEACTIVATE_FAILED" => BadRequest(Error(result)),
            "EXCEPTION_ALREADY_EXISTS" => Conflict(Error(result)),
            "EXCEPTION_NOT_FOUND" => NotFound(Error(result)),
            "INVALID_BLOCK_STATUS" => BadRequest(Error(result)),
            "BLOCK_NOT_FOUND" => NotFound(Error(result)),
            "BLOCK_CONFLICT" => Conflict(Error(result)),
            "BLOCK_UPDATE_FAILED" => BadRequest(Error(result)),
            "BLOCK_CANCEL_FAILED" => BadRequest(Error(result)),
            "INVALID_RELEASE_TIME" => BadRequest(Error(result)),
            "FORBIDDEN_BLOCK_RELEASE" => StatusCode(403, Error(result)),
            "BLOCK_RELEASE_FAILED" => BadRequest(Error(result)),
            "SURGEON_CLAIM_MISSING" => Unauthorized(Error(result)),
            "RELEASED_SLOT_NOT_FOUND" => NotFound(Error(result)),
            "INVALID_SLOT_STATE" => BadRequest(Error(result)),
            "SLOT_UPDATE_FAILED" => BadRequest(Error(result)),
            "WAITLIST_NOT_FOUND" => NotFound(Error(result)),
            "WAITLIST_ASSIGN_FAILED" => BadRequest(Error(result)),
            "WAITLIST_REMOVE_FAILED" => BadRequest(Error(result)),
            "DATE_RANGE_REQUIRED" => BadRequest(Error(result)),
            "SETTING_NOT_FOUND" => NotFound(Error(result)),
            "INVALID_SETTING_KEY" => BadRequest(Error(result)),
            "INVALID_SETTING_VALUE" => BadRequest(Error(result)),
            "SETTING_UPDATE_FAILED" => BadRequest(Error(result)),
            "INVALID_FORECAST_STATUS" => BadRequest(Error(result)),
            "FORECAST_RECOMMENDATION_NOT_FOUND" => NotFound(Error(result)),
            "FORECAST_STATUS_UPDATE_FAILED" => BadRequest(Error(result)),
            "CASE_OUTSIDE_BLOCK" => BadRequest(Error(result)),
            _ => BadRequest(Error(result))
        };
    }

    protected IActionResult MapAuthError(AuthResultDto result)
    {
        return result.ErrorCode switch
        {
            "INVALID_CREDENTIALS" => Unauthorized(Error(result)),
            "USERNAME_ALREADY_EXISTS" => Conflict(Error(result)),
            "INVALID_ROLE" => BadRequest(Error(result)),
            "ROLE_NOT_FOUND" => BadRequest(Error(result)),
            "SPECIALTY_REQUIRED" => BadRequest(Error(result)),
            "HOSPITAL_REQUIRED" => BadRequest(Error(result)),
            "EMAIL_ALREADY_EXISTS" => Conflict(Error(result)),
            "REFRESH_TOKEN_MISSING" => Unauthorized(Error(result)),
            "INVALID_REFRESH_TOKEN" => Unauthorized(Error(result)),
            "REFRESH_TOKEN_EXPIRED" => Unauthorized(Error(result)),
            "REFRESH_TOKEN_REVOKED" => Unauthorized(Error(result)),
            _ => BadRequest(Error(result))
        };
    }

    protected int? GetCurrentSurgeonId()
    {
        var surgeonIdValue = User.FindFirstValue("surgeonId");

        if (!int.TryParse(surgeonIdValue, out var surgeonId))
        {
            return null;
        }

        return surgeonId;
    }
    protected int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return userId;
    }

    protected int GetCurrentHospitalIdOrDefault()
    {
        var hospitalIdValue = User.FindFirstValue("hospitalId");

        if (int.TryParse(hospitalIdValue, out var hospitalId))
        {
            return hospitalId;
        }

        return 1;
    }

    protected string GetCurrentRoleName()
    {
        return User.FindFirstValue(ClaimTypes.Role) ?? "Unknown";
    }

    protected string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    protected string? GetUserAgent()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        return userAgent.Length > 300
            ? userAgent[..300]
            : userAgent;
    }
    private static object Error(ServiceResultDto result)
    {
        return new
        {
            success = false,
            errorCode = result.ErrorCode,
            message = result.Message
        };
    }

    private static object Error(AuthResultDto result)
    {
        return new
        {
            success = false,
            errorCode = result.ErrorCode,
            message = result.Message
        };
    }
}