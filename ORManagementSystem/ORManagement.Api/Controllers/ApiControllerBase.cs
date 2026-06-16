using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Shared;

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