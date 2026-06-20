using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly ORManagementDbContext _dbContext;

    public AuditRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAuditLogAsync(CreateAuditLogDto request)
    {
        var log = new AuditLog
        {
            HospitalId = request.HospitalId,
            UserId = request.UserId,
            RoleName = request.RoleName,
            Action = request.Action,
            Entity = request.Entity,
            EntityId = request.EntityId,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            Remarks = request.Remarks,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.AuditLogs.AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddPhiAccessLogAsync(CreatePhiAccessLogDto request)
    {
        var log = new PhiAccessLog
        {
            HospitalId = request.HospitalId,
            UserId = request.UserId,
            PatientId = request.PatientId,
            AccessType = request.AccessType,
            Context = request.Context,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            AccessedAt = DateTime.UtcNow
        };

        await _dbContext.PhiAccessLogs.AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(
    int hospitalId,
    string? entity,
    string? action,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber,
    int pageSize)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

        var query = _dbContext.AuditLogs
            .Where(log => log.HospitalId == hospitalId);

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(log => log.Entity == entity);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(log => log.Action == action);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(log => log.CreatedAt < toExclusive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AuditLogDto
            {
                AuditId = log.AuditId,
                HospitalId = log.HospitalId,
                UserId = log.UserId,
                RoleName = log.RoleName,
                Action = log.Action,
                Entity = log.Entity,
                EntityId = log.EntityId,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                Remarks = log.Remarks,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<AuditLogDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResultDto<PhiAccessLogDto>> GetPhiAccessLogsAsync(
    int hospitalId,
    int? patientId,
    int? userId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber,
    int pageSize)
    {
        (pageNumber, pageSize) = NormalizePaging(pageNumber, pageSize);

        var query = _dbContext.PhiAccessLogs
            .Where(log => log.HospitalId == hospitalId);

        if (patientId.HasValue)
        {
            query = query.Where(log => log.PatientId == patientId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.AccessedAt >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(log => log.AccessedAt < toExclusive);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(log => log.AccessedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new PhiAccessLogDto
            {
                AccessId = log.AccessId,
                HospitalId = log.HospitalId,
                UserId = log.UserId,
                PatientId = log.PatientId,
                AccessType = log.AccessType,
                Context = log.Context,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                AccessedAt = log.AccessedAt
            })
            .ToListAsync();

        return new PagedResultDto<PhiAccessLogDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
    public async Task AddPhiAccessLogsBulkAsync(List<CreatePhiAccessLogDto> requests)
    {
        if (requests.Count == 0)
        {
            return;
        }

        var logs = requests.Select(request => new PhiAccessLog
        {
            HospitalId = request.HospitalId,
            UserId = request.UserId,
            PatientId = request.PatientId,
            AccessType = request.AccessType,
            Context = request.Context,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            AccessedAt = DateTime.UtcNow
        }).ToList();

        await _dbContext.PhiAccessLogs.AddRangeAsync(logs);
        await _dbContext.SaveChangesAsync();
    }
    private static (int pageNumber, int pageSize) NormalizePaging(
    int pageNumber,
    int pageSize)
    {
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 20;
        }

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        return (pageNumber, pageSize);
    }
}