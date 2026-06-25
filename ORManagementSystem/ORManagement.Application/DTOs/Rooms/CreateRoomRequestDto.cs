using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Rooms;

public class CreateRoomRequestDto
{
    [Required(ErrorMessage = "Room name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Room name must be between 2 and 50 characters.")]
    public string RoomName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room type is required.")]
    [StringLength(50, ErrorMessage = "Room type cannot exceed 50 characters.")]
   
    public string RoomType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Location must be between 3 and 100 characters.")]
    public string Location { get; set; } = string.Empty;
}