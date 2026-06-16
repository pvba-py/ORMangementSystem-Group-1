using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class ReleasedSlot
{
    public int SlotId { get; set; }

    public int HospitalId { get; set; }

    public int BlockId { get; set; }

    public int ORRoomId { get; set; }

    public DateOnly SlotDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Source { get; set; } = null!;

    public int? ReleasedByUserId { get; set; }

    public string SlotState { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual BlockAllocation Block { get; set; } = null!;

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual OperatingRoom ORRoom { get; set; } = null!;

    public virtual User? ReleasedByUser { get; set; }

    public virtual ICollection<WaitlistRequest> WaitlistRequests { get; set; } = new List<WaitlistRequest>();
}
