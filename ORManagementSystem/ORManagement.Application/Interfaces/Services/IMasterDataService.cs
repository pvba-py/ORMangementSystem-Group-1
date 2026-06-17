using ORManagement.Application.DTOs.MasterData;
using ORManagement.Application.DTOs.Shared;

namespace ORManagement.Application.Interfaces.Services;

public interface IMasterDataService
{
    Task<ServiceResultDto<List<HospitalLookupDto>>> GetHospitalsAsync();
    Task<ServiceResultDto<List<UserLookupDto>>> GetUsersAsync(int hospitalId);
    Task<ServiceResultDto<List<SurgeonLookupDto>>> GetSurgeonsAsync(int hospitalId);

    Task<ServiceResultDto<List<PatientLookupDto>>> GetPatientsAsync(
        int hospitalId,
        int userId,
        string roleName,
        string? search,
        string? ipAddress,
        string? userAgent);

    Task<ServiceResultDto<PatientLookupDto>> GetPatientByIdAsync(
        int hospitalId,
        int patientId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent);
}