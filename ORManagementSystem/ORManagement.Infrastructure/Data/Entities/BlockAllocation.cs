using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class BlockAllocation
{
    public int BlockId { get; set; }

    public int HospitalId { get; set; }

    public int? SurgeonId { get; set; }

    public int ORRoomId { get; set; }

    public int? TemplateId { get; set; }

    public DateOnly BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string BlockType { get; set; } = null!;

    public string BlockStatus { get; set; } = null!;

    public string? Remarks { get; set; }

    public int? ModifiedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual User? ModifiedByUser { get; set; }

    public virtual OperatingRoom ORRoom { get; set; } = null!;

    public virtual ICollection<ReleasedSlot> ReleasedSlots { get; set; } = new List<ReleasedSlot>();

    public virtual Surgeon? Surgeon { get; set; }

    public virtual ICollection<SurgicalCase> SurgicalCases { get; set; } = new List<SurgicalCase>();

    public virtual RecurringBlockTemplate? Template { get; set; }

    public virtual ICollection<UtilizationRecord> UtilizationRecords { get; set; } = new List<UtilizationRecord>();
}
