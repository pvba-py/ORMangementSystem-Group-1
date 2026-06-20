using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api/audit")]
[Authorize(Roles = "ORScheduler")]
public class AuditController : ApiControllerBase
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }
    [HttpGet("logs")]
    public async Task<IActionResult> GetAuditLogs(
    [FromQuery] string? entity,
    [FromQuery] string? action,
    [FromQuery] DateTime? fromDate,
    [FromQuery] DateTime? toDate,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetAuditLogsAsync(
            GetCurrentHospitalIdOrDefault(),
            entity,
            action,
            fromDate,
            toDate,
            pageNumber,
            pageSize);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("phi-access-logs")]
    public async Task<IActionResult> GetPhiAccessLogs(
        [FromQuery] int? patientId,
        [FromQuery] int? userId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _auditService.GetPhiAccessLogsAsync(
            GetCurrentHospitalIdOrDefault(),
            patientId,
            userId,
            fromDate,
            toDate,
            pageNumber,
            pageSize);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
}