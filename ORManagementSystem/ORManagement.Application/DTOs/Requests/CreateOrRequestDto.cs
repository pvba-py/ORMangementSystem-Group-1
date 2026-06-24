using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Requests;

public class CreateOrRequestDto
{
    [Required]
    public int PatientId { get; set; }

    public int? CycleId { get; set; }

    public int? OriginalCycleId { get; set; }

    public DateTime? PreferredDate { get; set; }

    public string? PreferredQuarter { get; set; }

    [Required]
    [Range(1, 1440, ErrorMessage = "Estimated duration must be between 1 and 1440 minutes.")]
    public int EstimatedDurationMin { get; set; }

    [Required]
    [MaxLength(100)]
    public string SurgeryType { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Priority { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string PatientReadiness { get; set; } = string.Empty;

    public int? AvailableDaysMask { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}