using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Auth;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly ORManagementDbContext _dbContext;

    public AuthRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbContext.Users
            .AnyAsync(user => user.Username == username);
    }

    public async Task<int?> GetRoleIdByNameAsync(string roleName)
    {
        return await _dbContext.Roles
            .Where(role => role.RoleName == roleName && role.IsActive)
            .Select(role => (int?)role.RoleId)
            .FirstOrDefaultAsync();
    }

    public async Task<UserAuthRecordDto?> GetUserAuthByUsernameAsync(string username)
    {
        return await GetUserAuthQuery()
            .FirstOrDefaultAsync(user => user.Username == username);
    }

    public async Task<UserAuthRecordDto?> GetUserAuthByIdAsync(int userId)
    {
        return await GetUserAuthQuery()
            .FirstOrDefaultAsync(user => user.UserId == userId);
    }

    private IQueryable<UserAuthRecordDto> GetUserAuthQuery()
    {
        return
            from user in _dbContext.Users
            join role in _dbContext.Roles on user.RoleId equals role.RoleId
            join surgeon in _dbContext.Surgeons on user.UserId equals surgeon.UserId into surgeonJoin
            from surgeon in surgeonJoin.DefaultIfEmpty()
            where user.IsActive && role.IsActive
            select new UserAuthRecordDto
            {
                UserId = user.UserId,
                HospitalId = user.HospitalId,
                RoleId = user.RoleId,
                RoleName = role.RoleName,
                SurgeonId = surgeon != null ? surgeon.SurgeonId : null,
                Username = user.Username,
                PasswordHash = user.PasswordHash,
                FullName = user.FullName,
                Email = user.Email
            };
    }

    public async Task<AuthUserDto?> GetUserProfileByIdAsync(int userId)
    {
        return await
            (
                from user in _dbContext.Users
                join role in _dbContext.Roles on user.RoleId equals role.RoleId
                join surgeon in _dbContext.Surgeons on user.UserId equals surgeon.UserId into surgeonJoin
                from surgeon in surgeonJoin.DefaultIfEmpty()
                where user.UserId == userId && user.IsActive && role.IsActive
                select new AuthUserDto
                {
                    UserId = user.UserId,
                    HospitalId = user.HospitalId,
                    RoleId = user.RoleId,
                    RoleName = role.RoleName,
                    SurgeonId = surgeon != null ? surgeon.SurgeonId : null,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email
                }
            )
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateUserAsync(RegisterRequestDto request, int roleId, string passwordHash)
    {
        var user = new User
        {
            HospitalId = request.HospitalId,
            RoleId = roleId,
            Username = request.Username.Trim(),
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user.UserId;
    }

    public async Task CreateSurgeonProfileAsync(int userId, int hospitalId, string specialty)
    {
        var surgeon = new Surgeon
        {
            UserId = userId,
            HospitalId = hospitalId,
            Specialty = specialty.Trim(),
            IsActive = true
        };

        await _dbContext.Surgeons.AddAsync(surgeon);
        await _dbContext.SaveChangesAsync();
    }

    public async Task CreateRefreshTokenAsync(
        int userId,
        int? hospitalId,
        string tokenHash,
        DateTime expiresAt,
        string? ipAddress,
        string? userAgent)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            HospitalId = hospitalId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<RefreshTokenRecordDto?> GetRefreshTokenByHashAsync(string tokenHash)
    {
        return await _dbContext.RefreshTokens
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new RefreshTokenRecordDto
            {
                RefreshTokenId = token.RefreshTokenId,
                UserId = token.UserId,
                HospitalId = token.HospitalId,
                TokenHash = token.TokenHash,
                ExpiresAt = token.ExpiresAt,
                RevokedAt = token.RevokedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbContext.Users
            .AnyAsync(user => user.Email == email);
    }
    public async Task RevokeRefreshTokenAsync(
        string tokenHash,
        string? replacedByTokenHash,
        string? ipAddress)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (token is null)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReplacedByTokenHash = replacedByTokenHash;

        await _dbContext.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensForUserAsync(int userId, string? ipAddress)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token =>
                token.UserId == userId &&
                token.RevokedAt == null &&
                token.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await _dbContext.SaveChangesAsync();
    }
}