using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.MasterData;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class MasterDataService : IMasterDataService
{
    private readonly IMasterDataRepository _masterDataRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<MasterDataService> _logger;

    public MasterDataService(
        IMasterDataRepository masterDataRepository,
        IAuditRepository auditRepository,
        ILogger<MasterDataService> logger)
    {
        _masterDataRepository = masterDataRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<HospitalLookupDto>>> GetHospitalsAsync()
    {
        var hospitals = await _masterDataRepository.GetHospitalsAsync();

        return ServiceResultDto<List<HospitalLookupDto>>.Ok(hospitals);
    }

    public async Task<ServiceResultDto<List<UserLookupDto>>> GetUsersAsync(int hospitalId)
    {
        var users = await _masterDataRepository.GetUsersAsync(hospitalId);

        return ServiceResultDto<List<UserLookupDto>>.Ok(users);
    }

    public async Task<ServiceResultDto<List<SurgeonLookupDto>>> GetSurgeonsAsync(int hospitalId)
    {
        var surgeons = await _masterDataRepository.GetSurgeonsAsync(hospitalId);

        return ServiceResultDto<List<SurgeonLookupDto>>.Ok(surgeons);
    }

    public async Task<ServiceResultDto<List<PatientLookupDto>>> GetPatientsAsync(
    int hospitalId,
    int userId,
    string roleName,
    string? search,
    string? ipAddress,
    string? userAgent)
    {
        var patients = await _masterDataRepository.GetPatientsAsync(hospitalId, search);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = string.IsNullOrWhiteSpace(search)
                ? "PatientListViewed"
                : "PatientSearchPerformed",
            Entity = "Patients",
            EntityId = null,
            OldValue = null,
            NewValue = patients.Count.ToString(),
            Remarks = string.IsNullOrWhiteSpace(search)
                ? $"Patient list viewed. Returned {patients.Count} records."
                : $"Patient search performed. Search term: {search}. Returned {patients.Count} records.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Patient list/search accessed by UserId {UserId}. Count: {Count}",
            userId,
            patients.Count);

        return ServiceResultDto<List<PatientLookupDto>>.Ok(patients);
    }


    public async Task<ServiceResultDto<PatientLookupDto>> GetPatientByIdAsync(
        int hospitalId,
        int patientId,
        int userId,
        string roleName,
        string? ipAddress,
        string? userAgent)
    {
        var patient = await _masterDataRepository.GetPatientByIdAsync(hospitalId, patientId);

        if (patient is null)
        {
            return ServiceResultDto<PatientLookupDto>.Fail(
                "PATIENT_NOT_FOUND",
                "Patient was not found.");
        }

        await _auditRepository.AddPhiAccessLogAsync(new CreatePhiAccessLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            PatientId = patient.PatientId,
            AccessType = "View",
            Context = "Patient detail viewed",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Patient {PatientId} viewed by UserId {UserId}",
            patientId,
            userId);

        return ServiceResultDto<PatientLookupDto>.Ok(patient);
    }
}