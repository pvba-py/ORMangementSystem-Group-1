namespace ORManagement.Application.DTOs.Automation;

public class AutoBlockDemandRequestDto
{
    public int RequestId { get; set; }

    public int SurgeonId { get; set; }

    public int EstimatedDurationMin { get; set; }

    public string Priority { get; set; } = string.Empty;

    public string PatientReadiness { get; set; } = string.Empty;

    public string RequestStatus { get; set; } = string.Empty;
}