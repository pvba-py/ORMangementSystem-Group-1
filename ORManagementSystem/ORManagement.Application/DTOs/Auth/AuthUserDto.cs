namespace ORManagement.Application.DTOs.Auth;

public class AuthUserDto
{
    public int UserId { get; set; }
    public int? HospitalId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int? SurgeonId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
