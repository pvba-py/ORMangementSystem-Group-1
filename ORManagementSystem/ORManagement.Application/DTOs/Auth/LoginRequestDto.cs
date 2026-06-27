using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 100 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(200, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 200 characters.")]
    public string Password { get; set; } = string.Empty;
}