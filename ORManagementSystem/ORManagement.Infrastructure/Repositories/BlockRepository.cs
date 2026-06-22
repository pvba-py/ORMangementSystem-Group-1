using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly ORManagementDbContext _dbContext;

    public BlockRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CreateBlockAllocationAsync(
    int hospitalId,
    CreateBlockAllocationDto request)
    {
        var block = new BlockAllocation
        {
            HospitalId = hospitalId,
            TemplateId = null,
            SurgeonId = request.SurgeonId,
            ORRoomId = request.ORRoomId,
            BlockDate = DateOnly.FromDateTime(request.BlockDate.Date),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BlockType = request.BlockType,
            BlockStatus = "Allocated",
            Remarks = request.Remarks
        };

        await _dbContext.BlockAllocations.AddAsync(block);
        await _dbContext.SaveChangesAsync();

        return block.BlockId;
    }
    public async Task<List<BlockTemplateDto>> GetTemplatesAsync(int hospitalId)
    {
        return await
            (
                from template in _dbContext.RecurringBlockTemplates
                join surgeon in _dbContext.Surgeons on template.SurgeonId equals surgeon.SurgeonId
                join user in _dbContext.Users on surgeon.UserId equals user.UserId
                join room in _dbContext.OperatingRooms on template.ORRoomId equals room.ORRoomId
                where surgeon.HospitalId == hospitalId
                orderby template.DayOfWeek, template.StartTime
                select new BlockTemplateDto
                {
                    TemplateId = template.TemplateId,
                    SurgeonId = template.SurgeonId,
                    ORRoomId = template.ORRoomId,
                    SurgeonName = user.FullName,
                    RoomName = room.RoomName,
                    Specialty = template.Specialty,
                    DayOfWeek = template.DayOfWeek,
                    StartTime = template.StartTime,
                    EndTime = template.EndTime,
                    EffectiveFrom = template.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
                    EffectiveTo = template.EffectiveTo.HasValue
                        ? template.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue)
                        : null,
                    IsActive = template.IsActive
                }
            )
            .ToListAsync();
    }

    public async Task<BlockTemplateDto?> GetTemplateByIdAsync(int hospitalId, int templateId)
    {
        return await
            (
                from template in _dbContext.RecurringBlockTemplates
                join surgeon in _dbContext.Surgeons on template.SurgeonId equals surgeon.SurgeonId
                join user in _dbContext.Users on surgeon.UserId equals user.UserId
                join room in _dbContext.OperatingRooms on template.ORRoomId equals room.ORRoomId
                where surgeon.HospitalId == hospitalId
                      && template.TemplateId == templateId
                select new BlockTemplateDto
                {
                    TemplateId = template.TemplateId,
                    SurgeonId = template.SurgeonId,
                    ORRoomId = template.ORRoomId,
                    SurgeonName = user.FullName,
                    RoomName = room.RoomName,
                    Specialty = template.Specialty,
                    DayOfWeek = template.DayOfWeek,
                    StartTime = template.StartTime,
                    EndTime = template.EndTime,
                    EffectiveFrom = template.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
                    EffectiveTo = template.EffectiveTo.HasValue
                        ? template.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue)
                        : null,
                    IsActive = template.IsActive
                }
            )
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateTemplateAsync(CreateBlockTemplateDto request)
    {
        var entity = new RecurringBlockTemplate
        {
            SurgeonId = request.SurgeonId,
            ORRoomId = request.ORRoomId,
            Specialty = request.Specialty.Trim(),
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EffectiveFrom = DateOnly.FromDateTime(request.EffectiveFrom.Date),
            EffectiveTo = request.EffectiveTo.HasValue
                ? DateOnly.FromDateTime(request.EffectiveTo.Value.Date)
                : null,
            IsActive = true
        };

        await _dbContext.RecurringBlockTemplates.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return entity.TemplateId;
    }

    public async Task<bool> UpdateTemplateAsync(
        int hospitalId,
        int templateId,
        UpdateBlockTemplateDto request)
    {
        var entity = await
            (
                from template in _dbContext.RecurringBlockTemplates
                join surgeon in _dbContext.Surgeons on template.SurgeonId equals surgeon.SurgeonId
                where template.TemplateId == templateId
                      && surgeon.HospitalId == hospitalId
                select template
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return false;
        }

        entity.SurgeonId = request.SurgeonId;
        entity.ORRoomId = request.ORRoomId;
        entity.Specialty = request.Specialty.Trim();
        entity.DayOfWeek = request.DayOfWeek;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.EffectiveFrom = DateOnly.FromDateTime(request.EffectiveFrom.Date);
        entity.EffectiveTo = request.EffectiveTo.HasValue
            ? DateOnly.FromDateTime(request.EffectiveTo.Value.Date)
            : null;
        entity.IsActive = request.IsActive;
        entity.DeactivatedAt = request.IsActive ? null : DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateTemplateAsync(int hospitalId, int templateId)
    {
        var entity = await
            (
                from template in _dbContext.RecurringBlockTemplates
                join surgeon in _dbContext.Surgeons on template.SurgeonId equals surgeon.SurgeonId
                where template.TemplateId == templateId
                      && surgeon.HospitalId == hospitalId
                select template
            )
            .FirstOrDefaultAsync();

        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.DeactivatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<int> AddExceptionAsync(int templateId, CreateBlockExceptionDto request)
    {
        var entity = new BlockException
        {
            TemplateId = templateId,
            SkipDate = DateOnly.FromDateTime(request.SkipDate.Date),
            Reason = request.Reason
        };

        await _dbContext.BlockExceptions.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return entity.ExceptionId;
    }

    public async Task<bool> DeleteExceptionAsync(int templateId, int exceptionId)
    {
        var entity = await _dbContext.BlockExceptions
            .FirstOrDefaultAsync(exception =>
                exception.TemplateId == templateId &&
                exception.ExceptionId == exceptionId);

        if (entity is null)
        {
            return false;
        }

        _dbContext.BlockExceptions.Remove(entity);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExceptionExistsAsync(int templateId, DateTime skipDate)
    {
        var date = DateOnly.FromDateTime(skipDate.Date);

        return await _dbContext.BlockExceptions
            .AnyAsync(exception =>
                exception.TemplateId == templateId &&
                exception.SkipDate == date);
    }

    public async Task<List<BlockAllocationDto>> GetBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId)
    {
        var query =
    from block in _dbContext.BlockAllocations
    join room in _dbContext.OperatingRooms
        on block.ORRoomId equals room.ORRoomId
    join surgeon in _dbContext.Surgeons
        on block.SurgeonId equals surgeon.SurgeonId into surgeonJoin
    from surgeon in surgeonJoin.DefaultIfEmpty()
    join user in _dbContext.Users
        on surgeon != null ? surgeon.UserId : 0 equals user.UserId into userJoin
    from user in userJoin.DefaultIfEmpty()
    where block.HospitalId == hospitalId
    select new
    {
        block,
        room,
        surgeon,
        user
    };

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(item => item.block.BlockDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value.Date);
            query = query.Where(item => item.block.BlockDate <= to);
        }

        if (surgeonId.HasValue)
        {
            query = query.Where(item => item.block.SurgeonId == surgeonId.Value);
        }

        if (roomId.HasValue)
        {
            query = query.Where(item => item.block.ORRoomId == roomId.Value);
        }

        return await query
            .OrderBy(item => item.block.BlockDate)
            .ThenBy(item => item.block.StartTime)
           .Select(item => new BlockAllocationDto
           {
               BlockId = item.block.BlockId,
               HospitalId = item.block.HospitalId,
               TemplateId = item.block.TemplateId,
               SurgeonId = item.block.SurgeonId,
               SurgeonName = item.user != null
        ? item.user.FullName
        : item.block.BlockType + " Capacity",
               ORRoomId = item.room.ORRoomId,
               RoomName = item.room.RoomName,
               BlockDate = item.block.BlockDate.ToDateTime(TimeOnly.MinValue),
               StartTime = item.block.StartTime,
               EndTime = item.block.EndTime,
               BlockType = item.block.BlockType,
               BlockStatus = item.block.BlockStatus,
               Remarks = item.block.Remarks
           })
            .ToListAsync();
    }

    public async Task<List<BlockAllocationDto>> GetMyBlocksAsync(int hospitalId, int surgeonId)
    {
        return await GetBlocksAsync(hospitalId, null, null, surgeonId, null);
    }

    public async Task<BlockAllocationDto?> GetBlockByIdAsync(int hospitalId, int blockId)
    {
        var blocks = await GetBlocksAsync(hospitalId, null, null, null, null);

        return blocks.FirstOrDefault(block => block.BlockId == blockId);
    }

    public async Task<bool> BlockConflictExistsAsync(
        int hospitalId,
        int roomId,
        DateTime blockDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludeBlockId = null)
    {
        var date = DateOnly.FromDateTime(blockDate.Date);

        var query = _dbContext.BlockAllocations
            .Where(block =>
                block.HospitalId == hospitalId &&
                block.ORRoomId == roomId &&
                block.BlockDate == date &&
                block.BlockStatus != "Cancelled" &&
                startTime < block.EndTime &&
                endTime > block.StartTime);

        if (excludeBlockId.HasValue)
        {
            query = query.Where(block => block.BlockId != excludeBlockId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> CreateBlockAsync(
        int hospitalId,
        int surgeonId,
        int roomId,
        int? templateId,
        DateTime blockDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string blockType,
        string blockStatus,
        string? remarks,
        int modifiedByUserId)
    {
        var entity = new BlockAllocation
        {
            HospitalId = hospitalId,
            SurgeonId = surgeonId,
            ORRoomId = roomId,
            TemplateId = templateId,
            BlockDate = DateOnly.FromDateTime(blockDate.Date),
            StartTime = startTime,
            EndTime = endTime,
            BlockType = blockType,
            BlockStatus = blockStatus,
            Remarks = remarks,
            ModifiedByUserId = modifiedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.BlockAllocations.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return entity.BlockId;
    }

    public async Task<bool> UpdateBlockAsync(
        int hospitalId,
        int blockId,
        UpdateBlockAllocationDto request,
        int modifiedByUserId)
    {
        var entity = await _dbContext.BlockAllocations
            .FirstOrDefaultAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId);

        if (entity is null)
        {
            return false;
        }

        entity.SurgeonId = request.SurgeonId;
        entity.ORRoomId = request.ORRoomId;
        entity.BlockDate = DateOnly.FromDateTime(request.BlockDate.Date);
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.BlockStatus = request.BlockStatus;
        entity.Remarks = request.Remarks;
        entity.ModifiedByUserId = modifiedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CancelBlockAsync(int hospitalId, int blockId, int modifiedByUserId)
    {
        var entity = await _dbContext.BlockAllocations
            .FirstOrDefaultAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId);

        if (entity is null)
        {
            return false;
        }

        entity.BlockStatus = "Cancelled";
        entity.ModifiedByUserId = modifiedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<int> ReleaseBlockAsync(
        int hospitalId,
        int blockId,
        TimeOnly releaseStartTime,
        TimeOnly releaseEndTime,
        string source,
        int? releasedByUserId)
    {
        var block = await _dbContext.BlockAllocations
            .FirstOrDefaultAsync(block =>
                block.HospitalId == hospitalId &&
                block.BlockId == blockId);

        if (block is null)
        {
            return 0;
        }

        var fullBlockReleased =
            releaseStartTime == block.StartTime &&
            releaseEndTime == block.EndTime;

        block.BlockStatus = fullBlockReleased ? "Released" : "PartiallyBooked";
        block.ModifiedByUserId = releasedByUserId;

        var releasedSlot = new ReleasedSlot
        {
            HospitalId = hospitalId,
            BlockId = block.BlockId,
            ORRoomId = block.ORRoomId,
            SlotDate = block.BlockDate,
            StartTime = releaseStartTime,
            EndTime = releaseEndTime,
            Source = source,
            ReleasedByUserId = releasedByUserId,
            SlotState = "Available",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ReleasedSlots.AddAsync(releasedSlot);
        await _dbContext.SaveChangesAsync();

        return releasedSlot.SlotId;
    }

    public async Task<List<BlockTemplateGenerationDto>> GetActiveTemplatesForGenerationAsync(int hospitalId)
    {
        return await
            (
                from template in _dbContext.RecurringBlockTemplates
                join surgeon in _dbContext.Surgeons on template.SurgeonId equals surgeon.SurgeonId
                where surgeon.HospitalId == hospitalId
                      && template.IsActive
                select new BlockTemplateGenerationDto
                {
                    TemplateId = template.TemplateId,
                    SurgeonId = template.SurgeonId,
                    RoomId = template.ORRoomId,
                    Specialty = template.Specialty,
                    DayOfWeek = template.DayOfWeek,
                    StartTime = template.StartTime,
                    EndTime = template.EndTime,
                    EffectiveFrom = template.EffectiveFrom.ToDateTime(TimeOnly.MinValue),
                    EffectiveTo = template.EffectiveTo.HasValue
                        ? template.EffectiveTo.Value.ToDateTime(TimeOnly.MinValue)
                        : null
                }
            )
            .ToListAsync();
    }

    public async Task<bool> IsTemplateExceptionAsync(int templateId, DateTime date)
    {
        var skipDate = DateOnly.FromDateTime(date.Date);

        return await _dbContext.BlockExceptions
            .AnyAsync(exception =>
                exception.TemplateId == templateId &&
                exception.SkipDate == skipDate);
    }

    public async Task<bool> GeneratedBlockExistsAsync(int roomId, DateTime blockDate, TimeOnly startTime)
    {
        var date = DateOnly.FromDateTime(blockDate.Date);

        return await _dbContext.BlockAllocations
            .AnyAsync(block =>
                block.ORRoomId == roomId &&
                block.BlockDate == date &&
                block.StartTime == startTime);
    }
}