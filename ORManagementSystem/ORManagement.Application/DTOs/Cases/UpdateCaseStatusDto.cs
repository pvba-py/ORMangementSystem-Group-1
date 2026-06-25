using System.ComponentModel.DataAnnotations;

public class UpdateCaseStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    [MaxLength(30, ErrorMessage = "Status cannot exceed 30 characters.")]
    [RegularExpression("^(Scheduled|InProgress|Completed|Cancelled)$",
        ErrorMessage = "Status must be valid.")]
    public string Status { get; set; } = string.Empty;

    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }

    [StringLength(300, ErrorMessage = "Cancellation reason cannot exceed 300 characters.")]
    public string? CancellationReason { get; set; }
}