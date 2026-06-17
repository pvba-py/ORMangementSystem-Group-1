namespace ORManagement.Application.DTOs.Audit;

public class CreatePhiAccessLogDto
{
    public int HospitalId { get; set; }
    public int UserId { get; set; }
    public int PatientId { get; set; }

    public string AccessType { get; set; } = string.Empty;
    public string? Context { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
