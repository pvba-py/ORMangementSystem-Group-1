namespace ORManagement.Application.DTOs.Blocks;

public class BlockAllocationDto
{
    public int BlockId { get; set; }

    public int HospitalId { get; set; }

    public int? TemplateId { get; set; }

    public int? SurgeonId { get; set; }
    public string? SurgeonName { get; set; }

    public int ORRoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;

    public DateTime BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string BlockType { get; set; } = string.Empty;
    public string BlockStatus { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public int AllocatedMinutes { get; set; }
}
