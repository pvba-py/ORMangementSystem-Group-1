using ORManagement.Application.DTOs.MasterData;

namespace ORManagement.Application.Interfaces.Repositories;

public interface IMasterDataRepository
{
    Task<List<HospitalLookupDto>> GetHospitalsAsync();
    Task<List<UserLookupDto>> GetUsersAsync(int hospitalId);
    Task<List<SurgeonLookupDto>> GetSurgeonsAsync(int hospitalId);
    Task<List<PatientLookupDto>> GetPatientsAsync(int hospitalId, string? search);
    Task<PatientLookupDto?> GetPatientByIdAsync(int hospitalId, int patientId);
}