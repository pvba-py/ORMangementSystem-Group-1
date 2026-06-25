using System.ComponentModel.DataAnnotations;

public class UpdateForecastRecommendationStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    [MaxLength(30, ErrorMessage = "Status cannot exceed 30 characters.")]
   
    public string Status { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Scheduler remarks cannot exceed 300 characters.")]
    public string? SchedulerRemarks { get; set; }
}