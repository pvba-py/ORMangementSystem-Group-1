using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class BlockService : IBlockService
{
    private readonly IBlockRepository _blockRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<BlockService> _logger;

    private static readonly HashSet<string> AllowedBlockStatuses = new()
    {
        "Allocated",
        "PartiallyBooked",
        "FullyBooked",
        "Released",
        "Cancelled"
    };

    public BlockService(
        IBlockRepository blockRepository,
        IAuditRepository auditRepository,
        ILogger<BlockService> logger)
    {
        _blockRepository = blockRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<BlockTemplateDto>>> GetTemplatesAsync(int hospitalId)
    {
        var templates = await _blockRepository.GetTemplatesAsync(hospitalId);

        return ServiceResultDto<List<BlockTemplateDto>>.Ok(templates);
    }

    public async Task<ServiceResultDto<int>> CreateTemplateAsync(
        int hospitalId,
        int userId,
        string roleName,
        CreateBlockTemplateDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (request.EndTime <= request.StartTime)
        {
            return ServiceResultDto<int>.Fail("INVALID_BLOCK_TIME", "End time must be after start time.");
        }
        var duplicateExists = await _blockRepository.TemplateDuplicateExistsAsync(
    hospitalId,
    request.ORRoomId,
    request.BlockType == "Recurring" ? request.SurgeonId : null,
    request.DayOfWeek,
    request.StartTime,
    request.EndTime,
    request.BlockType);

        if (duplicateExists)
        {
            return ServiceResultDto<int>.Fail(
                "DUPLICATE_TEMPLATE",
                "A matching block template already exists for the selected room, surgeon, day, time, and block type.");
        }
        


        var overlapExists = await _blockRepository.TemplateOverlapExistsAsync(
            hospitalId,
            request.ORRoomId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime);

        if (overlapExists)
        {
            return ServiceResultDto<int>.Fail(
                "TEMPLATE_OVERLAP",
                "Template time overlaps with an existing active template for the selected room and day.");
        }
        var templateId = await _blockRepository.CreateTemplateAsync(request);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockTemplateCreated",
            Entity = "RecurringBlockTemplates",
            EntityId = templateId,
            NewValue = $"{request.DayOfWeek}|{request.StartTime}-{request.EndTime}",
            Remarks = "Recurring block template created.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation("Block template created. TemplateId: {TemplateId}, UserId: {UserId}", templateId, userId);

        return ServiceResultDto<int>.Ok(templateId, "Block template created successfully.");
    }

    public async Task<ServiceResultDto> UpdateTemplateAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        UpdateBlockTemplateDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (request.EndTime <= request.StartTime)
        {
            return ServiceResultDto.Fail("INVALID_BLOCK_TIME", "End time must be after start time.");
        }

        var existing = await _blockRepository.GetTemplateByIdAsync(hospitalId, templateId);

        if (existing is null)
        {
            return ServiceResultDto.Fail("TEMPLATE_NOT_FOUND", "Block template was not found.");
        }
        var duplicateExists = await _blockRepository.TemplateDuplicateExistsAsync(
    hospitalId,
    request.ORRoomId,
    request.BlockType == "Recurring" ? request.SurgeonId : null,
    request.DayOfWeek,
    request.StartTime,
    request.EndTime,
    request.BlockType,
    excludeTemplateId: templateId);

        var overlapExists = await _blockRepository.TemplateOverlapExistsAsync(
            hospitalId,
            request.ORRoomId,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime,
            excludeTemplateId: templateId);

        if (overlapExists)
        {
            return ServiceResultDto.Fail(
                "TEMPLATE_OVERLAP",
                "Template time overlaps with another active template for the selected room and day.");
        }

        if (duplicateExists)
        {
            return ServiceResultDto.Fail(
                "DUPLICATE_TEMPLATE",
                "Another matching block template already exists for the selected room, surgeon, day, time, and block type.");
        }

        var updated = await _blockRepository.UpdateTemplateAsync(hospitalId, templateId, request);

        if (!updated)
        {
            return ServiceResultDto.Fail("TEMPLATE_UPDATE_FAILED", "Block template could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockTemplateUpdated",
            Entity = "RecurringBlockTemplates",
            EntityId = templateId,
            OldValue = $"{existing.DayOfWeek}|{existing.StartTime}-{existing.EndTime}|{existing.IsActive}",
            NewValue = $"{request.DayOfWeek}|{request.StartTime}-{request.EndTime}|{request.IsActive}",
            Remarks = "Recurring block template updated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block template updated successfully.");
    }
    public async Task<ServiceResultDto> DeleteTemplateAsync(
    int hospitalId,
    int templateId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent)
    {
        var existing = await _blockRepository.GetTemplateByIdAsync(
            hospitalId,
            templateId);

        if (existing is null)
        {
            return ServiceResultDto.Fail(
                "TEMPLATE_NOT_FOUND",
                "Block template was not found.");
        }

        var deleted = await _blockRepository.DeleteTemplateAsync(
            hospitalId,
            templateId);

        if (!deleted)
        {
            return ServiceResultDto.Fail(
                "TEMPLATE_DELETE_FAILED",
                "Block template could not be deleted.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockTemplateDeleted",
            Entity = "RecurringBlockTemplates",
            EntityId = templateId,
            OldValue = $"{existing.DayOfWeek}|{existing.StartTime}-{existing.EndTime}|{existing.BlockType}|{existing.IsActive}",
            NewValue = "Deleted",
            Remarks = "Recurring block template deleted. Existing generated blocks were not removed.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block template deleted successfully.");
    }
    public async Task<ServiceResultDto> DeactivateTemplateAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var existing = await _blockRepository.GetTemplateByIdAsync(hospitalId, templateId);

        if (existing is null)
        {
            return ServiceResultDto.Fail("TEMPLATE_NOT_FOUND", "Block template was not found.");
        }

        var deactivated = await _blockRepository.DeactivateTemplateAsync(hospitalId, templateId);

        if (!deactivated)
        {
            return ServiceResultDto.Fail("TEMPLATE_DEACTIVATE_FAILED", "Block template could not be deactivated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockTemplateDeactivated",
            Entity = "RecurringBlockTemplates",
            EntityId = templateId,
            OldValue = "Active",
            NewValue = "Inactive",
            Remarks = "Recurring block template deactivated.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block template deactivated successfully.");
    }

    public async Task<ServiceResultDto<int>> AddExceptionAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        CreateBlockExceptionDto request,
        string? ipAddress,
        string? userAgent)
    {
        var template = await _blockRepository.GetTemplateByIdAsync(hospitalId, templateId);

        if (template is null)
        {
            return ServiceResultDto<int>.Fail("TEMPLATE_NOT_FOUND", "Block template was not found.");
        }

        var exists = await _blockRepository.ExceptionExistsAsync(templateId, request.SkipDate);

        if (exists)
        {
            return ServiceResultDto<int>.Fail("EXCEPTION_ALREADY_EXISTS", "Exception already exists for this date.");
        }

        var exceptionId = await _blockRepository.AddExceptionAsync(templateId, request);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockExceptionAdded",
            Entity = "BlockExceptions",
            EntityId = exceptionId,
            NewValue = request.SkipDate.Date.ToString("yyyy-MM-dd"),
            Remarks = request.Reason,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<int>.Ok(exceptionId, "Block exception added successfully.");
    }

    public async Task<ServiceResultDto> DeleteExceptionAsync(
        int hospitalId,
        int templateId,
        int exceptionId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var template = await _blockRepository.GetTemplateByIdAsync(hospitalId, templateId);

        if (template is null)
        {
            return ServiceResultDto.Fail("TEMPLATE_NOT_FOUND", "Block template was not found.");
        }

        var deleted = await _blockRepository.DeleteExceptionAsync(templateId, exceptionId);

        if (!deleted)
        {
            return ServiceResultDto.Fail("EXCEPTION_NOT_FOUND", "Block exception was not found.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockExceptionRemoved",
            Entity = "BlockExceptions",
            EntityId = exceptionId,
            OldValue = "Exists",
            NewValue = "Deleted",
            Remarks = "Block exception removed.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block exception removed successfully.");
    }

    public async Task<ServiceResultDto<int>> GenerateBlocksAsync(
     int hospitalId,
     int userId,
     string roleName,
     GenerateBlocksRequestDto request,
     string? ipAddress,
     string? userAgent)
    {
        if (request.ToDate.Date < request.FromDate.Date)
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_DATE_RANGE",
                "To date cannot be before From date.");
        }

        var templates = await _blockRepository.GetActiveTemplatesForGenerationAsync(
            hospitalId);

        var generatedCount = 0;
        var skippedCount = 0;

        for (var date = request.FromDate.Date; date <= request.ToDate.Date; date = date.AddDays(1))
        {
            var dayNumber = GetDayNumber(date);

            foreach (var template in templates)
            {
                if (template.DayOfWeek != dayNumber)
                {
                    continue;
                }

                if (date < template.EffectiveFrom.Date)
                {
                    continue;
                }

                if (template.EffectiveTo.HasValue &&
                    date > template.EffectiveTo.Value.Date)
                {
                    continue;
                }

                var isException = await _blockRepository.IsTemplateExceptionAsync(
                    template.TemplateId,
                    date);

                if (isException)
                {
                    skippedCount++;
                    continue;
                }

                /*
                    Full overlap check:
                    Prevents generated blocks from overlapping in same OR room/date/time.
                    This is better than checking only same room/date/start time.
                */
                var conflictExists = await _blockRepository.BlockConflictExistsAsync(
                    hospitalId,
                    template.RoomId,
                    date,
                    template.StartTime,
                    template.EndTime);

                if (conflictExists)
                {
                    skippedCount++;
                    continue;
                }

                /*
                    Template block type rules:
                    Recurring  -> keep surgeon
                    Open       -> no surgeon
                    Emergency  -> no surgeon
                */
                int? generatedSurgeonId = template.BlockType == "Recurring"
                    ? template.SurgeonId
                    : null;

                if (template.BlockType == "Recurring" && generatedSurgeonId is null)
                {
                    skippedCount++;
                    continue;
                }

                await _blockRepository.CreateBlockAsync(
                    hospitalId,
                    generatedSurgeonId,
                    template.RoomId,
                    template.TemplateId,
                    date,
                    template.StartTime,
                    template.EndTime,
                    template.BlockType,
                    "Allocated",
                    $"Generated from {template.BlockType} template #{template.TemplateId}.",
                    userId);

                generatedCount++;
            }
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlocksGenerated",
            Entity = "BlockAllocations",
            EntityId = null,
            NewValue = generatedCount.ToString(),
            Remarks = $"Generated {generatedCount} blocks and skipped {skippedCount} from {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<int>.Ok(
            generatedCount,
            $"Blocks generated successfully. Generated: {generatedCount}, Skipped: {skippedCount}.");
    }

    public async Task<ServiceResultDto<List<BlockAllocationDto>>> GetBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId)
    {
        var blocks = await _blockRepository.GetBlocksAsync(
            hospitalId,
            fromDate,
            toDate,
            surgeonId,
            roomId);

        return ServiceResultDto<List<BlockAllocationDto>>.Ok(blocks);
    }

    public async Task<ServiceResultDto<List<BlockAllocationDto>>> GetMyBlocksAsync(
        int hospitalId,
        int surgeonId)
    {
        var blocks = await _blockRepository.GetMyBlocksAsync(hospitalId, surgeonId);

        return ServiceResultDto<List<BlockAllocationDto>>.Ok(blocks);
    }

    public async Task<ServiceResultDto> UpdateBlockAsync(
        int hospitalId,
        int blockId,
        int userId,
        string roleName,
        UpdateBlockAllocationDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (request.EndTime <= request.StartTime)
        {
            return ServiceResultDto.Fail("INVALID_BLOCK_TIME", "End time must be after start time.");
        }

        if (!AllowedBlockStatuses.Contains(request.BlockStatus))
        {
            return ServiceResultDto.Fail("INVALID_BLOCK_STATUS", "Invalid block status.");
        }

        var existing = await _blockRepository.GetBlockByIdAsync(hospitalId, blockId);

        if (existing is null)
        {
            return ServiceResultDto.Fail("BLOCK_NOT_FOUND", "Block was not found.");
        }

        var conflictExists = await _blockRepository.BlockConflictExistsAsync(
            hospitalId,
            request.ORRoomId,
            request.BlockDate,
            request.StartTime,
            request.EndTime,
            blockId);

        if (conflictExists)
        {
            return ServiceResultDto.Fail("BLOCK_CONFLICT", "Block conflicts with another block.");
        }

        var updated = await _blockRepository.UpdateBlockAsync(
            hospitalId,
            blockId,
            request,
            userId);

        if (!updated)
        {
            return ServiceResultDto.Fail("BLOCK_UPDATE_FAILED", "Block could not be updated.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockUpdated",
            Entity = "BlockAllocations",
            EntityId = blockId,
            OldValue = $"{existing.BlockDate:yyyy-MM-dd}|{existing.StartTime}-{existing.EndTime}|{existing.BlockStatus}",
            NewValue = $"{request.BlockDate:yyyy-MM-dd}|{request.StartTime}-{request.EndTime}|{request.BlockStatus}",
            Remarks = request.Remarks,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block updated successfully.");
    }

    public async Task<ServiceResultDto> CancelBlockAsync(
        int hospitalId,
        int blockId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var existing = await _blockRepository.GetBlockByIdAsync(hospitalId, blockId);

        if (existing is null)
        {
            return ServiceResultDto.Fail("BLOCK_NOT_FOUND", "Block was not found.");
        }
        var hasCases = await _blockRepository.BlockHasCasesAsync(hospitalId, blockId);

        if (hasCases)
        {
            return ServiceResultDto.Fail(
                "BLOCK_HAS_CASES",
                "This block cannot be deleted because surgical cases are scheduled in it.");
        }

        var hasReleasedSlots = await _blockRepository.BlockHasReleasedSlotsAsync(hospitalId, blockId);

        if (hasReleasedSlots)
        {
            return ServiceResultDto.Fail(
                "BLOCK_HAS_RELEASED_SLOTS",
                "This block cannot be deleted because released slots exist for it.");
        }

        var hasUtilizationRecords = await _blockRepository.BlockHasUtilizationRecordsAsync(blockId);

        if (hasUtilizationRecords)
        {
            return ServiceResultDto.Fail(
                "BLOCK_HAS_UTILIZATION_RECORDS",
                "This block cannot be deleted because utilization records exist for it.");
        }
        var cancelled = await _blockRepository.CancelBlockAsync(hospitalId, blockId, userId);

        if (!cancelled)
        {
            return ServiceResultDto.Fail("BLOCK_CANCEL_FAILED", "Block could not be cancelled.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockCancelled",
            Entity = "BlockAllocations",
            EntityId = blockId,
            OldValue = existing.BlockStatus,
            NewValue = "Cancelled",
            Remarks = "Block cancelled by scheduler.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto.Ok("Block deleted successfully.");
    }

    public async Task<ServiceResultDto<int>> ReleaseBlockAsync(
        int hospitalId,
        int blockId,
        int? surgeonId,
        int userId,
        string roleName,
        ReleaseBlockRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (request.EndTime <= request.StartTime)
        {
            return ServiceResultDto<int>.Fail("INVALID_RELEASE_TIME", "Release end time must be after start time.");
        }

        var existing = await _blockRepository.GetBlockByIdAsync(hospitalId, blockId);

        if (existing is null)
        {
            return ServiceResultDto<int>.Fail("BLOCK_NOT_FOUND", "Block was not found.");
        }

        if (roleName == "Surgeon")
        {
            if (!surgeonId.HasValue || existing.SurgeonId != surgeonId.Value)
            {
                return ServiceResultDto<int>.Fail("FORBIDDEN_BLOCK_RELEASE", "Surgeons can release only their own blocks.");
            }
        }

        if (request.StartTime < existing.StartTime || request.EndTime > existing.EndTime)
        {
            return ServiceResultDto<int>.Fail("INVALID_RELEASE_TIME", "Release time must be within block time.");
        }
        if (existing.BlockStatus == "FullyBooked")
        {
            return ServiceResultDto<int>.Fail(
                "BLOCK_FULLY_BOOKED",
                "This block is fully booked. Release is not allowed unless scheduled cases are moved or cancelled first.");
        }
        var releaseOverlapsCases = await _blockRepository.ReleaseWindowHasCasesAsync(
    hospitalId,
    blockId,
    request.StartTime,
    request.EndTime);

        if (releaseOverlapsCases)
        {
            return ServiceResultDto<int>.Fail(
                "RELEASE_OVERLAPS_CASE",
                "Release time overlaps with an existing scheduled case in this block.");
        }
        var releaseSlotConflictExists = await _blockRepository.ReleaseSlotConflictExistsAsync(
    hospitalId,
    blockId,
    request.StartTime,
    request.EndTime);

        if (releaseSlotConflictExists)
        {
            return ServiceResultDto<int>.Fail(
                "RELEASE_SLOT_CONFLICT",
                "Release time overlaps with an existing released slot for this block.");
        }

        var source = roleName == "Surgeon"
            ? "SurgeonReleased"
            : "SchedulerReleased";

        var slotId = await _blockRepository.ReleaseBlockAsync(
            hospitalId,
            blockId,
            request.StartTime,
            request.EndTime,
            source,
            userId);

        if (slotId == 0)
        {
            return ServiceResultDto<int>.Fail("BLOCK_RELEASE_FAILED", "Block could not be released.");
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockReleased",
            Entity = "ReleasedSlots",
            EntityId = slotId,
            OldValue = existing.BlockStatus,
            NewValue = "Available",
            Remarks = request.Remarks,
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<int>.Ok(slotId, "Block released successfully.");
    }

    private static byte GetDayNumber(DateTime date)
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
    public async Task<ServiceResultDto<int>> CreateBlockAllocationAsync(
    int hospitalId,
    int userId,
    string roleName,
    CreateBlockAllocationDto request,
    string? ipAddress,
    string? userAgent)
    {
        request.BlockType = NormalizeBlockType(request.BlockType);

        if (!AllowedBlockTypes.Contains(request.BlockType))
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_BLOCK_TYPE",
                "Invalid block type.");
        }

        if (request.StartTime >= request.EndTime)
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_BLOCK_TIME",
                "Block start time must be before end time.");
        }

        if (request.BlockType == "Recurring" && request.SurgeonId is null)
        {
            return ServiceResultDto<int>.Fail(
                "SURGEON_REQUIRED_FOR_RECURRING_BLOCK",
                "Recurring blocks require a surgeon.");
        }

        if (request.BlockType == "Open" || request.BlockType == "Emergency")
        {
            request.SurgeonId = null;
        }

        var blockId = await _blockRepository.CreateBlockAllocationAsync(
            hospitalId,
            request);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "BlockCreated",
            Entity = "BlockAllocations",
            EntityId = blockId,
            NewValue = $"{request.BlockType}|{request.BlockDate:yyyy-MM-dd}|{request.StartTime}-{request.EndTime}",
            Remarks = "One-time block created.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        return ServiceResultDto<int>.Ok(
            blockId,
            "Block created successfully.");
    }
    private static string NormalizeBlockType(string blockType)
    {
        return blockType.Trim() switch
        {
            "Recurring" => "Recurring",
            "Open" => "Open",
            "Emergency" => "Emergency",
            "AdHoc" => "AdHoc",
            _ => blockType.Trim()
        };
    }
    private static readonly HashSet<string> AllowedBlockTypes = new()
{
    "Recurring",
    "Open",
    "Emergency",
    "AdHoc"
};
}