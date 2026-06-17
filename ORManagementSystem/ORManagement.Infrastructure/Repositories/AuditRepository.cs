using ORManagement.Application.DTOs.Audit;
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
}