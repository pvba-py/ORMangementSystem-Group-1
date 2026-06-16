using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class Hospital
{
    public int HospitalId { get; set; }

    public string HospitalCode { get; set; } = null!;

    public string HospitalName { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Timezone { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<BlockAllocation> BlockAllocations { get; set; } = new List<BlockAllocation>();

    public virtual ICollection<ForecastRecommendation> ForecastRecommendations { get; set; } = new List<ForecastRecommendation>();

    public virtual ICollection<ORRequest> ORRequests { get; set; } = new List<ORRequest>();

    public virtual ICollection<OperatingRoom> OperatingRooms { get; set; } = new List<OperatingRoom>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual ICollection<PhiAccessLog> PhiAccessLogs { get; set; } = new List<PhiAccessLog>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<ReleasedSlot> ReleasedSlots { get; set; } = new List<ReleasedSlot>();

    public virtual ICollection<SchedulingCycle> SchedulingCycles { get; set; } = new List<SchedulingCycle>();

    public virtual ICollection<Surgeon> Surgeons { get; set; } = new List<Surgeon>();

    public virtual ICollection<SurgicalCase> SurgicalCases { get; set; } = new List<SurgicalCase>();

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
