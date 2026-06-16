using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class SchedulingCycle
{
    public int CycleId { get; set; }

    public int HospitalId { get; set; }

    public DateOnly WeekStartDate { get; set; }

    public DateOnly WeekEndDate { get; set; }

    public DateTime CutoffAt { get; set; }

    public string CycleStatus { get; set; } = null!;

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual ICollection<ORRequest> ORRequestCycles { get; set; } = new List<ORRequest>();

    public virtual ICollection<ORRequest> ORRequestOriginalCycles { get; set; } = new List<ORRequest>();
}
