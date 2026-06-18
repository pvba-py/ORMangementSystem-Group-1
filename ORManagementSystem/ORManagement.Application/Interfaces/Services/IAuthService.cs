using ORManagement.Application.DTOs.Auth;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, string? ipAddress, string? userAgent);
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, string? ipAddress, string? userAgent);
    Task<AuthResultDto> RefreshAsync(string? refreshToken, string? ipAddress, string? userAgent);
    Task<ServiceResultDto> LogoutAsync(string? refreshToken, int? userId, string? ipAddress, string? userAgent);
    Task<ServiceResultDto<AuthUserDto>> GetCurrentUserAsync(int userId);
}