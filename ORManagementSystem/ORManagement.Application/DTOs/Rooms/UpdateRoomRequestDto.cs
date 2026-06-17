using System.ComponentModel.DataAnnotations;

namespace ORManagement.Application.DTOs.Rooms;

public class UpdateRoomRequestDto
{
    [Required]
    [StringLength(50)]
    public string RoomName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RoomType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}