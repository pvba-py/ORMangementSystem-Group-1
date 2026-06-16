using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class EmergencyBuffer
{
    public int BufferId { get; set; }

    public string Quarter { get; set; } = null!;

    public int BufferMinutes { get; set; }

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
