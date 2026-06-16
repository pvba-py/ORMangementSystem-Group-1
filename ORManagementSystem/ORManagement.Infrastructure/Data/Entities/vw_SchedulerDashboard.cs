using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class vw_SchedulerDashboard
{
    public int HospitalId { get; set; }

    public string HospitalName { get; set; } = null!;

    public int? ActiveRooms { get; set; }

    public int? PendingRequests { get; set; }

    public int? Waitlisted { get; set; }

    public int? StarvedRequests { get; set; }

    public int? AvailableSlots { get; set; }

    public int? PendingRecs { get; set; }
}
