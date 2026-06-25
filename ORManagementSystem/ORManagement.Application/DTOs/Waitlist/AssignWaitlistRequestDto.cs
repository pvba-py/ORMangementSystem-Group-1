using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Waitlist;

public class AssignWaitlistRequestDto
{
    [Required(ErrorMessage = "Slot Id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Slot Id must be greater than 0.")]
    public int SlotId { get; set; }

    [Range(0, 100, ErrorMessage = "Match score must be between 0 and 100.")]
    public decimal? MatchScore { get; set; }
}
