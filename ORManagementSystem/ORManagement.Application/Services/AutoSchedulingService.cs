using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Automation;
using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class AutoSchedulingService : IAutoSchedulingService
{
    private static readonly HashSet<int> TopRecurringSurgeonIds = new()
    {
        10,
        3
    };

    private readonly ICycleRepository _cycleRepository;
    private readonly IRequestRepository _requestRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IBlockService _blockService;
    private readonly ICaseService _caseService;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<AutoSchedulingService> _logger;

    public AutoSchedulingService(
        ICycleRepository cycleRepository,
        IRequestRepository requestRepository,
        IBlockRepository blockRepository,
        IBlockService blockService,
        ICaseService caseService,
        IAuditRepository auditRepository,
        ILogger<AutoSchedulingService> logger)
    {
        _cycleRepository = cycleRepository;
        _requestRepository = requestRepository;
        _blockRepository = blockRepository;
        _blockService = blockService;
        _caseService = caseService;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<AutoAssignCasesResultDto>> AutoAssignCasesAsync(
    int hospitalId,
    int cycleId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(
            hospitalId,
            cycleId);

        if (cycle is null)
        {
            return ServiceResultDto<AutoAssignCasesResultDto>.Fail(
                "CYCLE_NOT_FOUND",
                "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Cutoff")
        {
            return ServiceResultDto<AutoAssignCasesResultDto>.Fail(
                "INVALID_CYCLE_STATUS",
                "Auto assign cases can be run only after cycle cutoff.");
        }

        var result = new AutoAssignCasesResultDto
        {
            CycleId = cycleId
        };

        var weekStart = cycle.WeekStartDate.Date;
        var weekEnd = cycle.WeekEndDate.Date;

        var requests = await _requestRepository.GetAutoAssignCandidateRequestsForCycleAsync(
    hospitalId,
    cycleId);

        if (requests.Count == 0)
        {
            return ServiceResultDto<AutoAssignCasesResultDto>.Ok(
                result,
                "No approved ready requests were found for auto assignment.");
        }

        var blocks = await _blockRepository.GetBlocksAsync(
            hospitalId,
            weekStart,
            weekEnd,
            surgeonId: null,
            roomId: null);

        var candidateBlocks = blocks
            .Where(block =>
                block.BlockStatus != "Cancelled" &&
                block.BlockStatus != "Released" &&
                block.BlockStatus != "FullyBooked")
            .OrderBy(block => block.BlockDate)
            .ThenBy(block => block.StartTime)
            .ThenBy(block => block.BlockId)
            .ToList();

        if (candidateBlocks.Count == 0)
        {
            foreach (var request in requests)
            {

                if (request.RequestStatus == "Scheduled")
                {
                    result.SkippedRequests.Add(new AutoSkippedRequestDto
                    {
                        RequestId = request.RequestId,
                        Reason = "Request is already scheduled."
                    });

                    continue;
                }

            }

            result.RequestsSkipped = result.SkippedRequests.Count;

            return ServiceResultDto<AutoAssignCasesResultDto>.Ok(
                result,
                "No available blocks were found for auto assignment.");
        }

        var existingCasesResult = await _caseService.GetCasesAsync(
            hospitalId,
            userId,
            roleName,
            status: null,
            ipAddress,
            userAgent);

        if (!existingCasesResult.Success)
        {
            return ServiceResultDto<AutoAssignCasesResultDto>.Fail(
                "CASE_LIST_FAILED",
                "Could not load existing cases for auto assignment.");
        }

        var scheduledCases = (existingCasesResult.Data ?? new List<SurgicalCaseDto>())
            .Where(surgicalCase =>
                surgicalCase.CaseStatus != "Cancelled" &&
                surgicalCase.ScheduledStart.Date >= weekStart &&
                surgicalCase.ScheduledStart.Date <= weekEnd)
            .ToList();

        var rankedRequests = requests
            .OrderByDescending(request => GetPriorityRank(request.Priority))
            .ThenByDescending(request => request.EstimatedDurationMin)
            .ThenBy(request => request.RequestId)
            .ToList();

        foreach (var request in rankedRequests)
        {

            var existingCaseForRequest = scheduledCases.Any(surgicalCase =>
                    surgicalCase.RequestId == request.RequestId &&
                    surgicalCase.CaseStatus != "Cancelled");

            if (existingCaseForRequest)
            {
                result.SkippedRequests.Add(new AutoSkippedRequestDto
                {
                    RequestId = request.RequestId,
                    Reason = "Request already has a non-cancelled surgical case."
                });

                continue;
            }

            var assigned = false;

            foreach (var block in candidateBlocks)
            {
                if (!IsBlockCompatibleWithRequest(block, request))
                {
                    continue;
                }

                var slot = FindEarliestAvailableSlot(
                    block,
                    request.EstimatedDurationMin,
                    scheduledCases);

                if (slot is null)
                {
                    continue;
                }

                var createResult = await _caseService.CreateCaseAsync(
                    hospitalId,
                    userId,
                    roleName,
                    new CreateCaseRequestDto
                    {
                        RequestId = request.RequestId,
                        BlockId = block.BlockId,
                        ScheduledStart = slot.Value.Start,
                        ScheduledEnd = slot.Value.End
                    },
                    ipAddress,
                    userAgent);

                if (!createResult.Success)
                {
                    result.SkippedRequests.Add(new AutoSkippedRequestDto
                    {
                        RequestId = request.RequestId,
                        Reason = createResult.Message ?? "Case scheduling failed."
                    });

                    assigned = true;
                    break;
                }

                var surgeryId = createResult.Data;

                result.ScheduledCases.Add(new AutoScheduledCaseDto
                {
                    RequestId = request.RequestId,
                    SurgeryId = surgeryId,
                    BlockId = block.BlockId,
                    ScheduledStart = slot.Value.Start,
                    ScheduledEnd = slot.Value.End
                });

                scheduledCases.Add(new SurgicalCaseDto
                {
                    SurgeryId = surgeryId,
                    HospitalId = hospitalId,
                    RequestId = request.RequestId,
                    BlockId = block.BlockId,
                    SurgeonId = request.SurgeonId,
                    ORRoomId = block.ORRoomId,
                    ScheduledStart = slot.Value.Start,
                    ScheduledEnd = slot.Value.End,
                    CaseStatus = "Scheduled"
                });

                await UpdateBlockStatusAfterSchedulingAsync(
                    hospitalId,
                    block,
                    scheduledCases,
                    userId);

                assigned = true;
                break;
            }

            if (!assigned)
            {
                result.SkippedRequests.Add(new AutoSkippedRequestDto
                {
                    RequestId = request.RequestId,
                    Reason = "No compatible block with sufficient available duration was found."
                });
            }
        }

        result.CasesScheduled = result.ScheduledCases.Count;
        result.RequestsSkipped = result.SkippedRequests.Count;

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "AutoAssignCases",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            NewValue = $"CasesScheduled={result.CasesScheduled};RequestsSkipped={result.RequestsSkipped}",
            Remarks = "Auto assignment of approved ready requests completed.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Auto assign cases completed. CycleId: {CycleId}, CasesScheduled: {CasesScheduled}, RequestsSkipped: {RequestsSkipped}",
            cycleId,
            result.CasesScheduled,
            result.RequestsSkipped);

        return ServiceResultDto<AutoAssignCasesResultDto>.Ok(
            result,
            "Auto assign cases completed successfully.");
    }
    private static int GetPriorityRank(string priority)
    {
        return priority switch
        {
            "Emergency" => 4,
            "Urgent" => 3,
            "Elective" => 2,
            _ => 1
        };
    }

    private static bool IsBlockCompatibleWithRequest(
        BlockAllocationDto block,
        AutoBlockDemandRequestDto request)
    {
        if (block.BlockType == "Recurring")
        {
            return block.SurgeonId == request.SurgeonId;
        }

        if (block.BlockType == "Emergency")
        {
            return request.Priority == "Emergency";
        }

        if (block.BlockType == "AdHoc")
        {
            return !block.SurgeonId.HasValue ||
                   block.SurgeonId.Value == request.SurgeonId;
        }

        return block.BlockType == "Open";
    }

    private static (DateTime Start, DateTime End)? FindEarliestAvailableSlot(
        BlockAllocationDto block,
        int estimatedDurationMin,
        List<SurgicalCaseDto> existingCases)
    {
        var blockDate = block.BlockDate.Date;

        var blockStart = block.StartTime;
        var blockEnd = block.EndTime;

        var blockCases = existingCases
            .Where(surgicalCase =>
                surgicalCase.BlockId == block.BlockId &&
                surgicalCase.CaseStatus != "Cancelled")
            .OrderBy(surgicalCase => surgicalCase.ScheduledStart)
            .ToList();

        var cursor = blockStart;

        foreach (var surgicalCase in blockCases)
        {
            var caseStart = TimeOnly.FromDateTime(surgicalCase.ScheduledStart);
            var caseEnd = TimeOnly.FromDateTime(surgicalCase.ScheduledEnd);

            if (cursor.AddMinutes(estimatedDurationMin) <= caseStart)
            {
                var candidateStart = blockDate.Add(cursor.ToTimeSpan());
                var candidateEnd = candidateStart.AddMinutes(estimatedDurationMin);

                return (candidateStart, candidateEnd);
            }

            if (caseEnd > cursor)
            {
                cursor = caseEnd;
            }
        }

        if (cursor.AddMinutes(estimatedDurationMin) <= blockEnd)
        {
            var candidateStart = blockDate.Add(cursor.ToTimeSpan());
            var candidateEnd = candidateStart.AddMinutes(estimatedDurationMin);

            return (candidateStart, candidateEnd);
        }

        return null;
    }

    private async Task UpdateBlockStatusAfterSchedulingAsync(
        int hospitalId,
        BlockAllocationDto block,
        List<SurgicalCaseDto> existingCases,
        int modifiedByUserId)
    {
        var totalBlockMinutes = (int)Math.Round(
            (block.EndTime - block.StartTime).TotalMinutes);

        if (totalBlockMinutes <= 0)
        {
            return;
        }

        var usedMinutes = existingCases
            .Where(surgicalCase =>
                surgicalCase.BlockId == block.BlockId &&
                surgicalCase.CaseStatus != "Cancelled")
            .Sum(surgicalCase =>
                (int)Math.Round(
                    (surgicalCase.ScheduledEnd - surgicalCase.ScheduledStart).TotalMinutes));

        var newStatus = usedMinutes >= totalBlockMinutes
            ? "FullyBooked"
            : "PartiallyBooked";

        await _blockRepository.UpdateBlockAsync(
            hospitalId,
            block.BlockId,
            new UpdateBlockAllocationDto
            {
                SurgeonId = block.SurgeonId,
                ORRoomId = block.ORRoomId,
                BlockDate = block.BlockDate,
                StartTime = block.StartTime,
                EndTime = block.EndTime,
                BlockType = block.BlockType,
                BlockStatus = newStatus,
                Remarks = block.Remarks
            },
            modifiedByUserId);

        block.BlockStatus = newStatus;
    }
    public async Task<ServiceResultDto<AutoBuildBlocksResultDto>> AutoBuildBlocksAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var cycle = await _cycleRepository.GetCycleByIdAsync(
            hospitalId,
            cycleId);

        if (cycle is null)
        {
            return ServiceResultDto<AutoBuildBlocksResultDto>.Fail(
                "CYCLE_NOT_FOUND",
                "Scheduling cycle was not found.");
        }

        if (cycle.CycleStatus != "Cutoff")
        {
            return ServiceResultDto<AutoBuildBlocksResultDto>.Fail(
                "INVALID_CYCLE_STATUS",
                "Auto build blocks can be run only after cycle cutoff.");
        }

        var activeRoomIds = await _blockRepository.GetActiveRoomIdsAsync(
            hospitalId);

        if (activeRoomIds.Count == 0)
        {
            return ServiceResultDto<AutoBuildBlocksResultDto>.Fail(
                "NO_ACTIVE_ROOM",
                "No active operating room was found for auto block creation.");
        }

        var result = new AutoBuildBlocksResultDto
        {
            CycleId = cycleId
        };

        var weekStart = cycle.WeekStartDate.Date;
        var weekEnd = cycle.WeekEndDate.Date;

        var approvedReadyRequests = await _requestRepository.GetApprovedReadyRequestsForCycleAsync(
            hospitalId,
            cycleId);

        result.Messages.Add(
            $"Approved ready request count considered for capacity: {approvedReadyRequests.Count}.");

        await BuildEmergencyTemplatesAsync(
            hospitalId,
            userId,
            roleName,
            activeRoomIds,
            weekStart,
            weekEnd,
            ipAddress,
            userAgent,
            result);

        await BuildRecurringTemplatesAsync(
            hospitalId,
            userId,
            roleName,
            activeRoomIds,
            weekStart,
            weekEnd,
            approvedReadyRequests,
            ipAddress,
            userAgent,
            result);

        await BuildOpenTemplatesAsync(
            hospitalId,
            userId,
            roleName,
            activeRoomIds,
            weekStart,
            weekEnd,
            approvedReadyRequests,
            ipAddress,
            userAgent,
            result);

        var generateResult = await _blockService.GenerateBlocksAsync(
            hospitalId,
            userId,
            roleName,
            new GenerateBlocksRequestDto
            {
                FromDate = weekStart,
                ToDate = weekEnd
            },
            ipAddress,
            userAgent);

        if (!generateResult.Success)
        {
            result.Messages.Add(
                $"Template creation completed, but block generation failed: {generateResult.Message}");

            return ServiceResultDto<AutoBuildBlocksResultDto>.Ok(
                result,
                "Auto build partially completed.");
        }

        result.BlocksGenerated = generateResult.Data;

        result.Messages.Add(
            $"Generated {generateResult.Data} blocks for cycle #{cycleId}.");

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "AutoBuildBlocks",
            Entity = "SchedulingCycles",
            EntityId = cycleId,
            NewValue = $"TemplatesCreated={result.TemplatesCreated};BlocksGenerated={result.BlocksGenerated}",
            Remarks = "Auto build blocks completed for cutoff cycle.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Auto build blocks completed. CycleId: {CycleId}, TemplatesCreated: {TemplatesCreated}, BlocksGenerated: {BlocksGenerated}",
            cycleId,
            result.TemplatesCreated,
            result.BlocksGenerated);

        return ServiceResultDto<AutoBuildBlocksResultDto>.Ok(
            result,
            "Auto build blocks completed successfully.");
    }

    private async Task BuildEmergencyTemplatesAsync(
        int hospitalId,
        int userId,
        string roleName,
        List<int> activeRoomIds,
        DateTime weekStart,
        DateTime weekEnd,
        string? ipAddress,
        string? userAgent,
        AutoBuildBlocksResultDto result)
    {
        var emergencyRoomId = activeRoomIds.First();

        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                continue;
            }

            var dayNumber = GetDayNumber(date);

            await TryCreateTemplateAsync(
                hospitalId,
                userId,
                roleName,
                roomId: emergencyRoomId,
                surgeonId: null,
                blockType: "Emergency",
                specialty: "Emergency Capacity",
                dayOfWeek: dayNumber,
                startTime: TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
                endTime: TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                effectiveFrom: weekStart,
                effectiveTo: weekEnd,
                ipAddress,
                userAgent,
                result);

            await TryCreateTemplateAsync(
                hospitalId,
                userId,
                roleName,
                roomId: emergencyRoomId,
                surgeonId: null,
                blockType: "Emergency",
                specialty: "Emergency Capacity",
                dayOfWeek: dayNumber,
                startTime: TimeOnly.FromTimeSpan(TimeSpan.FromHours(20)),
                endTime: TimeOnly.FromTimeSpan(TimeSpan.FromHours(22)),
                effectiveFrom: weekStart,
                effectiveTo: weekEnd,
                ipAddress,
                userAgent,
                result);
        }
    }

    private async Task BuildRecurringTemplatesAsync(
        int hospitalId,
        int userId,
        string roleName,
        List<int> activeRoomIds,
        DateTime weekStart,
        DateTime weekEnd,
        List<AutoBlockDemandRequestDto> approvedReadyRequests,
        string? ipAddress,
        string? userAgent,
        AutoBuildBlocksResultDto result)
    {
        var recurringDemandBySurgeon = approvedReadyRequests
    .Where(request => TopRecurringSurgeonIds.Contains(request.SurgeonId))
    .GroupBy(request => request.SurgeonId)
    .Select(group => new
    {
        SurgeonId = group.Key,
        DemandMinutes = group.Sum(request => request.EstimatedDurationMin)
    })
    .Where(item => item.DemandMinutes > 0)
    .OrderByDescending(item => item.DemandMinutes)
    .ToList();

        foreach (var demand in recurringDemandBySurgeon)
        {
            var createdMinutes = await CreateDemandTemplatesAsync(
                hospitalId,
                userId,
                roleName,
                activeRoomIds,
                surgeonId: demand.SurgeonId,
                blockType: "Recurring",
                specialty: "Recurring Capacity",
                requiredMinutes: demand.DemandMinutes,
                weekStart,
                weekEnd,
                startHour: 10,
                endHour: 20,
                ipAddress,
                userAgent,
                result);

            result.Messages.Add(
                $"Recurring demand for surgeon #{demand.SurgeonId}: required {demand.DemandMinutes} min, template capacity created {createdMinutes} min.");
        }
    }
   
    private async Task BuildOpenTemplatesAsync(
        int hospitalId,
        int userId,
        string roleName,
        List<int> activeRoomIds,
        DateTime weekStart,
        DateTime weekEnd,
        List<AutoBlockDemandRequestDto> approvedReadyRequests,
        string? ipAddress,
        string? userAgent,
        AutoBuildBlocksResultDto result)
    {
    var openDemandMinutes = approvedReadyRequests
        .Where(request =>
            !TopRecurringSurgeonIds.Contains(request.SurgeonId) &&
            request.Priority != "Emergency")
        .Sum(request => request.EstimatedDurationMin);

    if (openDemandMinutes <= 0)
    {
        result.Messages.Add("No Open capacity demand found.");
        return;
    }

    var createdMinutes = await CreateDemandTemplatesAsync(
        hospitalId,
        userId,
        roleName,
        activeRoomIds,
        surgeonId: null,
        blockType: "Open",
        specialty: "Open Capacity",
        requiredMinutes: openDemandMinutes,
        weekStart,
        weekEnd,
        startHour: 10,
        endHour: 20,
        ipAddress,
        userAgent,
        result);

    result.Messages.Add(
        $"Open demand: required {openDemandMinutes} min, template capacity created {createdMinutes} min.");
}

