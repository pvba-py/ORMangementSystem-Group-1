using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class SurgicalCase
{
    public int SurgeryId { get; set; }

    public int HospitalId { get; set; }

    public int RequestId { get; set; }

    public int BlockId { get; set; }

    public int SurgeonId { get; set; }

    public int ORRoomId { get; set; }

    public DateTime ScheduledStart { get; set; }

    public DateTime ScheduledEnd { get; set; }

    public DateTime? ActualStart { get; set; }

    public DateTime? ActualEnd { get; set; }

    public string CaseStatus { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public int? ModifiedByUserId { get; set; }

    public virtual BlockAllocation Block { get; set; } = null!;

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual User? ModifiedByUser { get; set; }

    public virtual OperatingRoom ORRoom { get; set; } = null!;

    public virtual ORRequest Request { get; set; } = null!;

    public virtual Surgeon Surgeon { get; set; } = null!;
}
