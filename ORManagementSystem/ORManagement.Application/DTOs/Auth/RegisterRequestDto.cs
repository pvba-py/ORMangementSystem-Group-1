using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public int? HospitalId { get; set; } = 1;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string RoleName { get; set; } = string.Empty;

    public string? Specialty { get; set; }
}