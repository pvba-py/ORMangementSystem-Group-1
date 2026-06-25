using System.ComponentModel.DataAnnotations;

public class UpdateBlockTemplateDto
{
    [Range(1, int.MaxValue)]
    public int? SurgeonId { get; set; }

    [Required(ErrorMessage = "OR Room Id is required.")]
    [Range(1, int.MaxValue)]
    public int ORRoomId { get; set; }

    [MaxLength(100)]
    public string? Specialty { get; set; }

    [Required(ErrorMessage = "Day of week is required.")]
    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Effective From is required.")]
    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    [Required(ErrorMessage = "Block type is required.")]
    [RegularExpression("^(Recurring|Adhoc|Open|Emergency)$")]
    public string BlockType { get; set; } = "Recurring";
}