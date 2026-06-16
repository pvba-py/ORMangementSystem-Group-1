using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class vw_ORCalendar
{
    public int HospitalId { get; set; }

    public string RoomName { get; set; } = null!;

    public DateOnly BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string BlockStatus { get; set; } = null!;

    public string SurgeonName { get; set; } = null!;

    public int? SurgeryId { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    public string? CaseStatus { get; set; }
}
