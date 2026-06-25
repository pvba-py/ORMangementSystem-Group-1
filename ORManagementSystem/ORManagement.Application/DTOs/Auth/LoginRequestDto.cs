using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}
