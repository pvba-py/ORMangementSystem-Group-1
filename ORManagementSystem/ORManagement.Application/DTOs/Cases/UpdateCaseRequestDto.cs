using System.ComponentModel.DataAnnotations;

public class UpdateCaseRequestDto
{
    [Required(ErrorMessage = "Scheduled start time is required.")]
    public DateTime ScheduledStart { get; set; }

    [Required(ErrorMessage = "Scheduled end time is required.")]
    public DateTime ScheduledEnd { get; set; }
}