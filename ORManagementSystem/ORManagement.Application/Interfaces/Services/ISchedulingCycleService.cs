using ORManagement.Application.DTOs.Cycles;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface ISchedulingCycleService
{
    Task<ServiceResultDto<SchedulingCycleDto>> GetCurrentCycleAsync(int hospitalId);

    Task<ServiceResultDto<List<RankedRequestDto>>> GetRankedRequestsAsync(
        int hospitalId,
        int cycleId);
    Task<ServiceResultDto<List<SchedulingCycleDto>>> GetCyclesAsync(int hospitalId);
    Task<ServiceResultDto> CutoffCycleAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> StartCycleAsync(
    int hospitalId,
    int cycleId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent);

    Task<ServiceResultDto> CloseCycleAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);
    Task<ServiceResultDto> PublishCycleAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);
}