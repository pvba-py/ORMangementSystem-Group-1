using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Blocks;

public class CreateBlockAllocationDto
{
    public int? SurgeonId { get; set; }

    [Required]
    public int ORRoomId { get; set; }

    [Required]
    public DateTime BlockDate { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }

    [Required]
    public string BlockType { get; set; } = "Open";

    public string? Remarks { get; set; }
}