using ORManagement.Application.DTOs.Requests;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IRequestService
{
    Task<ServiceResultDto<List<OrRequestResponseDto>>> GetRequestsAsync(
    int hospitalId,
    int userId,
    string roleName,
    string? status,
    int? cycleId,
    string? ipAddress,
    string? userAgent);

    Task<ServiceResultDto<List<OrRequestResponseDto>>> GetMyRequestsAsync(
    int hospitalId,
    int surgeonId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent);


    Task<ServiceResultDto<OrRequestResponseDto>> GetRequestByIdAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<int>> CreateRequestAsync(
        int hospitalId,
        int surgeonId,
        int userId,
        string roleName,
        CreateOrRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateRequestAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        UpdateOrRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> DeleteRequestAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateRequestStatusAsync(
        int hospitalId,
        int requestId,
        int userId,
        string roleName,
        UpdateRequestStatusDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<RequestScoreDto>> GetRequestScoreAsync(
        int hospitalId,
        int requestId);
}
