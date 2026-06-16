using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class UtilizationRecord
{
    public int UtilizationId { get; set; }

    public int BlockId { get; set; }

    public int AllocatedMinutes { get; set; }

    public int UsedMinutes { get; set; }

    public decimal? UtilizationPct { get; set; }

    public string UtilStatus { get; set; } = null!;

    public DateTime CalculatedAt { get; set; }

    public virtual BlockAllocation Block { get; set; } = null!;
}
