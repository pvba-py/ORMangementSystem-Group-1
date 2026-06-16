using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class PhiAccessLog
{
    public int AccessId { get; set; }

    public int HospitalId { get; set; }

    public int UserId { get; set; }

    public int PatientId { get; set; }

    public string AccessType { get; set; } = null!;

    public string? Context { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime AccessedAt { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
