using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class BlockException
{
    public int ExceptionId { get; set; }

    public int TemplateId { get; set; }

    public DateOnly SkipDate { get; set; }

    public string? Reason { get; set; }

    public virtual RecurringBlockTemplate Template { get; set; } = null!;
}
