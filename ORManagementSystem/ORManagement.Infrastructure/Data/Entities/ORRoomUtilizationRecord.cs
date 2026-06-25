namespace ORManagement.Infrastructure.Data.Entities;

public partial class ORRoomUtilizationRecord
{
    public int ORRoomUtilizationId { get; set; }

    public int HospitalId { get; set; }

    public int ORRoomId { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public int AllocatedMinutes { get; set; }

    public int UsedMinutes { get; set; }

    public decimal? UtilizationPct { get; set; }

    public string UtilStatus { get; set; } = null!;

    public DateTime CalculatedAt { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual OperatingRoom ORRoom { get; set; } = null!;
}