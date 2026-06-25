using System.ComponentModel.DataAnnotations;

public class GenerateBlocksRequestDto
{
    [Required(ErrorMessage = "From Date is required.")]
    public DateTime FromDate { get; set; }

    [Required(ErrorMessage = "To Date is required.")]
    public DateTime ToDate { get; set; }
}