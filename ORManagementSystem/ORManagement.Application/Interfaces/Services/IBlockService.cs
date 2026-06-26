using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IBlockService
{
    Task<ServiceResultDto<List<BlockTemplateDto>>> GetTemplatesAsync(int hospitalId);

    Task<ServiceResultDto<int>> CreateTemplateAsync(
        int hospitalId,
        int userId,
        string roleName,
        CreateBlockTemplateDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateTemplateAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        UpdateBlockTemplateDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> DeactivateTemplateAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<int>> AddExceptionAsync(
        int hospitalId,
        int templateId,
        int userId,
        string roleName,
        CreateBlockExceptionDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> DeleteExceptionAsync(
        int hospitalId,
        int templateId,
        int exceptionId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<int>> GenerateBlocksAsync(
        int hospitalId,
        int userId,
        string roleName,
        GenerateBlocksRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<List<BlockAllocationDto>>> GetBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId);

    Task<ServiceResultDto<List<BlockAllocationDto>>> GetMyBlocksAsync(int hospitalId, int surgeonId);

    Task<ServiceResultDto> UpdateBlockAsync(
        int hospitalId,
        int blockId,
        int userId,
        string roleName,
        UpdateBlockAllocationDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> CancelBlockAsync(
        int hospitalId,
        int blockId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<int>> ReleaseBlockAsync(
        int hospitalId,
        int blockId,
        int? surgeonId,
        int userId,
        string roleName,
        ReleaseBlockRequestDto request,
        string? ipAddress,
        string? userAgent);
    Task<ServiceResultDto> DeleteTemplateAsync(
    int hospitalId,
    int templateId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent);
    Task<ServiceResultDto<int>> CreateBlockAllocationAsync(
    int hospitalId,
    int userId,
    string roleName,
    CreateBlockAllocationDto request,
    string? ipAddress,
    string? userAgent);
}