using Microsoft.Extensions.Logging;
using ORManagement.Application.DTOs.Audit;
using ORManagement.Application.DTOs.Shared;
using ORManagement.Application.DTOs.Utilization;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Application.Interfaces.Services;

namespace ORManagement.Application.Services;

public class UtilizationService : IUtilizationService
{
    private readonly IUtilizationRepository _utilizationRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<UtilizationService> _logger;

    public UtilizationService(
        IUtilizationRepository utilizationRepository,
        IAuditRepository auditRepository,
        ILogger<UtilizationService> logger)
    {
        _utilizationRepository = utilizationRepository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task<ServiceResultDto<List<UtilizationRecordDto>>> GetUtilizationRecordsAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate,
        int? surgeonId,
        int? roomId,
        string? status)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<List<UtilizationRecordDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var records = await _utilizationRepository.GetUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            surgeonId,
            roomId,
            status);

        return ServiceResultDto<List<UtilizationRecordDto>>.Ok(records);
    }
    public async Task<ServiceResultDto<List<ORRoomUtilizationRecordDto>>> GetORRoomUtilizationRecordsAsync(
    int hospitalId,
    DateTime? fromDate,
    DateTime? toDate,
    int? roomId,
    string? status)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<List<ORRoomUtilizationRecordDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var records = await _utilizationRepository.GetORRoomUtilizationRecordsAsync(
            hospitalId,
            fromDate,
            toDate,
            roomId,
            status);

        return ServiceResultDto<List<ORRoomUtilizationRecordDto>>.Ok(records);
    }

    public async Task<ServiceResultDto<ORRoomUtilizationSummaryDto>> GetORRoomUtilizationSummaryAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<ORRoomUtilizationSummaryDto>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var summary = await _utilizationRepository.GetORRoomUtilizationSummaryAsync(
            hospitalId,
            fromDate,
            toDate);

        return ServiceResultDto<ORRoomUtilizationSummaryDto>.Ok(summary);
    }

    public async Task<ServiceResultDto<List<UnderutilizedORRoomDto>>> GetUnderutilizedORRoomsAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<List<UnderutilizedORRoomDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var rooms = await _utilizationRepository.GetUnderutilizedORRoomsAsync(
            hospitalId,
            fromDate,
            toDate);

        return ServiceResultDto<List<UnderutilizedORRoomDto>>.Ok(rooms);
    }

    public async Task<ServiceResultDto<int>> CalculateORRoomWeeklyUtilizationAsync(
        int hospitalId,
        int userId,
        string roleName,
        CalculateORRoomUtilizationRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (!request.WeekStartDate.HasValue)
        {
            return ServiceResultDto<int>.Fail(
                "WEEK_START_DATE_REQUIRED",
                "WeekStartDate is required.");
        }

        var weekStartDate = request.WeekStartDate.Value.Date;

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            return ServiceResultDto<int>.Fail(
                "INVALID_WEEK_START_DATE",
                "WeekStartDate must be a Monday.");
        }

        if (request.ORRoomId.HasValue)
        {
            var roomExists = await _utilizationRepository.ORRoomExistsAsync(
                hospitalId,
                request.ORRoomId.Value);

            if (!roomExists)
            {
                return ServiceResultDto<int>.Fail(
                    "OR_ROOM_NOT_FOUND",
                    "OR room was not found.");
            }
        }

        var calculatedCount = await _utilizationRepository.CalculateORRoomWeeklyUtilizationAsync(
            hospitalId,
            request.ORRoomId,
            weekStartDate);

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "ORRoomWeeklyUtilizationCalculated",
            Entity = "ORRoomUtilizationRecords",
            EntityId = request.ORRoomId,
            NewValue = calculatedCount.ToString(),
            Remarks = request.ORRoomId.HasValue
                ? $"OR room weekly utilization calculated for room {request.ORRoomId.Value}, week {weekStartDate:yyyy-MM-dd} to {weekStartDate.AddDays(4):yyyy-MM-dd}."
                : $"OR room weekly utilization calculated for all rooms, week {weekStartDate:yyyy-MM-dd} to {weekStartDate.AddDays(4):yyyy-MM-dd}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "OR room weekly utilization calculated. Count: {CalculatedCount}, UserId: {UserId}, WeekStartDate: {WeekStartDate}",
            calculatedCount,
            userId,
            weekStartDate);

        return ServiceResultDto<int>.Ok(
            calculatedCount,
            "OR room weekly utilization calculation completed successfully.");
    }

    public async Task<ServiceResultDto<ORRoomWeeklyReportDto>> GenerateORRoomWeeklyReportAsync(
        int hospitalId,
        int userId,
        string roleName,
        GenerateORRoomWeeklyReportRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        if (!request.WeekStartDate.HasValue)
        {
            return ServiceResultDto<ORRoomWeeklyReportDto>.Fail(
                "WEEK_START_DATE_REQUIRED",
                "WeekStartDate is required.");
        }

        var weekStartDate = request.WeekStartDate.Value.Date;

        if (weekStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            return ServiceResultDto<ORRoomWeeklyReportDto>.Fail(
                "INVALID_WEEK_START_DATE",
                "WeekStartDate must be a Monday.");
        }

        var weekEndDate = weekStartDate.AddDays(4);

        var calculatedCount = await _utilizationRepository.CalculateORRoomWeeklyUtilizationAsync(
            hospitalId,
            null,
            weekStartDate);

        var records = await _utilizationRepository.GetORRoomUtilizationRecordsAsync(
            hospitalId,
            weekStartDate,
            weekStartDate,
            null,
            null);

        var summary = await _utilizationRepository.GetORRoomUtilizationSummaryAsync(
            hospitalId,
            weekStartDate,
            weekStartDate);

        var underutilizedRooms = await _utilizationRepository.GetUnderutilizedORRoomsAsync(
            hospitalId,
            weekStartDate,
            weekStartDate);

        var report = new ORRoomWeeklyReportDto
        {
            WeekStartDate = weekStartDate,
            WeekEndDate = weekEndDate,
            GeneratedAt = DateTime.UtcNow,
            CalculatedRooms = calculatedCount,
            Summary = summary,
            Rooms = records,
            UnderutilizedRooms = underutilizedRooms
        };

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "ORRoomWeeklyUtilizationReportGenerated",
            Entity = "ORRoomUtilizationRecords",
            EntityId = null,
            NewValue = calculatedCount.ToString(),
            Remarks = $"OR room weekly utilization report generated for week {weekStartDate:yyyy-MM-dd} to {weekEndDate:yyyy-MM-dd}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "OR room weekly utilization report generated. Count: {CalculatedCount}, UserId: {UserId}, WeekStartDate: {WeekStartDate}",
            calculatedCount,
            userId,
            weekStartDate);

        return ServiceResultDto<ORRoomWeeklyReportDto>.Ok(
            report,
            "OR room weekly utilization report generated successfully.");
    }

    public async Task<ServiceResultDto<UtilizationSummaryDto>> GetSummaryAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<UtilizationSummaryDto>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var summary = await _utilizationRepository.GetSummaryAsync(
            hospitalId,
            fromDate,
            toDate);

        return ServiceResultDto<UtilizationSummaryDto>.Ok(summary);
    }

    public async Task<ServiceResultDto<List<UnderutilizedBlockDto>>> GetUnderutilizedBlocksAsync(
        int hospitalId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return ServiceResultDto<List<UnderutilizedBlockDto>>.Fail(
                "INVALID_DATE_RANGE",
                "From date cannot be after To date.");
        }

        var blocks = await _utilizationRepository.GetUnderutilizedBlocksAsync(
            hospitalId,
            fromDate,
            toDate);

        return ServiceResultDto<List<UnderutilizedBlockDto>>.Ok(blocks);
    }

    public async Task<ServiceResultDto<int>> CalculateUtilizationAsync(
        int hospitalId,
        int userId,
        string roleName,
        CalculateUtilizationRequestDto request,
        string? ipAddress,
        string? userAgent)
    {
        var calculatedCount = 0;

        if (request.BlockId.HasValue)
        {
            var exists = await _utilizationRepository.BlockExistsAsync(
                hospitalId,
                request.BlockId.Value);

            if (!exists)
            {
                return ServiceResultDto<int>.Fail(
                    "BLOCK_NOT_FOUND",
                    "Block was not found.");
            }

            await _utilizationRepository.CalculateBlockUtilizationAsync(request.BlockId.Value);
            calculatedCount = 1;
        }
        else
        {
            if (!request.FromDate.HasValue || !request.ToDate.HasValue)
            {
                return ServiceResultDto<int>.Fail(
                    "DATE_RANGE_REQUIRED",
                    "FromDate and ToDate are required when BlockId is not provided.");
            }

            if (request.FromDate.Value.Date > request.ToDate.Value.Date)
            {
                return ServiceResultDto<int>.Fail(
                    "INVALID_DATE_RANGE",
                    "From date cannot be after To date.");
            }

            var blockIds = await _utilizationRepository.GetBlockIdsForDateRangeAsync(
                hospitalId,
                request.FromDate.Value,
                request.ToDate.Value);

            foreach (var blockId in blockIds)
            {
                await _utilizationRepository.CalculateBlockUtilizationAsync(blockId);
                calculatedCount++;
            }
        }

        await _auditRepository.AddAuditLogAsync(new CreateAuditLogDto
        {
            HospitalId = hospitalId,
            UserId = userId,
            RoleName = roleName,
            Action = "UtilizationCalculated",
            Entity = "UtilizationRecords",
            EntityId = request.BlockId,
            NewValue = calculatedCount.ToString(),
            Remarks = request.BlockId.HasValue
                ? $"Utilization calculated for block {request.BlockId.Value}."
                : $"Utilization calculated for range {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        });

        _logger.LogInformation(
            "Utilization calculated. Count: {CalculatedCount}, UserId: {UserId}",
            calculatedCount,
            userId);

        return ServiceResultDto<int>.Ok(
            calculatedCount,
            "Utilization calculation completed successfully.");
    }
}