using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ORManagement.Application.DTOs.Cases;

public class UpdateCaseStatusDto
{
    [Required(ErrorMessage = "Status is required.")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("actualStart")]
    public DateTime? ActualStart { get; set; }

    [JsonPropertyName("actualEnd")]
    public DateTime? ActualEnd { get; set; }

    [JsonPropertyName("cancellationReason")]
    public string? CancellationReason { get; set; }
}