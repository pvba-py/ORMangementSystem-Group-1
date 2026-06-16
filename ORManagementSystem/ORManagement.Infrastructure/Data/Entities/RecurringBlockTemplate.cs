using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class RecurringBlockTemplate
{
    public int TemplateId { get; set; }

    public int SurgeonId { get; set; }

    public int ORRoomId { get; set; }

    public string Specialty { get; set; } = null!;

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual ICollection<BlockAllocation> BlockAllocations { get; set; } = new List<BlockAllocation>();

    public virtual ICollection<BlockException> BlockExceptions { get; set; } = new List<BlockException>();

    public virtual OperatingRoom ORRoom { get; set; } = null!;

    public virtual Surgeon Surgeon { get; set; } = null!;
}
