using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "HospitalId must be a positive number.")]
    public int? HospitalId { get; set; } = 1;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(200, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 200 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is invalid.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Surgeon|ORScheduler)$", ErrorMessage = "Role must be either Surgeon or ORScheduler.")]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Specialty cannot exceed 100 characters.")]
    public string? Specialty { get; set; }
}