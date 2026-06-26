using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Cycles;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class SchedulingCycleService : ISchedulingCycleService
{
    private readonly ICycleRepository _cycleRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<SchedulingCycleService> _logger;

    public SchedulingCycleService(
        ICycleRepository cycleRepository,
        IAuditRepository auditRepository,
        ILogger<SchedulingCycleService> logger)
    {
        _cycleRepository = cycleRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<SchedulingCycleDto>> GetCurrentCycleAsync(int hospitalId)
    {
        var cycle = await _cycleRepository.GetCurrentCycleAsync(hospitalId);

        if (cycle is null)
        {
            return ServiceResultDto<SchedulingCycleDto>.Fail(
                "CYCLE_NOT_FOUND",
                "No open scheduling cycle was found.");
        }

        return ServiceResultDto<SchedulingCycleDto>.Ok(cycle);
    }

    public async Task<ServiceResultDto<List<RankedRequestDto>>> GetRankedRequestsAsync(
        int hospitalId,
        int cycleId)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(hospitalId, cycleId);

        if (cycle is null)
        {
            return ServiceResultDto<List<RankedRequestDto>>.Fail(
                "CYCLE_NOT_FOUND",
                "Scheduling cycle was not found.");
        }

        var requests = await _cycleRepository.GetRankedRequestsAsync(cycleId);

        return ServiceResultDto<List<RankedRequestDto>>.Ok(requests);
    }

    public async Task<ServiceResultDto> CutoffCycleAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(hospitalId, cycleId);

        if (cycle is null)
        {
            return ServiceResultDto.Fail("CYCLE_NOT_FOUND", "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Open")
        {
            return ServiceResultDto.Fail(
                "INVALID_CYCLE_STATUS",
                "Only an open cycle can be moved to cutoff.");
        }

        var cutoffDone = await _cycleRepository.CutoffCycleAsync(hospitalId, cycleId);

        if (!cutoffDone)
        {
            return ServiceResultDto.Fail(
                "CYCLE_CUTOFF_FAILED",
                "Cycle cutoff could not be completed.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CycleCutoffTriggered",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            OldValue = cycle.CycleStatus,
            NewValue = "Cutoff",
            Remarks = "Scheduler triggered weekly cycle cutoff.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Cycle cutoff completed. CycleId: {CycleId}, UserId: {UserId}",
            cycleId,
            userId);

        return ServiceResultDto.Ok("Cycle cutoff completed successfully.");
    }

    public async Task<ServiceResultDto> PublishCycleAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(hospitalId, cycleId);

        if (cycle is null)
        {
            return ServiceResultDto.Fail("CYCLE_NOT_FOUND", "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Scheduling" && cycle.CycleStatus != "Cutoff")
        {
            return ServiceResultDto.Fail(
                "INVALID_CYCLE_STATUS",
                "Only a cutoff or scheduling cycle can be published.");
        }

        var published = await _cycleRepository.PublishCycleAsync(hospitalId, cycleId, userId);

        if (!published)
        {
            return ServiceResultDto.Fail(
                "CYCLE_PUBLISH_FAILED",
                "Cycle could not be published.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CyclePublished",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            OldValue = cycle.CycleStatus,
            NewValue = "Published",
            Remarks = "Weekly schedule was published.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Cycle published. CycleId: {CycleId}, UserId: {UserId}",
            cycleId,
            userId);

        return ServiceResultDto.Ok("Cycle published successfully.");
    }
    public async Task<ServiceResultDto<List<SchedulingCycleDto>>> GetCyclesAsync(int hospitalId)
    {
        var cycles = await _cycleRepository.GetCyclesAsync(hospitalId);

        return ServiceResultDto<List<SchedulingCycleDto>>.Ok(cycles);
    }
    public async Task<ServiceResultDto> StartCycleAsync(
     int hospitalId,
     int cycleId,
     int userId,
     string roleName,
     string? ipAddress,
     string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(hospitalId, cycleId);

        if (cycle is null)
        {
            return ServiceResultDto.Fail(
                "CYCLE_NOT_FOUND",
                "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Closed")
        {
            return ServiceResultDto.Fail(
                "INVALID_CYCLE_STATUS",
                "Only a closed cycle can be started.");
        }

        var openCycleExists = await _cycleRepository.HasOpenCycleAsync(
            hospitalId,
            excludeCycleId: cycleId);

        if (openCycleExists)
        {
            return ServiceResultDto.Fail(
                "OPEN_CYCLE_ALREADY_EXISTS",
                "Another cycle is already open. Close or cutoff the open cycle before starting a new one.");
        }

        var started = await _cycleRepository.StartCycleAsync(
    hospitalId,
    cycleId,
    userId);

        if (!started)
        {
            return ServiceResultDto.Fail(
                "CYCLE_START_FAILED",
                "Cycle could not be started.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CycleStarted",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            OldValue = cycle.CycleStatus,
            NewValue = "Open",
            Remarks = "Scheduler manually started the scheduling cycle.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Cycle started. CycleId: {CycleId}, UserId: {UserId}",
            cycleId,
            userId);

        return ServiceResultDto.Ok("Cycle started successfully.");
    }
    public async Task<ServiceResultDto> CloseCycleAsync(
    int hospitalId,
    int cycleId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(hospitalId, cycleId);

        if (cycle is null)
        {
            return ServiceResultDto.Fail(
                "CYCLE_NOT_FOUND",
                "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Published")
        {
            return ServiceResultDto.Fail(
                "INVALID_CYCLE_STATUS",
                "Only a published cycle can be closed.");
        }

        var closed = await _cycleRepository.CloseCycleAsync(
            hospitalId,
            cycleId,
            userId);

        if (!closed)
        {
            return ServiceResultDto.Fail(
                "CYCLE_CLOSE_FAILED",
                "Cycle could not be closed.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "CycleClosed",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            OldValue = cycle.CycleStatus,
            NewValue = "Closed",
            Remarks = "Scheduler closed the published scheduling cycle.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Cycle closed. CycleId: {CycleId}, UserId: {UserId}",
            cycleId,
            userId);

        return ServiceResultDto.Ok("Cycle closed successfully.");
    }
}