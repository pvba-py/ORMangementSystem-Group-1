using System.ComponentModel.DataAnnotations;

public class UpdateOrRequestDto
{
    //[Required(ErrorMessage = "Preferred Date is required.")]
    //[DataType(DataType.Date)]
    public DateTime? PreferredDate { get; set; }

    //[Required(ErrorMessage = "Preferred Quarter is required.")]
    //[MaxLength(20)]
    //[RegularExpression("^(Q1|Q2|Q3|Q4)$")]
    public string? PreferredQuarter { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int EstimatedDurationMin { get; set; }

    [Required(ErrorMessage = "Surgery type is required.")]
    [StringLength(100, MinimumLength = 3)]
    public string SurgeryType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Priority is required.")]
    [MaxLength(30)]
    public string Priority { get; set; } = string.Empty;

    [Required(ErrorMessage = "Patient readiness is required.")]
    [MaxLength(30)]
    public string PatientReadiness { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Remarks { get; set; }

    [Range(1, int.MaxValue)]
    public int AvailableDaysMask { get; set; } = 31;
}