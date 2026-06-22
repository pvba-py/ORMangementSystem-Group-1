namespace ORManagement.Application.DTOs.Cases;

public class BlockBoundaryDto
{
    public int BlockId { get; set; }

    public int? SurgeonId { get; set; }

    public string BlockType { get; set; } = string.Empty;

    public DateTime BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
}