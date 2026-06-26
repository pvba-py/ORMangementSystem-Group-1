namespace ORManagement.Application.DTOs.Automation;

public class AutoBuildBlocksResultDto
{
    public int CycleId { get; set; }

    public int TemplatesCreated { get; set; }

    public int BlocksGenerated { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Messages { get; set; } = new();
}