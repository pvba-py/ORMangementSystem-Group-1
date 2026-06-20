using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task AddAuditLogAsync(CreateAuditLogDto request);

    Task AddPhiAccessLogAsync(CreatePhiAccessLogDto request);

    Task AddPhiAccessLogsBulkAsync(List<CreatePhiAccessLogDto> requests);

    Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(
        int hospitalId,
        string? entity,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize);

    Task<PagedResultDto<PhiAccessLogDto>> GetPhiAccessLogsAsync(
        int hospitalId,
        int? patientId,
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize);
}