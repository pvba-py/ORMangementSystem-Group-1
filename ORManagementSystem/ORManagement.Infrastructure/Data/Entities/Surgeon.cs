using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class Surgeon
{
    public int SurgeonId { get; set; }

    public int UserId { get; set; }

    public int HospitalId { get; set; }

    public string Specialty { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual ICollection<BlockAllocation> BlockAllocations { get; set; } = new List<BlockAllocation>();

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual ICollection<ORRequest> ORRequests { get; set; } = new List<ORRequest>();

    public virtual ICollection<RecurringBlockTemplate> RecurringBlockTemplates { get; set; } = new List<RecurringBlockTemplate>();

    public virtual ICollection<SurgicalCase> SurgicalCases { get; set; } = new List<SurgicalCase>();

    public virtual User User { get; set; } = null!;
}
