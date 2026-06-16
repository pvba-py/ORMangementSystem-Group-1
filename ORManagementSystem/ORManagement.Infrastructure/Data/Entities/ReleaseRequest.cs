using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class ReleaseRequest
{
    public int ReleaseId { get; set; }

    public int BlockId { get; set; }

    public int? RequestedByUser { get; set; }

    public string ReleaseSource { get; set; } = null!;

    public int MinutesReleased { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BlockAllocation Block { get; set; } = null!;

    public virtual User? RequestedByUserNavigation { get; set; }
}
