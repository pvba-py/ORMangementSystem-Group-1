using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IAuditService
{
    Task<ServiceResultDto<PagedResultDto<AuditLogDto>>> GetAuditLogsAsync(
        int hospitalId,
        string? entity,
        string? action,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize);

    Task<ServiceResultDto<PagedResultDto<PhiAccessLogDto>>> GetPhiAccessLogsAsync(
        int hospitalId,
        int? patientId,
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize);
}