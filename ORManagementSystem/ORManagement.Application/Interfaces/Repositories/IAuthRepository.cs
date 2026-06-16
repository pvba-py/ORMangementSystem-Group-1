using ORManagement.Application.DTOs.Auth;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IAuthRepository
{
    Task<bool> UsernameExistsAsync(string username);
    Task<int?> GetRoleIdByNameAsync(string roleName);

    Task<UserAuthRecordDto?> GetUserAuthByUsernameAsync(string username);
    Task<UserAuthRecordDto?> GetUserAuthByIdAsync(int userId);
    Task<AuthUserDto?> GetUserProfileByIdAsync(int userId);
    Task<bool> EmailExistsAsync(string email);
    Task<int> CreateUserAsync(RegisterRequestDto request, int roleId, string passwordHash);
    Task CreateSurgeonProfileAsync(int userId, int hospitalId, string specialty);

    Task CreateRefreshTokenAsync(
        int userId,
        int? hospitalId,
        string tokenHash,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent);

    Task<RefreshTokenRecordDto?> GetRefreshTokenByHashAsync(string tokenHash);

    Task RevokeRefreshTokenAsync(
        string tokenHash,
        string? replacedByTokenHash,
        string? ipAddress);

    Task RevokeAllRefreshTokensForUserAsync(int userId, string? ipAddress);
}