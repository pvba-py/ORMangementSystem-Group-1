using System.ComponentModel.DataAnnotations;

public class CreateBlockExceptionDto
{
    [Required(ErrorMessage = "Skip Date is required.")]
    public DateTime SkipDate { get; set; }

    [StringLength(200, ErrorMessage = "Reason cannot exceed 200 characters.")]
    public string? Reason { get; set; }
}