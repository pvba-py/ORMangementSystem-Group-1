namespace ORManagement.Application.DTOs.MasterData;

public class PatientLookupDto
{
    public int PatientId { get; set; }
    public int HospitalId { get; set; }

    public string MRN { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }

    public bool IsActive { get; set; }
}