namespace ORManagement.Application.DTOs.MasterData;

public class UserLookupDto
{
    public int UserId { get; set; }
    public int? HospitalId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}