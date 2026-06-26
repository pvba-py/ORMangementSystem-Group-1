using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ORManagement.Application.DTOs.Blocks;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Api.Controllers;

[Route("api")]
[Authorize]
public class BlocksController : ApiControllerBase
{
    private readonly IBlockService _blockService;
    private readonly ILogger<BlocksController> _logger;

    public BlocksController(
        IBlockService blockService,
        ILogger<BlocksController> logger)
    {
        _blockService = blockService;
        _logger = logger;
    }

    [HttpGet("block-templates")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetTemplates()
    {
        var result = await _blockService.GetTemplatesAsync(GetCurrentHospitalIdOrDefault());

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpPost("block-templates")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateBlockTemplateDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block template request.",
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

        var result = await _blockService.CreateTemplateAsync(
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

        return Ok(new
        {
            success = true,
            message = result.Message,
            templateId = result.Data
        });
    }

    [HttpPut("block-templates/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateBlockTemplateDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block template update request.",
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

        var result = await _blockService.UpdateTemplateAsync(
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
    [HttpDelete("block-templates/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> DeleteTemplate(int id)
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

        var result = await _blockService.DeleteTemplateAsync(
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
    [HttpPut("block-templates/{id:int}/deactivate")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> DeactivateTemplate(int id)
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

        var result = await _blockService.DeactivateTemplateAsync(
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

    [HttpPost("block-templates/{id:int}/exceptions")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> AddException(int id, [FromBody] CreateBlockExceptionDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block exception request.",
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

        var result = await _blockService.AddExceptionAsync(
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
            message = result.Message,
            exceptionId = result.Data
        });
    }

    [HttpDelete("block-templates/{templateId:int}/exceptions/{exceptionId:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> DeleteException(int templateId, int exceptionId)
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

        var result = await _blockService.DeleteExceptionAsync(
            GetCurrentHospitalIdOrDefault(),
            templateId,
            exceptionId,
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

    [HttpPost("blocks/generate")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GenerateBlocks([FromBody] GenerateBlocksRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid generate blocks request.",
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

        var result = await _blockService.GenerateBlocksAsync(
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

        return Ok(new
        {
            success = true,
            message = result.Message,
            generatedCount = result.Data
        });
    }

    [HttpPost("blocks")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CreateBlockAllocation([FromBody] CreateBlockAllocationDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block request.",
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

        var result = await _blockService.CreateBlockAllocationAsync(
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

        return Ok(new
        {
            success = true,
            message = result.Message,
            blockId = result.Data
        });
    }
    [HttpGet("blocks")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> GetBlocks(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? surgeonId,
        [FromQuery] int? roomId)
    {
        var result = await _blockService.GetBlocksAsync(
            GetCurrentHospitalIdOrDefault(),
            fromDate,
            toDate,
            surgeonId,
            roomId);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpGet("blocks/my")]
    [Authorize(Roles = "Surgeon")]
    public async Task<IActionResult> GetMyBlocks()
    {
        var surgeonId = GetCurrentSurgeonId();

        if (surgeonId is null)
        {
            return Unauthorized(new
            {
                success = false,
                errorCode = "SURGEON_CLAIM_MISSING",
                message = "Surgeon profile was not found in token."
            });
        }

        var result = await _blockService.GetMyBlocksAsync(
            GetCurrentHospitalIdOrDefault(),
            surgeonId.Value);

        if (!result.Success)
        {
            return MapError(result);
        }

        return Ok(result.Data);
    }

    [HttpPut("blocks/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> UpdateBlock(int id, [FromBody] UpdateBlockAllocationDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block update request.",
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

        var result = await _blockService.UpdateBlockAsync(
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

    [HttpDelete("blocks/{id:int}")]
    [Authorize(Roles = "ORScheduler")]
    public async Task<IActionResult> CancelBlock(int id)
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

        var result = await _blockService.CancelBlockAsync(
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

    [HttpPost("blocks/{id:int}/release")]
    [Authorize(Roles = "Surgeon,ORScheduler")]
    public async Task<IActionResult> ReleaseBlock(int id, [FromBody] ReleaseBlockRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                errorCode = "VALIDATION_ERROR",
                message = "Invalid block release request.",
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

        var result = await _blockService.ReleaseBlockAsync(
            GetCurrentHospitalIdOrDefault(),
            id,
            GetCurrentSurgeonId(),
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
            message = result.Message,
            slotId = result.Data
        });
    }
}