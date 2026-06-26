using ORManagement.Application.DTOs.Automation;
using ORManagement.Application.DTOs.Requests;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IRequestRepository
{
    Task<List<OrRequestResponseDto>> GetRequestsAsync(
        int hospitalId,
        string? status,
        int? cycleId);

    Task<List<OrRequestResponseDto>> GetMyRequestsAsync(
        int hospitalId,
        int surgeonId);

    Task<OrRequestResponseDto?> GetRequestByIdAsync(
        int hospitalId,
        int requestId);

    Task<int> CreateRequestAsync(
        int hospitalId,
        int surgeonId,
        CreateOrRequestDto request);

    Task<bool> UpdateRequestAsync(
        int hospitalId,
        int requestId,
        UpdateOrRequestDto request,
        int modifiedByUserId);

    Task<bool> UpdateRequestStatusAsync(
        int hospitalId,
        int requestId,
        string status,
        string? schedulerRemarks,
        int modifiedByUserId);
    Task<RequestCapacitySummaryDto> GetCapacitySummaryAsync(int hospitalId);

    Task<bool> DeletePendingRequestAsync(
        int hospitalId,
        int requestId,
        int userId);
    Task<CurrentSchedulingCycleDto?> GetCurrentCycleAsync(int hospitalId);
    Task<bool> RemoveFromWaitlistByRequestIdAsync(int requestId);
    Task<bool> AddToWaitlistIfNotExistsAsync(int requestId);

    Task<bool> PatientExistsAsync(int hospitalId, int patientId);
    Task<List<AutoBlockDemandRequestDto>> GetApprovedReadyRequestsForCycleAsync(
    int hospitalId,
    int cycleId);
    Task<List<AutoBlockDemandRequestDto>> GetAutoAssignCandidateRequestsForCycleAsync(
    int hospitalId,
    int cycleId);
}