using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class OperatingRoom
{
    public int ORRoomId { get; set; }

    public int HospitalId { get; set; }

    public string RoomName { get; set; } = null!;

    public string RoomType { get; set; } = null!;

    public string Location { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual ICollection<BlockAllocation> BlockAllocations { get; set; } = new List<BlockAllocation>();

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual ICollection<RecurringBlockTemplate> RecurringBlockTemplates { get; set; } = new List<RecurringBlockTemplate>();

    public virtual ICollection<ReleasedSlot> ReleasedSlots { get; set; } = new List<ReleasedSlot>();

    public virtual ICollection<SurgicalCase> SurgicalCases { get; set; } = new List<SurgicalCase>();
}