private async Task<int> CreateDemandTemplatesAsync(
    int hospitalId,
    int userId,
    string roleName,
    List<int> activeRoomIds,
    int? surgeonId,
    string blockType,
    string specialty,
    int requiredMinutes,
    DateTime weekStart,
    DateTime weekEnd,
    int startHour,
    int endHour,
    string? ipAddress,
    string? userAgent,
    AutoBuildBlocksResultDto result)
{
    var remainingMinutes = requiredMinutes;
    var createdMinutes = 0;

    const int maxTemplateMinutes = 360;
    const int minimumUsefulTemplateMinutes = 30;

    while (remainingMinutes > 0)
    {
        var chunkMinutes = Math.Min(remainingMinutes, maxTemplateMinutes);

        if (chunkMinutes < minimumUsefulTemplateMinutes)
        {
            chunkMinutes = minimumUsefulTemplateMinutes;
        }

        var slot = await FindAvailableTemplateSlotAsync(
            hospitalId,
            activeRoomIds,
            weekStart,
            weekEnd,
            startHour,
            endHour,
            chunkMinutes);

        if (slot is null)
        {
            result.SkippedCount++;
            result.Messages.Add(
                $"Could not create {blockType} template for remaining {remainingMinutes} min because no non-overlapping room/day slot was available.");

            break;
        }

        var createSucceeded = await TryCreateTemplateAsync(
            hospitalId,
            userId,
            roleName,
            slot.RoomId,
            surgeonId,
            blockType,
            specialty,
            slot.DayOfWeek,
            slot.StartTime,
            slot.EndTime,
            weekStart,
            weekEnd,
            ipAddress,
            userAgent,
            result);

        if (!createSucceeded)
        {
            result.SkippedCount++;
            break;
        }

        createdMinutes += chunkMinutes;
        remainingMinutes -= chunkMinutes;
    }

    return createdMinutes;
}

