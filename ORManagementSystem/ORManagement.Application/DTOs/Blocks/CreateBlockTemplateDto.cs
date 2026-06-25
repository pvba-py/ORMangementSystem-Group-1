using System.ComponentModel.DataAnnotations;

public class CreateBlockTemplateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Surgeon Id must be greater than 0.")]
    public int? SurgeonId { get; set; }

    [Required(ErrorMessage = "OR Room Id is required.")]
    [Range(1, int.MaxValue)]
    public int ORRoomId { get; set; }

    [MaxLength(100, ErrorMessage = "Specialty cannot exceed 100 characters.")]
    public string? Specialty { get; set; }

    [Required(ErrorMessage = "Day of week is required.")]
    [Range(1, 7, ErrorMessage = "Day of week must be between 1 and 7.")]
    public int DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Effective From is required.")]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Block type is required.")]
    [RegularExpression("^(Recurring|Adhoc)$",
        ErrorMessage = "Block type must be either Recurring or Adhoc.")]
    public string BlockType { get; set; } = "Recurring";
}