using System;
using System.Collections.Generic;

namespace ORManagement.Infrastructure.Data.Entities;

public partial class WaitlistRequest
{
    public int WaitlistId { get; set; }

    public int RequestId { get; set; }

    public DateTime WaitingSince { get; set; }

    public decimal? MatchScore { get; set; }

    public int? MatchedSlotId { get; set; }

    public virtual ReleasedSlot? MatchedSlot { get; set; }

    public virtual ORRequest Request { get; set; } = null!;
}
