using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class vw_CycleQueue
{
    public int HospitalId { get; set; }

    public int? CycleId { get; set; }

    public int RequestId { get; set; }

    public string SurgeonName { get; set; } = null!;

    public string SurgeryType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string PatientReadiness { get; set; } = null!;

    public int EstimatedDurationMin { get; set; }

    public string PreferredQuarter { get; set; } = null!;

    public int CyclesWaited { get; set; }

    public DateTime CreatedAt { get; set; }

    public int IsStarved { get; set; }
}
