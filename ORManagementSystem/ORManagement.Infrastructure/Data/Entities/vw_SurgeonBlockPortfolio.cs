using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class vw_SurgeonBlockPortfolio
{
    public int HospitalId { get; set; }

    public int BlockId { get; set; }

    public int SurgeonId { get; set; }

    public string SurgeonName { get; set; } = null!;

    public string RoomName { get; set; } = null!;

    public DateOnly BlockDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string BlockStatus { get; set; } = null!;

    public int? AllocatedMin { get; set; }

    public int UsedMin { get; set; }
}
