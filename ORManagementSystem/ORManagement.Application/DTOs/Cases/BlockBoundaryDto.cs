namespace ORManagement.Application.DTOs.Cases;

public class BlockBoundaryDto
{
    public int BlockId { get; set; }
    public int HospitalId { get; set; }
    public int SurgeonId { get; set; }
    public int ORRoomId { get; set; }

    public DateTime BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public string BlockStatus { get; set; } = string.Empty;
}