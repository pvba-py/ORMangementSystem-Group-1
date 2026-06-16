using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class User
{
    public int UserId { get; set; }

    public int? HospitalId { get; set; }

    public int RoleId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<BlockAllocation> BlockAllocations { get; set; } = new List<BlockAllocation>();

    public virtual ICollection<ForecastRecommendation> ForecastRecommendations { get; set; } = new List<ForecastRecommendation>();

    public virtual Hospital? Hospital { get; set; }

    public virtual ICollection<ORRequest> ORRequests { get; set; } = new List<ORRequest>();

    public virtual ICollection<PhiAccessLog> PhiAccessLogs { get; set; } = new List<PhiAccessLog>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<ReleasedSlot> ReleasedSlots { get; set; } = new List<ReleasedSlot>();

    public virtual Role Role { get; set; } = null!;

    public virtual Surgeon? Surgeon { get; set; }

    public virtual ICollection<SurgicalCase> SurgicalCases { get; set; } = new List<SurgicalCase>();
}
