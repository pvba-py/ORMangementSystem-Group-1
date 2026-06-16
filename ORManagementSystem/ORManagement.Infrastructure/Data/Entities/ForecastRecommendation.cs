using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class ForecastRecommendation
{
    public int RecId { get; set; }

    public int HospitalId { get; set; }

    public string RuleId { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? EvidenceJson { get; set; }

    public string RecStatus { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Hospital Hospital { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }
}
