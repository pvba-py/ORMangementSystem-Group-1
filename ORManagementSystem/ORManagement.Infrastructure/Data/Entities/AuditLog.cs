using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class AuditLog
{
    public int AuditId { get; set; }

    public int? HospitalId { get; set; }

    public int? UserId { get; set; }

    public string RoleName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string Entity { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Remarks { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hospital? Hospital { get; set; }

    public virtual User? User { get; set; }
}
