using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Auth;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private const string AccessTokenCookieName = "or_access_token";
    private const string RefreshTokenCookieName = "or_refresh_token";

    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid registration request.",
                errors = ModelState
            });
        }

        var result = await _authService.RegisterAsync(
            request,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        if (!result.Success)
        {
            return MapAuthError(result);
        }

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new
        {
            success = true,
            message = result.Message,
            user = result.User
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid login request.",
                errors = ModelState
            });
        }

        var result = await _authService.LoginAsync(
            request,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        if (!result.Success)
        {
            return MapAuthError(result);
        }

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new
        {
            success = true,
            message = result.Message,
            user = result.User
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        var result = await _authService.RefreshAsync(
            refreshToken,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        if (!result.Success)
        {
            ClearAuthCookies();
            return MapAuthError(result);
        }

        SetAuthCookies(result.AccessToken!, result.RefreshToken!);

        return Ok(new
        {
            success = true,
            message = result.Message,
            user = result.User
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        var userId = GetCurrentUserId();

        var result = await _authService.LogoutAsync(
            refreshToken,
            userId,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        ClearAuthCookies();

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

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
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

        var result = await _authService.GetCurrentUserAsync(userId.Value);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
   

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var useSecureCookies = Convert.ToBoolean(_configuration["CookieSettings:UseSecureCookies"] ?? "true");

        Response.Cookies.Append(AccessTokenCookieName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = useSecureCookies,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddMinutes(
                Convert.ToInt32(_configuration["Jwt:AccessTokenExpiresInMinutes"] ?? "240"))
        });

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = useSecureCookies,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(
                Convert.ToInt32(_configuration["Jwt:RefreshTokenExpiresInDays"] ?? "7"))
        });
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete(AccessTokenCookieName);
        Response.Cookies.Delete(RefreshTokenCookieName);
    }
}