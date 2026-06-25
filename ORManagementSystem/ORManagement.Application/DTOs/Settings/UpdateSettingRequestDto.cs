using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Settings;

public class UpdateSettingRequestDto
{
    [Required(ErrorMessage = "Setting value is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Setting value must be between 1 and 200 characters.")]
    public string SettingValue { get; set; } = string.Empty;
}