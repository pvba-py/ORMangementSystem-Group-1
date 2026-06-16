using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class Patient
{
    public int PatientId { get; set; }

    public int HospitalId { get; set; }

    public string MRN { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual ICollection<ORRequest> ORRequests { get; set; } = new List<ORRequest>();

    public virtual ICollection<PhiAccessLog> PhiAccessLogs { get; set; } = new List<PhiAccessLog>();
}
