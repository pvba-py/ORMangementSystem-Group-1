using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.MasterData;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;

namespace ORManagement.Infrastructure.Repositories;

public class MasterDataRepository : IMasterDataRepository
{
    private readonly ORManagementDbContext _dbContext;

    public MasterDataRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<HospitalLookupDto>> GetHospitalsAsync()
    {
        return await _dbContext.Hospitals
            .Where(hospital => hospital.IsActive)
            .OrderBy(hospital => hospital.HospitalName)
            .Select(hospital => new HospitalLookupDto
            {
                HospitalId = hospital.HospitalId,
                HospitalCode = hospital.HospitalCode,
                HospitalName = hospital.HospitalName,
                City = hospital.City
            })
            .ToListAsync();
    }

    public async Task<List<UserLookupDto>> GetUsersAsync(int hospitalId)
    {
        return await
            (
                from user in _dbContext.Users
                join role in _dbContext.Roles on user.RoleId equals role.RoleId
                where user.HospitalId == hospitalId && user.IsActive
                orderby user.FullName
                select new UserLookupDto
                {
                    UserId = user.UserId,
                    HospitalId = user.HospitalId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    RoleName = role.RoleName,
                    IsActive = user.IsActive
                }
            )
            .ToListAsync();
    }

    public async Task<List<SurgeonLookupDto>> GetSurgeonsAsync(int hospitalId)
    {
        return await
            (
                from surgeon in _dbContext.Surgeons
                join user in _dbContext.Users on surgeon.UserId equals user.UserId
                where surgeon.HospitalId == hospitalId
                      && surgeon.IsActive
                      && user.IsActive
                orderby user.FullName
                select new SurgeonLookupDto
                {
                    SurgeonId = surgeon.SurgeonId,
                    UserId = surgeon.UserId,
                    HospitalId = surgeon.HospitalId,
                    FullName = user.FullName,
                    Specialty = surgeon.Specialty,
                    IsActive = surgeon.IsActive
                }
            )
            .ToListAsync();
    }

    public async Task<List<PatientLookupDto>> GetPatientsAsync(int hospitalId, string? search)
    {
        var query = _dbContext.Patients
            .Where(patient => patient.HospitalId == hospitalId && patient.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(patient =>
                patient.FullName.Contains(term) ||
                patient.MRN.Contains(term));
        }

        return await query
            .OrderBy(patient => patient.FullName)
            .Select(patient => new PatientLookupDto
            {
                PatientId = patient.PatientId,
                HospitalId = patient.HospitalId,
                MRN = patient.MRN,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                IsActive = patient.IsActive
            })
            .ToListAsync();
    }

    public async Task<PatientLookupDto?> GetPatientByIdAsync(int hospitalId, int patientId)
    {
        return await _dbContext.Patients
            .Where(patient =>
                patient.HospitalId == hospitalId &&
                patient.PatientId == patientId &&
                patient.IsActive)
            .Select(patient => new PatientLookupDto
            {
                PatientId = patient.PatientId,
                HospitalId = patient.HospitalId,
                MRN = patient.MRN,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                IsActive = patient.IsActive
            })
            .FirstOrDefaultAsync();
    }
}