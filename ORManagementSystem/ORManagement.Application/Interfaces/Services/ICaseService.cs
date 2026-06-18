using ORManagement.Application.DTOs.Cases;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface ICaseService
{
    Task<ServiceResultDto<List<SurgicalCaseDto>>> GetCasesAsync(
        int hospitalId,
        int userId,
        string roleName,
        string? status,
        string? ipAddress,
        string? userAgent);
    Task<ServiceResultDto<List<SurgicalCaseDto>>> GetMyCasesAsync(
        int hospitalId,
        int surgeonId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);
    Task<ServiceResultDto<SurgicalCaseDto>> GetCaseByIdAsync(
        int hospitalId,
        int surgeryId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<int>> CreateCaseAsync(
        int hospitalId,
        int userId,
        string roleName,
        CreateCaseRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateCaseAsync(
        int hospitalId,
        int surgeryId,
        int userId,
        string roleName,
        UpdateCaseRequestDto request,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto> UpdateCaseStatusAsync(
        int hospitalId,
        int surgeryId,
        int userId,
        string roleName,
        UpdateCaseStatusDto request,
        string? ipAddress,
        string? userAgent);
}