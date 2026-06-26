using ORManagement.Application.DTOs.Automation;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IAutoSchedulingService
{
    Task<ServiceResultDto<AutoBuildBlocksResultDto>> AutoBuildBlocksAsync(
        int hospitalId,
        int cycleId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<AutoAssignCasesResultDto>> AutoAssignCasesAsync(
    int hospitalId,
    int cycleId,
    int userId,
    string roleName,
    string? ipAddress,
    string? userAgent);
}