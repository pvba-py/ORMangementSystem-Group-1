using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class DatabasePhiAuditLog
{
    public int DbAuditId { get; set; }

    public DateTime EventTime { get; set; }

    public string? ActionId { get; set; }

    public string? ActionName { get; set; }

    public string? ServerPrincipalName { get; set; }

    public string? DatabasePrincipalName { get; set; }

    public string? DatabaseName { get; set; }

    public string? SchemaName { get; set; }

    public string? ObjectName { get; set; }

    public string? StatementText { get; set; }

    public string? ClientIp { get; set; }

    public string? ApplicationName { get; set; }

    public DateTime ImportedAt { get; set; }
}
