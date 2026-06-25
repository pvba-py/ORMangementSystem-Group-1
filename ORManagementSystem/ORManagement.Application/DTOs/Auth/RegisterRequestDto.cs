using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Auth;

public class RegisterRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Hospital Id must be greater than 0.")]
    public int? HospitalId { get; set; } = 1;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
    [RegularExpression("^(Admin|Doctor|Nurse|Scheduler)$",
        ErrorMessage = "Role name must be one of: Admin, Doctor, Nurse, Scheduler.")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Specialty cannot exceed 100 characters.")]
    public string? Specialty { get; set; }
}
