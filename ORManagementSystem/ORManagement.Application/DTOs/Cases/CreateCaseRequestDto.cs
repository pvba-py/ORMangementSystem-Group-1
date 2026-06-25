using System.ComponentModel.DataAnnotations;

public class CreateCaseRequestDto
{
    [Required(ErrorMessage = "Request Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Request Id must be greater than 0.")]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Block Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Block Id must be greater than 0.")]
    public int BlockId { get; set; }

    [Required(ErrorMessage = "Scheduled start time is required.")]
    public DateTime ScheduledStart { get; set; }

    [Required(ErrorMessage = "Scheduled end time is required.")]
    public DateTime ScheduledEnd { get; set; }
}