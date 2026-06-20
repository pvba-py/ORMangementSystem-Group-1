using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IAuditRepository auditRepository,
        ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<PagedResultDto<AuditLogDto>>> GetAuditLogsAsync(
    int hospitalId,
    string? entity,
    string? action,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber,
    int pageSize)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<PagedResultDto<AuditLogDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var logs = await _auditRepository.GetAuditLogsAsync(
            hospitalId,
            entity,
            action,
            fromDate,
            toDate,
            pageNumber,
            pageSize);

        return ServiceResultDto<PagedResultDto<AuditLogDto>>.Ok(logs);
    }

    public async Task<ServiceResultDto<PagedResultDto<PhiAccessLogDto>>> GetPhiAccessLogsAsync(
        int hospitalId,
        int? patientId,
        int? userId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<PagedResultDto<PhiAccessLogDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var logs = await _auditRepository.GetPhiAccessLogsAsync(
            hospitalId,
            patientId,
            userId,
            fromDate,
            toDate,
            pageNumber,
            pageSize);

        return ServiceResultDto<PagedResultDto<PhiAccessLogDto>>.Ok(logs);
    }
}