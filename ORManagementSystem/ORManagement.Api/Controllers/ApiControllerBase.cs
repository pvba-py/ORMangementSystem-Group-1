using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Shared;
using System.Security.Claims;

namespace ORManagement.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private const int DefaultHospitalId = 1;
    private const int MaxUserAgentLength = 300;

    protected IActionResult MapError(ServiceResultDto result)
    {
        var errorResponse = Error(result);

        return result.ErrorCode switch
        {
            // 404 Not Found
            "USER_NOT_FOUND"
            or "PATIENT_NOT_FOUND"
            or "ROOM_NOT_FOUND"
            or "REQUEST_NOT_FOUND"
            or "CYCLE_NOT_FOUND"
            or "CASE_NOT_FOUND"
            or "TEMPLATE_NOT_FOUND"
            or "EXCEPTION_NOT_FOUND"
            or "BLOCK_NOT_FOUND"
            or "RELEASED_SLOT_NOT_FOUND"
            or "WAITLIST_NOT_FOUND"
            or "SETTING_NOT_FOUND"
            or "FORECAST_RECOMMENDATION_NOT_FOUND"
                => NotFound(errorResponse),

            // 409 Conflict
            "USERNAME_ALREADY_EXISTS"
            or "EMAIL_ALREADY_EXISTS"
            or "ROOM_ALREADY_EXISTS"
            or "CASE_CONFLICT"
            or "EXCEPTION_ALREADY_EXISTS"
            or "BLOCK_CONFLICT"
                => Conflict(errorResponse),

            // 401 Unauthorized
            "INVALID_CREDENTIALS"
            or "REFRESH_TOKEN_MISSING"
            or "INVALID_REFRESH_TOKEN"
            or "REFRESH_TOKEN_EXPIRED"
            or "REFRESH_TOKEN_REVOKED"
            or "SURGEON_CLAIM_MISSING"
            or "INVALID_TOKEN"
                => Unauthorized(errorResponse),

            // 403 Forbidden
            "FORBIDDEN_BLOCK_RELEASE"
                => StatusCode(StatusCodes.Status403Forbidden, errorResponse),

            // 400 Bad Request
            "INVALID_ROLE"
            or "ROLE_NOT_FOUND"
            or "SPECIALTY_REQUIRED"
            or "HOSPITAL_REQUIRED"
            or "VALIDATION_ERROR"
            or "ROOM_UPDATE_FAILED"
            or "ROOM_DELETE_FAILED"
            or "INVALID_DATE_RANGE"
            or "REQUEST_UPDATE_FAILED"
            or "REQUEST_CANCEL_FAILED"
            or "REQUEST_STATUS_UPDATE_FAILED"
            or "INVALID_REQUEST_STATUS"
            or "INVALID_QUARTER"
            or "INVALID_PRIORITY"
            or "INVALID_READINESS"
            or "INVALID_AVAILABLE_DAYS"
            or "INVALID_CYCLE_STATUS"
            or "CYCLE_CUTOFF_FAILED"
            or "CYCLE_PUBLISH_FAILED"
            or "INVALID_CASE_TIME"
            or "INVALID_BLOCK"
            or "CASE_UPDATE_FAILED"
            or "CASE_STATUS_UPDATE_FAILED"
            or "INVALID_CASE_STATUS"
            or "INVALID_CASE_TRANSITION"
            or "CANCELLATION_REASON_REQUIRED"
            or "INVALID_CANCELLATION_REASON"
            or "DATE_OUTSIDE_AVAILABILITY"
            or "INVALID_BLOCK_TIME"
            or "TEMPLATE_UPDATE_FAILED"
            or "TEMPLATE_DEACTIVATE_FAILED"
            or "INVALID_BLOCK_STATUS"
            or "BLOCK_UPDATE_FAILED"
            or "BLOCK_CANCEL_FAILED"
            or "INVALID_RELEASE_TIME"
            or "BLOCK_RELEASE_FAILED"
            or "INVALID_SLOT_STATE"
            or "SLOT_UPDATE_FAILED"
            or "WAITLIST_ASSIGN_FAILED"
            or "WAITLIST_REMOVE_FAILED"
            or "DATE_RANGE_REQUIRED"
            or "INVALID_SETTING_KEY"
            or "INVALID_SETTING_VALUE"
            or "SETTING_UPDATE_FAILED"
            or "INVALID_FORECAST_STATUS"
            or "FORECAST_STATUS_UPDATE_FAILED"
            or "CASE_OUTSIDE_BLOCK"
            or "INVALID_BLOCK_TYPE"
            or "SURGEON_REQUIRED_FOR_RECURRING_BLOCK"
            or "BLOCK_SURGEON_MISMATCH"
            or "EMERGENCY_BLOCK_REQUIRED_PRIORITY"
            or "CASE_DURATION_TOO_SHORT"
            or "BLOCK_HAS_CASES"
            or "BLOCK_HAS_RELEASED_SLOTS"
            or "BLOCK_HAS_UTILIZATION_RECORDS"
            or "SURGEON_CASE_CONFLICT"
                => BadRequest(errorResponse),

            _ => BadRequest(errorResponse)
        };
    }

    protected IActionResult MapAuthError(AuthResultDto result)
    {
        var errorResponse = Error(result);

        return result.ErrorCode switch
        {
            // 401 Unauthorized
            "INVALID_CREDENTIALS"
            or "REFRESH_TOKEN_MISSING"
            or "INVALID_REFRESH_TOKEN"
            or "REFRESH_TOKEN_EXPIRED"
            or "REFRESH_TOKEN_REVOKED"
            or "INVALID_TOKEN"
                => Unauthorized(errorResponse),

            // 409 Conflict
            "USERNAME_ALREADY_EXISTS"
            or "EMAIL_ALREADY_EXISTS"
                => Conflict(errorResponse),

            // 400 Bad Request
            "INVALID_ROLE"
            or "ROLE_NOT_FOUND"
            or "SPECIALTY_REQUIRED"
            or "HOSPITAL_REQUIRED"
                => BadRequest(errorResponse),

            _ => BadRequest(errorResponse)
        };
    }

    protected int? GetCurrentSurgeonId()
    {
        return GetIntClaimValue("surgeonId");
    }

    protected int? GetCurrentUserId()
    {
        return GetIntClaimValue(ClaimTypes.NameIdentifier);
    }

    protected int GetCurrentHospitalIdOrDefault()
    {
        return GetIntClaimValue("hospitalId") ?? DefaultHospitalId;
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

        return userAgent.Length > MaxUserAgentLength
            ? userAgent[..MaxUserAgentLength]
            : userAgent;
    }

    private int? GetIntClaimValue(string claimType)
    {
        var claimValue = User.FindFirstValue(claimType);

        return int.TryParse(claimValue, out var value)
            ? value
            : null;
    }

    private static object Error(ServiceResultDto result)
    {
        return CreateError(result.ErrorCode, result.Message);
    }

    private static object Error(AuthResultDto result)
    {
        return CreateError(result.ErrorCode, result.Message);
    }

    private static object CreateError(string? errorCode, string? message)
    {
        return new
        {
            success = false,
            errorCode,
            message
        };
    }
}