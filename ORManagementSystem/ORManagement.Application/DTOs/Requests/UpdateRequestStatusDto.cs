using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Requests;

public class UpdateRequestStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    [MaxLength(30, ErrorMessage = "Status cannot exceed 30 characters.")]
    [RegularExpression("^(Pending|Approved|Rejected|Scheduled|Cancelled)$",
        ErrorMessage = "Status must be one of: Pending, Approved, Rejected, Scheduled, Cancelled.")]
    public string Status { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Scheduler remarks cannot exceed 300 characters.")]
    public string? SchedulerRemarks { get; set; }
}