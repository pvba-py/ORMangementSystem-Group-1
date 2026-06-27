using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.DTOs.Cases;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IBlockRepository
{
    Task<List<BlockTemplateDto>> GetTemplatesAsync(int hospitalId);
    Task<BlockTemplateDto?> GetTemplateByIdAsync(int hospitalId, int templateId);

    Task<int> CreateTemplateAsync(CreateBlockTemplateDto request);
    Task<bool> UpdateTemplateAsync(int hospitalId, int templateId, UpdateBlockTemplateDto request);
    Task<bool> DeactivateTemplateAsync(int hospitalId, int templateId);

    Task<int> AddExceptionAsync(int templateId, CreateBlockExceptionDto request);
    Task<bool> DeleteExceptionAsync(int templateId, int exceptionId);
    Task<bool> ExceptionExistsAsync(int templateId, DateTime skipDate);

    Task<List<BlockAllocationDto>> GetBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId);

    Task<List<BlockAllocationDto>> GetMyBlocksAsync(int hospitalId, int surgeonId);
    Task<BlockAllocationDto?> GetBlockByIdAsync(int hospitalId, int blockId);

    Task<bool> BlockConflictExistsAsync(
        int hospitalId,
        int roomId,
        DateTime blockDate,
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludeBlockId = null);

    Task<int> CreateBlockAsync(
    int hospitalId,
    int? surgeonId,
    int roomId,
    int? templateId,
    DateTime blockDate,
    TimeOnly startTime,
    TimeOnly endTime,
    string blockType,
    string blockStatus,
    string? remarks,
    int modifiedByUserId);

    Task<bool> UpdateBlockAsync(
        int hospitalId,
        int blockId,
        UpdateBlockAllocationDto request,
        int modifiedByUserId);
    Task<bool> BlockHasCasesAsync(int hospitalId, int blockId);

    Task<bool> BlockHasReleasedSlotsAsync(int hospitalId, int blockId);

    Task<bool> BlockHasUtilizationRecordsAsync(int blockId);
    Task<bool> CancelBlockAsync(int hospitalId, int blockId, int modifiedByUserId);

    Task<int> ReleaseBlockAsync(
        int hospitalId,
        int blockId,
        TimeOnly releaseStartTime,
        TimeOnly releaseEndTime,
        string source,
        int? releasedByUserId);

    Task<List<BlockTemplateGenerationDto>> GetActiveTemplatesForGenerationAsync(int hospitalId);
    Task<bool> IsTemplateExceptionAsync(int templateId, DateTime date);
    Task<bool> GeneratedBlockExistsAsync(int roomId, DateTime blockDate, TimeOnly startTime);
    Task<int> CreateBlockAllocationAsync(
    int hospitalId,
    CreateBlockAllocationDto request);
    Task<bool> TemplateDuplicateExistsAsync(
    int hospitalId,
    int roomId,
    int? surgeonId,
    int dayOfWeek,
    TimeOnly startTime,
    TimeOnly endTime,
    string blockType,
    int? excludeTemplateId = null);
    Task<bool> ReleaseWindowHasCasesAsync(
    int hospitalId,
    int blockId,
    TimeOnly releaseStartTime,
    TimeOnly releaseEndTime);

    Task<bool> ReleaseSlotConflictExistsAsync(
        int hospitalId,
        int blockId,
        TimeOnly releaseStartTime,
        TimeOnly releaseEndTime);
    Task<bool> DeleteTemplateAsync(
        int hospitalId,
        int templateId);
    Task<int?> GetFirstActiveRoomIdAsync(int hospitalId);
    Task<List<int>> GetActiveRoomIdsAsync(int hospitalId);

    Task<bool> TemplateOverlapExistsAsync(
        int hospitalId,
        int roomId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int? excludeTemplateId = null);
}