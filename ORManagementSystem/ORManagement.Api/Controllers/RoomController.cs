using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Rooms;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api")]
[Authorize]
public class RoomsController : ApiControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IRoomService roomService,
        ILogger<RoomsController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var hospitalId = GetCurrentHospitalIdOrDefault();

        var result = await _roomService.GetRoomsAsync(hospitalId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
    [HttpGet("rooms/paged")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetRoomsPaged(
    [FromQuery] bool? isActive,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10)
    {
        var result = await _roomService.GetRoomsPagedAsync(
            GetCurrentHospitalIdOrDefault(),
            isActive,
            pageNumber,
            pageSize);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("rooms")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid room request.",
                errors = ModelState
            });
        }

        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var result = await _roomService.CreateRoomAsync(
            GetCurrentHospitalIdOrDefault(),
            userId.Value,
            GetCurrentRoleName(),
            request,
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return CreatedAtAction(nameof(GetRooms), new
        {
            id = result.Data
        }, new
        {
            success = true,
            message = result.Message,
            roomId = result.Data
        });
    }

    [HttpPut("rooms/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] UpdateRoomRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid room update request.",
                errors = ModelState
            });
        }

        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var result = await _roomService.UpdateRoomAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            request,
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    [HttpDelete("rooms/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var result = await _roomService.DeleteRoomAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            userId.Value,
            GetCurrentRoleName(),
            GetIpAddress(),
            GetUserAgent());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(new
        {
            success = true,
            message = result.Message
        });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] int? roomId)
    {
        var result = await _roomService.GetCalendarAsync(
            GetCurrentHospitalIdOrDefault(),
            fromDate,
            toDate,
            roomId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
    [HttpGet("calendar/my")]
    [Authorize(Roles = "Surgeon")]
    public async Task<IActionResult> GetMyCalendar(
    [FromQuery] DateTime fromDate,
    [FromQuery] DateTime toDate)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "INVALID_TOKEN",
                message = "Invalid token."
            });
        }

        var result = await _roomService.GetMyCalendarAsync(
            GetCurrentHospitalIdOrDefault(),
            userId.Value,
            fromDate,
            toDate);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }
}