using System.ComponentModel.DataAnnotations;

public class ReleaseBlockRequestDto
{
    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly EndTime { get; set; }

    [StringLength(300, ErrorMessage = "Remarks cannot exceed 300 characters.")]
    public string? Remarks { get; set; }
}