using ORManagement.Application.DTOs.Audit;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task AddAuditLogAsync(CreateAuditLogDto request);
    Task AddPhiAccessLogAsync(CreatePhiAccessLogDto request);
}