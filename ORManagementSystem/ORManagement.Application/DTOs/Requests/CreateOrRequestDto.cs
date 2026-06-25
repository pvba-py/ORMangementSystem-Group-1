using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Requests;

public class CreateOrRequestDto
{
    [Required(ErrorMessage = "Patient Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Patient Id must be greater than 0.")]
    public int PatientId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Cycle Id must be greater than 0.")]
    public int? CycleId { get; set; }

<<<<<<< Updated upstream
    [Required]
    public DateTime PreferredDate { get; set; }

    [Required]
    public string PreferredQuarter { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int EstimatedDurationMin { get; set; }

    [Required]
    [StringLength(100)]
    public string SurgeryType { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;

    [Required]
    public string PatientReadiness { get; set; } = string.Empty;

    [StringLength(300)]
=======
    [Range(1, int.MaxValue, ErrorMessage = "Original Cycle Id must be greater than 0.")]
    public int? OriginalCycleId { get; set; }

    [DataType(DataType.Date, ErrorMessage = "Preferred Date must be a valid date.")]
    public DateTime? PreferredDate { get; set; }

    [MaxLength(20, ErrorMessage = "Preferred Quarter cannot exceed 20 characters.")]
    [RegularExpression("^(Q1|Q2|Q3|Q4)$",
        ErrorMessage = "Preferred Quarter must be one of: Q1, Q2, Q3, Q4.")]
    public string? PreferredQuarter { get; set; }

    [Required(ErrorMessage = "Estimated duration is required.")]
    [Range(1, 1440, ErrorMessage = "Estimated duration must be between 1 and 1440 minutes.")]
    public int EstimatedDurationMin { get; set; }

    [Required(ErrorMessage = "Surgery type is required.")]
    [MaxLength(100, ErrorMessage = "Surgery type cannot exceed 100 characters.")]
    [MinLength(3, ErrorMessage = "Surgery type must be at least 3 characters.")]
    public string SurgeryType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required.")]
    [MaxLength(30, ErrorMessage = "Priority cannot exceed 30 characters.")]
    [RegularExpression("^(Elective|Urgent|Emergency)$",
        ErrorMessage = "Priority must be one of: Elective, Urgent, Emergency.")]
    public string Priority { get; set; } = string.Empty;

    [Required(ErrorMessage = "Patient readiness is required.")]
    [MaxLength(30, ErrorMessage = "Patient readiness cannot exceed 30 characters.")]
    [RegularExpression("^(Ready|NotReady|Pending)$",
        ErrorMessage = "Patient readiness must be one of: Ready, NotReady, Pending.")]
    public string PatientReadiness { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Available Days Mask must be a positive value.")]
    public int? AvailableDaysMask { get; set; }

    [MaxLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
>>>>>>> Stashed changes
    public string? Remarks { get; set; }

    public int AvailableDaysMask { get; set; } = 31;
}