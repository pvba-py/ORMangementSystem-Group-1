using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class SystemSetting
{
    public int SettingId { get; set; }

    public int? HospitalId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? DeactivatedAt { get; set; }

    public virtual Hospital? Hospital { get; set; }
}
