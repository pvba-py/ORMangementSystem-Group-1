namespace ORManagement.Application.DTOs.MasterData;

public class HospitalLookupDto
{
    public int HospitalId { get; set; }
    public string HospitalCode { get; set; } = string.Empty;
    public string HospitalName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}