private async Task<TemplateSlot?> FindAvailableTemplateSlotAsync(
    int hospitalId,
    List<int> activeRoomIds,
    DateTime weekStart,
    DateTime weekEnd,
    int startHour,
    int endHour,
    int requiredMinutes)
{
    for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            continue;
        }

        var dayNumber = GetDayNumber(date);

        foreach (var roomId in activeRoomIds)
        {
            var windowStart = TimeOnly.FromTimeSpan(TimeSpan.FromHours(startHour));
            var windowEnd = TimeOnly.FromTimeSpan(TimeSpan.FromHours(endHour));

            var cursor = windowStart;

            while (cursor.AddMinutes(requiredMinutes) <= windowEnd)
            {
                var candidateEnd = cursor.AddMinutes(requiredMinutes);

                var overlapExists = await _blockRepository.TemplateOverlapExistsAsync(
                    hospitalId,
                    roomId,
                    dayNumber,
                    cursor,
                    candidateEnd);

                if (!overlapExists)
                {
                    return new TemplateSlot
                    {
                        RoomId = roomId,
                        DayOfWeek = dayNumber,
                        StartTime = cursor,
                        EndTime = candidateEnd
                    };
                }

                cursor = cursor.AddMinutes(30);
            }
        }
    }

    return null;
}

