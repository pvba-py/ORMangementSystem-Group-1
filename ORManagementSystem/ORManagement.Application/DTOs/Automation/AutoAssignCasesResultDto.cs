namespace ORManagement.Application.DTOs.Automation;

public class AutoAssignCasesResultDto
{
    public int CycleId { get; set; }

    public int CasesScheduled { get; set; }

    public int RequestsSkipped { get; set; }

    public List<AutoScheduledCaseDto> ScheduledCases { get; set; } = new();

    public List<AutoSkippedRequestDto> SkippedRequests { get; set; } = new();
}

public class AutoScheduledCaseDto
{
    public int RequestId { get; set; }

    public int SurgeryId { get; set; }

    public int BlockId { get; set; }

    public DateTime ScheduledStart { get; set; }

    public DateTime ScheduledEnd { get; set; }
}

public class AutoSkippedRequestDto
{
    public int RequestId { get; set; }

    public string Reason { get; set; } = string.Empty;
}