using ORManagement.Application.DTOs.Cycles;

namespace ORManagement.Application.Interfaces.Repositories;

public interface ICycleRepository
{
    Task<SchedulingCycleDto?> GetCurrentCycleAsync(int hospitalId);
    Task<SchedulingCycleDto?> GetCycleByIdAsync(int hospitalId, int cycleId);

    Task<List<RankedRequestDto>> GetRankedRequestsAsync(int cycleId);
    Task<List<SchedulingCycleDto>> GetCyclesAsync(int hospitalId);
    Task<bool> CutoffCycleAsync(int hospitalId, int cycleId);
    Task<bool> PublishCycleAsync(int hospitalId, int cycleId, int modifiedByUserId);
    Task<bool> HasOpenCycleAsync(
    int hospitalId,
    int? excludeCycleId = null);

    Task<bool> StartCycleAsync(
        int hospitalId,
        int cycleId,
        int createdByUserId);

    Task<bool> CloseCycleAsync(
        int hospitalId,
        int cycleId,
        int modifiedByUserId);
}