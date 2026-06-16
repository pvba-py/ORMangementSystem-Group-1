namespace ORManagement.Application.DTOs.Auth;

public class RefreshTokenRecordDto
{
    public int RefreshTokenId { get; set; }
    public int UserId { get; set; }
    public int? HospitalId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}