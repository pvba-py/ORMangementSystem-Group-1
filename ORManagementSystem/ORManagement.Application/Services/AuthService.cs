using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Auth;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ORManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuditRepository auditRepository,
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _auditRepository = auditRepository;
        _authRepository = authRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResultDto> RegisterAsync(
        RegisterRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        _logger.LogInformation("Register request received for username {Username}", request.Username);

        var roleName = request.RoleName.Trim();

        if (roleName != "Surgeon" && roleName != "ORScheduler")
        {
            return AuthResultDto.Fail("INVALID_ROLE", "Role must be either Surgeon or ORScheduler.");
        }

        if (roleName == "Surgeon" && string.IsNullOrWhiteSpace(request.Specialty))
        {
            return AuthResultDto.Fail("SPECIALTY_REQUIRED", "Specialty is required when registering a surgeon.");
        }

        if (roleName == "Surgeon" && request.HospitalId is null)
        {
            return AuthResultDto.Fail("HOSPITAL_REQUIRED", "HospitalId is required for surgeon registration.");
        }

        var usernameExists = await _authRepository.UsernameExistsAsync(request.Username.Trim());
        if (usernameExists)
        {
            return AuthResultDto.Fail("USERNAME_ALREADY_EXISTS", "Username already exists.");
        }

        var emailExists = await _authRepository.EmailExistsAsync(request.Email.Trim());

        if (emailExists)
        {
            return AuthResultDto.Fail("EMAIL_ALREADY_EXISTS", "Email is already in use.");
        }

        var roleId = await _authRepository.GetRoleIdByNameAsync(roleName);
        if (roleId is null)
        {
            return AuthResultDto.Fail("ROLE_NOT_FOUND", "Selected role was not found.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var userId = await _authRepository.CreateUserAsync(request, roleId.Value, passwordHash);

        if (roleName == "Surgeon")
        {
            await _authRepository.CreateSurgeonProfileAsync(
                userId,
                request.HospitalId!.Value,
                request.Specialty!);
        }

        var createdUser = await _authRepository.GetUserProfileByIdAsync(userId);
        if (createdUser is null)
        {
            return AuthResultDto.Fail("REGISTRATION_FAILED", "User was created but profile could not be loaded.");
        }
        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = createdUser.HospitalId,
            UserId = createdUser.UserId,
            RoleName = createdUser.RoleName,
            Action = "UserRegistered",
            Entity = "Users",
            EntityId = createdUser.UserId,
            NewValue = $"{createdUser.Username}|{createdUser.RoleName}",
            Remarks = "New user registered.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        if (createdUser.RoleName == "Surgeon" && createdUser.SurgeonId.HasValue)
        {
            await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
            {
                HospitalId = createdUser.HospitalId,
                UserId = createdUser.UserId,
                RoleName = createdUser.RoleName,
                Action = "SurgeonProfileCreated",
                Entity = "Surgeons",
                EntityId = createdUser.SurgeonId.Value,
                NewValue = createdUser.FullName,
                Remarks = "Surgeon profile created during registration.",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });
        }

        var accessToken = GenerateAccessToken(createdUser);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        await _authRepository.CreateRefreshTokenAsync(
            createdUser.UserId,
            createdUser.HospitalId,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            ipAddress,
            userAgent);

        _logger.LogInformation("User registered successfully. UserId: {UserId}, Role: {RoleName}", userId, roleName);

        return AuthResultDto.Ok(accessToken, refreshToken, createdUser, "Registration successful.");
    }

    public async Task<AuthResultDto> LoginAsync(
    LoginRequestDto request,
    string? ipAddress,
    string? userAgent)
    {
        _logger.LogInformation("Login attempt for username {Username}", request.Username);

        var username = request.Username.Trim();

        var user = await _authRepository.GetUserAuthByUsernameAsync(username);

        if (user is null)
        {
            await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
            {
                HospitalId = null,
                UserId = null,
                RoleName = "Unknown",
                Action = "LoginFailed",
                Entity = "Auth",
                EntityId = null,
                OldValue = null,
                NewValue = null,
                Remarks = $"Login failed. Username not found: {username}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            _logger.LogWarning("Login failed. Username not found: {Username}", username);

            return AuthResultDto.Fail("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
            {
                HospitalId = user.HospitalId,
                UserId = user.UserId,
                RoleName = user.RoleName,
                Action = "LoginFailed",
                Entity = "Auth",
                EntityId = user.UserId,
                OldValue = null,
                NewValue = null,
                Remarks = $"Login failed. Invalid password for username: {username}",
                IpAddress = ipAddress,
                UserAgent = userAgent
            });

            _logger.LogWarning("Login failed. Invalid password for username {Username}", username);

            return AuthResultDto.Fail("INVALID_CREDENTIALS", "Invalid username or password.");
        }

        var authUser = ToAuthUserDto(user);

        var accessToken = GenerateAccessToken(authUser);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        await _authRepository.CreateRefreshTokenAsync(
            authUser.UserId,
            authUser.HospitalId,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            ipAddress,
            userAgent);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = authUser.HospitalId,
            UserId = authUser.UserId,
            RoleName = authUser.RoleName,
            Action = "LoginSuccess",
            Entity = "Auth",
            EntityId = authUser.UserId,
            OldValue = null,
            NewValue = "Authenticated",
            Remarks = "User logged in successfully.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Login successful. UserId: {UserId}, Role: {RoleName}",
            user.UserId,
            user.RoleName);

        return AuthResultDto.Ok(accessToken, refreshToken, authUser, "Login successful.");
    }

    public async Task<AuthResultDto> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthResultDto.Fail("REFRESH_TOKEN_MISSING", "Refresh token is missing.");
        }

        var oldTokenHash = HashToken(refreshToken);
        var storedToken = await _authRepository.GetRefreshTokenByHashAsync(oldTokenHash);

        if (storedToken is null)
        {
            return AuthResultDto.Fail("INVALID_REFRESH_TOKEN", "Invalid refresh token.");
        }

        if (storedToken.RevokedAt is not null)
        {
            return AuthResultDto.Fail("REFRESH_TOKEN_REVOKED", "Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return AuthResultDto.Fail("REFRESH_TOKEN_EXPIRED", "Refresh token has expired.");
        }

        var user = await _authRepository.GetUserAuthByIdAsync(storedToken.UserId);

        if (user is null)
        {
            return AuthResultDto.Fail("USER_NOT_FOUND", "User not found.");
        }

        var authUser = ToAuthUserDto(user);

        var newAccessToken = GenerateAccessToken(authUser);
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshToken);

        await _authRepository.CreateRefreshTokenAsync(
            authUser.UserId,
            authUser.HospitalId,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            ipAddress,
            userAgent);

        await _authRepository.RevokeRefreshTokenAsync(
            oldTokenHash,
            newRefreshTokenHash,
            ipAddress);

        _logger.LogInformation("Refresh token rotated successfully for UserId {UserId}", authUser.UserId);

        return AuthResultDto.Ok(newAccessToken, newRefreshToken, authUser, "Token refreshed successfully.");
    }

    public async Task<ServiceResultDto> LogoutAsync(
     string? refreshToken,
     int? userId,
     string? ipAddress,
     string? userAgent)
    {
        AuthUserDto? currentUser = null;

        if (userId.HasValue)
        {
            currentUser = await _authRepository.GetUserProfileByIdAsync(userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = HashToken(refreshToken);

            await _authRepository.RevokeRefreshTokenAsync(
                tokenHash,
                null,
                ipAddress);
        }
        else if (userId.HasValue)
        {
            await _authRepository.RevokeAllRefreshTokensForUserAsync(
                userId.Value,
                ipAddress);
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = currentUser?.HospitalId,
            UserId = userId,
            RoleName = currentUser?.RoleName ?? "Unknown",
            Action = "LogoutCompleted",
            Entity = "Auth",
            EntityId = userId,
            OldValue = "Authenticated",
            NewValue = "LoggedOut",
            Remarks = "User logged out successfully.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("Logout completed for UserId {UserId}", userId);

        return ServiceResultDto.Ok("Logout successful.");
    }

    public async Task<ServiceResultDto<AuthUserDto>> GetCurrentUserAsync(int userId)
    {
        var user = await _authRepository.GetUserProfileByIdAsync(userId);

        if (user is null)
        {
            return ServiceResultDto<AuthUserDto>.Fail("USER_NOT_FOUND", "User not found.");
        }

        return ServiceResultDto<AuthUserDto>.Ok(user);
    }

    private string GenerateAccessToken(AuthUserDto user)
    {
        var jwtKey = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiryMinutes = Convert.ToInt32(_configuration["Jwt:AccessTokenExpiresInMinutes"] ?? "240");

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("JWT key is missing from configuration.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleName),
            new Claim("fullName", user.FullName),
            new Claim("email", user.Email)
        };

        if (user.HospitalId.HasValue)
        {
            claims.Add(new Claim("hospitalId", user.HospitalId.Value.ToString()));
        }

        if (user.SurgeonId.HasValue)
        {
            claims.Add(new Claim("surgeonId", user.SurgeonId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private int GetRefreshTokenExpiryDays()
    {
        return Convert.ToInt32(_configuration["Jwt:RefreshTokenExpiresInDays"] ?? "7");
    }

    private static AuthUserDto ToAuthUserDto(UserAuthRecordDto user)
    {
        return new AuthUserDto
        {
            UserId = user.UserId,
            HospitalId = user.HospitalId,
            RoleId = user.RoleId,
            RoleName = user.RoleName,
            SurgeonId = user.SurgeonId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email
        };
    }
}