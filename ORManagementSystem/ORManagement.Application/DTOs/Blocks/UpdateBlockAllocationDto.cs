using System.ComponentModel.DataAnnotations;

public class UpdateBlockAllocationDto
{
    [Range(1, int.MaxValue)]
    public int? SurgeonId { get; set; }

    [Required(ErrorMessage = "OR Room Id is required.")]
    [Range(1, int.MaxValue)]
    public int ORRoomId { get; set; }

    [Required(ErrorMessage = "Block Date is required.")]
    public DateTime BlockDate { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Block status is required.")]
    [RegularExpression("^(Allocated|Partially Booked|Fully Booked|Released|Cancelled)$",
        ErrorMessage = "Block status must be valid.")]
    public string BlockStatus { get; set; } = string.Empty;

    [Required(ErrorMessage = "Block type is required.")]
    [RegularExpression("^(Open|Reserved|Emergency)$")]
    public string BlockType { get; set; } = "Open";

    [StringLength(300)]
    public string? Remarks { get; set; }
}
