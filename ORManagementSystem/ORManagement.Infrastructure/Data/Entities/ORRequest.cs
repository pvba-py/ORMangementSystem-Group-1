using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class ORRequest
{
    public int RequestId { get; set; }

    public int HospitalId { get; set; }

    public int SurgeonId { get; set; }

    public int PatientId { get; set; }

    public int? CycleId { get; set; }

    public int? OriginalCycleId { get; set; }

    public int CyclesWaited { get; set; }

    public DateOnly PreferredDate { get; set; }

    public string PreferredQuarter { get; set; } = null!;

    public int EstimatedDurationMin { get; set; }

    public string SurgeryType { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string PatientReadiness { get; set; } = null!;

    public string? Remarks { get; set; }

    public string RequestStatus { get; set; } = null!;

    public string? SchedulerRemarks { get; set; }

    public int? ModifiedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SchedulingCycle? Cycle { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual User? ModifiedByUser { get; set; }

    public virtual SchedulingCycle? OriginalCycle { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Surgeon Surgeon { get; set; } = null!;

    public virtual SurgicalCase? SurgicalCase { get; set; }

    public virtual WaitlistRequest? WaitlistRequest { get; set; }
}
