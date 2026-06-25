using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Waitlist;

public class UpdateReleasedSlotStatusDto
{
    [Required(ErrorMessage = "Slot state is required.")]
    [MaxLength(30, ErrorMessage = "Slot state cannot exceed 30 characters.")]
    [RegularExpression("^(Available|Reserved|Booked|Cancelled|Expired)$",
        ErrorMessage = "Slot state must be one of: Available, Reserved, Booked, Cancelled, Expired.")]
    public string SlotState { get; set; } = string.Empty;
}
