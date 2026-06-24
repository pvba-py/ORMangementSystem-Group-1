using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.DTOs.Requests;

namespace ORManagement.Application.Interfaces.Repositories;

public interface ICaseRepository
{

    Task<List<SurgicalCaseDto>> GetCasesAsync(int hospitalId, string? status);
    Task<List<SurgicalCaseDto>> GetMyCasesAsync(int hospitalId, int surgeonId);
    Task<SurgicalCaseDto?> GetCaseByIdAsync(int hospitalId, int surgeryId);

    Task<OrRequestResponseDto?> GetRequestForSchedulingAsync(int hospitalId, int requestId);
    Task<bool> BlockExistsForRequestAsync(int hospitalId, int blockId, int surgeonId);
    Task<bool> HasCaseConflictAsync(int hospitalId, int blockId, DateTime scheduledStart, DateTime scheduledEnd, int? excludeSurgeryId = null);

    Task<int> CreateCaseAsync(
        int hospitalId,
        int requestId,
        int blockId,
        int surgeonId,
        int orRoomId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        int modifiedByUserId);

    Task<bool> UpdateCaseAsync(
        int hospitalId,
        int surgeryId,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        int modifiedByUserId);
    Task<BlockBoundaryDto?> GetBlockBoundaryAsync(int hospitalId, int blockId);
    Task<bool> UpdateCaseStatusAsync(
        int hospitalId,
        int surgeryId,
        string status,
        DateTime? actualStart,
        DateTime? actualEnd,
        string? cancellationReason,
        int modifiedByUserId);

    Task<bool> MarkRequestScheduledAsync(int hospitalId, int requestId, int modifiedByUserId);
    Task<int?> GetBlockRoomIdAsync(int hospitalId, int blockId);
    Task CalculateUtilizationForBlockAsync(int blockId);
    Task<bool> SurgeonCaseConflictExistsAsync(
    int hospitalId,
    int surgeonId,
    DateTime scheduledStart,
    DateTime scheduledEnd,
    int? excludeSurgeryId = null);
}