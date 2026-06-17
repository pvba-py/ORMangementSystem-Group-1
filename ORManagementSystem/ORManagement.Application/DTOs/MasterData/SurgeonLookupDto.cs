namespace ORManagement.Application.DTOs.MasterData;

public class SurgeonLookupDto
{
    public int SurgeonId { get; set; }
    public int UserId { get; set; }
    public int HospitalId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}