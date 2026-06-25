using System.ComponentModel.DataAnnotations;

public class CreateBlockAllocationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Surgeon Id must be greater than 0.")]
    public int? SurgeonId { get; set; }

    [Required(ErrorMessage = "OR Room Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "OR Room Id must be greater than 0.")]
    public int ORRoomId { get; set; }

    [Required(ErrorMessage = "Block Date is required.")]
    public DateTime BlockDate { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public TimeOnly EndTime { get; set; }

    [Required(ErrorMessage = "Block type is required.")]
    [RegularExpression("^(Open|Recurring|Emergency)$",
        ErrorMessage = "Block type must be one of: Open, Reserved, Emergency.")]
    public string BlockType { get; set; } = "Open";

    [StringLength(300, ErrorMessage = "Remarks cannot exceed 300 characters.")]
    public string? Remarks { get; set; }
}