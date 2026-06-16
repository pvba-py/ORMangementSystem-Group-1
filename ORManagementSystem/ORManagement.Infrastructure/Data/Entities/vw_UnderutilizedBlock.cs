using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class vw_UnderutilizedBlock
{
    public int HospitalId { get; set; }

    public int BlockId { get; set; }

    public int SurgeonId { get; set; }

    public DateOnly BlockDate { get; set; }

    public decimal? UtilizationPct { get; set; }

    public string UtilStatus { get; set; } = null!;
}
