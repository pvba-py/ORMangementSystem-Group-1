using ORManagement.Application.DTOs.Audit;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task AddAuditLogAsync(CreateAuditLogDto request);
    Task AddPhiAccessLogAsync(CreatePhiAccessLogDto request);

    Task<List<AuditLogDto>> GetAuditLogsAsync(
        int hospitalId,
        string? entity,
        string? action,
        DateTime? fromDate,
        DateTime? toDate);

    Task<List<PhiAccessLogDto>> GetPhiAccessLogsAsync(
        int hospitalId,
        int? patientId,
        int? userId,
        DateTime? fromDate,
        DateTime? toDate);

    Task AddPhiAccessLogsBulkAsync(List<CreatePhiAccessLogDto> requests);
}