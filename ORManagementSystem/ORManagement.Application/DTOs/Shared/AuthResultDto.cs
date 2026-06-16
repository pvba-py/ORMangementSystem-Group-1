using ORManagement.Application.DTOs.Auth;

namespace ORManagement.Application.DTOs.Shared;

public class AuthResultDto
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }

    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    public AuthUserDto? User { get; set; }

    public static AuthResultDto Ok(
        string accessToken,
        string refreshToken,
        AuthUserDto user,
        string? message = null)
    {
        return new AuthResultDto
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = user,
            Message = message
        };
    }

    public static AuthResultDto Fail(string errorCode, string message)
    {
        return new AuthResultDto
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}