private async Task<bool> TryCreateTemplateAsync(
    int hospitalId,
    int userId,
    string roleName,
    int roomId,
    int? surgeonId,
    string blockType,
    string specialty,
    int dayOfWeek,
    TimeOnly startTime,
    TimeOnly endTime,
    DateTime effectiveFrom,
    DateTime? effectiveTo,
    string? ipAddress,
    string? userAgent,
    AutoBuildBlocksResultDto result)
{
    var normalizedSurgeonId = blockType == "Recurring"
        ? surgeonId
        : null;

    var duplicateExists = await _blockRepository.TemplateDuplicateExistsAsync(
        hospitalId,
        roomId,
        normalizedSurgeonId,
        dayOfWeek,
        startTime,
        endTime,
        blockType);

    if (duplicateExists)
    {
        result.SkippedCount++;
        result.Messages.Add(
            $"Skipped {blockType} template for room #{roomId}, day {dayOfWeek}, {startTime}-{endTime}: already exists.");

        return false;
    }

    var overlapExists = await _blockRepository.TemplateOverlapExistsAsync(
        hospitalId,
        roomId,
        dayOfWeek,
        startTime,
        endTime);

    if (overlapExists)
    {
        result.SkippedCount++;
        result.Messages.Add(
            $"Skipped {blockType} template for room #{roomId}, day {dayOfWeek}, {startTime}-{endTime}: overlaps existing template.");

        return false;
    }

    var createResult = await _blockService.CreateTemplateAsync(
        hospitalId,
        userId,
        roleName,
        new CreateBlockTemplateDto
        {
            BlockType = blockType,
            SurgeonId = normalizedSurgeonId,
            ORRoomId = roomId,
            Specialty = specialty,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        },
        ipAddress,
        userAgent);

    if (!createResult.Success)
    {
        result.SkippedCount++;
        result.Messages.Add(
            $"Failed to create {blockType} template for room #{roomId}, day {dayOfWeek}, {startTime}-{endTime}: {createResult.Message}");

        return false;
    }

    result.TemplatesCreated++;
    result.Messages.Add(
        $"Created {blockType} template #{createResult.Data} for room #{roomId}, day {dayOfWeek}, {startTime}-{endTime}.");

    return true;
}

private static int GetDayNumber(DateTime date)
{
    return date.DayOfWeek switch
    {
        DayOfWeek.Monday => 1,
        DayOfWeek.Tuesday => 2,
        DayOfWeek.Wednesday => 3,
        DayOfWeek.Thursday => 4,
        DayOfWeek.Friday => 5,
        DayOfWeek.Saturday => 6,
        DayOfWeek.Sunday => 7,
        _ => 1
    };
}

private sealed class TemplateSlot
{
    public int RoomId { get; init; }

    public int DayOfWeek { get; init; }

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }
}
}