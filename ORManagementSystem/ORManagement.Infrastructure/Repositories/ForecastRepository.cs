using Microsoft.EntityFrameworkCore;
using ORManagement.Application.DTOs.Forecast;
using ORManagement.Application.Interfaces.Repositories;
using ORManagement.Infrastructure.Data;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Repositories;

public class ForecastRepository : IForecastRepository
{
    private readonly ORManagementDbContext _dbContext;

    public ForecastRepository(ORManagementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ForecastRecommendationDto>> GetRecommendationsAsync(
        int hospitalId,
        string? status)
    {
        var query = _dbContext.ForecastRecommendations
            .Where(recommendation => recommendation.HospitalId == hospitalId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(recommendation => recommendation.RecStatus == status);
        }

        return await query
            .OrderByDescending(recommendation => recommendation.CreatedAt)
            .Select(recommendation => new ForecastRecommendationDto
            {
                RecId = recommendation.RecId,
                HospitalId = recommendation.HospitalId,
                RuleId = recommendation.RuleId,
                Description = recommendation.Description,
                EvidenceJson = recommendation.EvidenceJson,
                RecStatus = recommendation.RecStatus,
                ReviewedBy = recommendation.ReviewedBy,
                CreatedAt = recommendation.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ForecastRecommendationDto?> GetRecommendationByIdAsync(
        int hospitalId,
        int recId)
    {
        return await _dbContext.ForecastRecommendations
            .Where(recommendation =>
                recommendation.HospitalId == hospitalId &&
                recommendation.RecId == recId)
            .Select(recommendation => new ForecastRecommendationDto
            {
                RecId = recommendation.RecId,
                HospitalId = recommendation.HospitalId,
                RuleId = recommendation.RuleId,
                Description = recommendation.Description,
                EvidenceJson = recommendation.EvidenceJson,
                RecStatus = recommendation.RecStatus,
                ReviewedBy = recommendation.ReviewedBy,
                CreatedAt = recommendation.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<ForecastDemandSignalDto>> GetDemandSignalsAsync(int hospitalId)
    {
        var blockSignals = await
            (
                from block in _dbContext.BlockAllocations
                join surgeon in _dbContext.Surgeons on block.SurgeonId equals surgeon.SurgeonId
                where block.HospitalId == hospitalId &&
                      block.BlockStatus != "Cancelled"
                group block by surgeon.Specialty into groupData
                select new
                {
                    Specialty = groupData.Key,
                    TotalBlocks = groupData.Count()
                }
            )
            .ToListAsync();

        var utilizationSignals = await
            (
                from utilization in _dbContext.UtilizationRecords
                join block in _dbContext.BlockAllocations on utilization.BlockId equals block.BlockId
                join surgeon in _dbContext.Surgeons on block.SurgeonId equals surgeon.SurgeonId
                where block.HospitalId == hospitalId
                group utilization by surgeon.Specialty into groupData
                select new
                {
                    Specialty = groupData.Key,
                    AverageUtilizationPercent = groupData.Average(item => item.UtilizationPct ?? 0)
                }
            )
            .ToListAsync();

        var waitlistSignals = await
            (
                from waitlist in _dbContext.WaitlistRequests
                join request in _dbContext.ORRequests on waitlist.RequestId equals request.RequestId
                join surgeon in _dbContext.Surgeons on request.SurgeonId equals surgeon.SurgeonId
                where request.HospitalId == hospitalId &&
                      request.RequestStatus == "Waitlisted"
                group waitlist by surgeon.Specialty into groupData
                select new
                {
                    Specialty = groupData.Key,
                    WaitlistedRequests = groupData.Count()
                }
            )
            .ToListAsync();

        var specialties = blockSignals
            .Select(item => item.Specialty)
            .Union(utilizationSignals.Select(item => item.Specialty))
            .Union(waitlistSignals.Select(item => item.Specialty))
            .Distinct()
            .ToList();

        return specialties
            .Select(specialty => new ForecastDemandSignalDto
            {
                HospitalId = hospitalId,
                Specialty = specialty,
                TotalBlocks = blockSignals.FirstOrDefault(item => item.Specialty == specialty)?.TotalBlocks ?? 0,
                AverageUtilizationPercent = Math.Round(
                    utilizationSignals.FirstOrDefault(item => item.Specialty == specialty)?.AverageUtilizationPercent ?? 0,
                    2),
                WaitlistedRequests = waitlistSignals.FirstOrDefault(item => item.Specialty == specialty)?.WaitlistedRequests ?? 0
            })
            .ToList();
    }

    public async Task<int> CreateRecommendationAsync(CreateForecastRecommendationDto request)
    {
        var recommendation = new ForecastRecommendation
        {
            HospitalId = request.HospitalId,
            RuleId = request.RuleId,
            Description = request.Description,
            EvidenceJson = request.EvidenceJson,
            RecStatus = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ForecastRecommendations.AddAsync(recommendation);
        await _dbContext.SaveChangesAsync();

        return recommendation.RecId;
    }

    public async Task<bool> UpdateRecommendationStatusAsync(
        int hospitalId,
        int recId,
        string status,
        int reviewedByUserId)
    {
        var recommendation = await _dbContext.ForecastRecommendations
            .FirstOrDefaultAsync(recommendation =>
                recommendation.HospitalId == hospitalId &&
                recommendation.RecId == recId);

        if (recommendation is null)
        {
            return false;
        }

        recommendation.RecStatus = status;
        recommendation.ReviewedBy = reviewedByUserId;

        await _dbContext.SaveChangesAsync();

        return true;
    }
    public async Task<List<SurgeryDurationDto>> GetSurgeryDurationAveragesAsync(int hospitalId)
    {
        var result = await
            (
                from surgicalCase in _dbContext.SurgicalCases.AsNoTracking()
                join request in _dbContext.ORRequests.AsNoTracking()
                    on surgicalCase.RequestId equals request.RequestId
                where surgicalCase.HospitalId == hospitalId &&
                      surgicalCase.CaseStatus != "Cancelled"
                let durationMinutes = EF.Functions.DateDiffMinute(
                    surgicalCase.ScheduledStart,
                    surgicalCase.ScheduledEnd)
                where durationMinutes > 0
                group new
                {
                    request.SurgeryType,
                    DurationMinutes = durationMinutes
                }
                by request.SurgeryType.Trim()
                into surgeryGroup
                select new SurgeryDurationDto
                {
                    SurgeryType = surgeryGroup.Key,
                    CaseCount = surgeryGroup.Count(),
                    AverageDurationMinutes = Math.Round(
                        surgeryGroup.Average(item => (decimal)item.DurationMinutes),
                        2)
                }
            )
            .OrderByDescending(item => item.AverageDurationMinutes)
            .ThenBy(item => item.SurgeryType)
            .ToListAsync();

        return result;
    }
    public async Task<ForecastSummaryDto> GetForecastSummaryAsync(int hospitalId)
    {
        await Task.CompletedTask;

        var currentWeekStart = StartOfWeek(DateTime.UtcNow.Date);

        var weeklyDemandTrend = new List<WeeklyDemandTrendDto>
    {
        new()
        {
            WeekStartDate = currentWeekStart.AddDays(-21),
            ActualCases = 11,
            ForecastedCases = null
        },
        new()
        {
            WeekStartDate = currentWeekStart.AddDays(-14),
            ActualCases = 14,
            ForecastedCases = null
        },
        new()
        {
            WeekStartDate = currentWeekStart.AddDays(-7),
            ActualCases = 16,
            ForecastedCases = null
        },
        new()
        {
            WeekStartDate = currentWeekStart,
            ActualCases = 18,
            ForecastedCases = null
        },
        new()
        {
            WeekStartDate = currentWeekStart.AddDays(7),
            ActualCases = 0,
            ForecastedCases = 21
        }
    };

        var surgeryTypeForecasts = new List<SurgeryTypeForecastDto>
    {
        new()
        {
            SurgeryType = "Spinal Decompression",
            HistoricalCaseCount = 8,
            AverageDurationMinutes = 180,
            ForecastedCasesNextWeek = 3,
            ForecastedHoursNextWeek = 9
        },
        new()
        {
            SurgeryType = "Knee Replacement",
            HistoricalCaseCount = 7,
            AverageDurationMinutes = 120,
            ForecastedCasesNextWeek = 4,
            ForecastedHoursNextWeek = 8
        },
        new()
        {
            SurgeryType = "Laparoscopic Cholecystectomy",
            HistoricalCaseCount = 6,
            AverageDurationMinutes = 90,
            ForecastedCasesNextWeek = 4,
            ForecastedHoursNextWeek = 6
        },
        new()
        {
            SurgeryType = "Craniotomy",
            HistoricalCaseCount = 5,
            AverageDurationMinutes = 150,
            ForecastedCasesNextWeek = 2,
            ForecastedHoursNextWeek = 5
        },
        new()
        {
            SurgeryType = "Ureteroscopy",
            HistoricalCaseCount = 4,
            AverageDurationMinutes = 120,
            ForecastedCasesNextWeek = 2,
            ForecastedHoursNextWeek = 4
        },
        new()
        {
            SurgeryType = "Tonsillectomy",
            HistoricalCaseCount = 5,
            AverageDurationMinutes = 60,
            ForecastedCasesNextWeek = 3,
            ForecastedHoursNextWeek = 3
        },
        new()
        {
            SurgeryType = "Skin Graft Procedure",
            HistoricalCaseCount = 3,
            AverageDurationMinutes = 90,
            ForecastedCasesNextWeek = 2,
            ForecastedHoursNextWeek = 3
        },
        new()
        {
            SurgeryType = "Diagnostic Procedure",
            HistoricalCaseCount = 4,
            AverageDurationMinutes = 60,
            ForecastedCasesNextWeek = 1,
            ForecastedHoursNextWeek = 1
        }
    };

        var predictedCases = (int)Math.Round(
            surgeryTypeForecasts.Sum(item => item.ForecastedCasesNextWeek),
            MidpointRounding.AwayFromZero);

        var predictedHours = Math.Round(
            surgeryTypeForecasts.Sum(item => item.ForecastedHoursNextWeek),
            2);

        var availableHours = 34m;

        var specialtyCapacityGaps = new List<SpecialtyCapacityGapDto>
    {
        new()
        {
            Specialty = "Neurosurgery",
            ForecastedHours = 14,
            AvailableBlockHours = 10,
            GapHours = 4
        },
        new()
        {
            Specialty = "Orthopedics",
            ForecastedHours = 8,
            AvailableBlockHours = 8,
            GapHours = 0
        },
        new()
        {
            Specialty = "General Surgery",
            ForecastedHours = 6,
            AvailableBlockHours = 7,
            GapHours = -1
        },
        new()
        {
            Specialty = "Urology",
            ForecastedHours = 4,
            AvailableBlockHours = 4,
            GapHours = 0
        },
        new()
        {
            Specialty = "ENT",
            ForecastedHours = 3,
            AvailableBlockHours = 3,
            GapHours = 0
        },
        new()
        {
            Specialty = "Plastic Surgery",
            ForecastedHours = 3,
            AvailableBlockHours = 2,
            GapHours = 1
        }
    };

        return new ForecastSummaryDto
        {
            PredictedCasesNextWeek = predictedCases,
            PredictedOrHoursNextWeek = predictedHours,
            AvailableBlockHoursNextWeek = availableHours,
            CapacityGapHours = Math.Round(predictedHours - availableHours, 2),

            SurgeryTypeForecasts = surgeryTypeForecasts
                .OrderByDescending(item => item.ForecastedHoursNextWeek)
                .ThenBy(item => item.SurgeryType)
                .ToList(),

            WeeklyDemandTrend = weeklyDemandTrend,

            SpecialtyCapacityGaps = specialtyCapacityGaps
                .OrderByDescending(item => item.GapHours)
                .ThenBy(item => item.Specialty)
                .ToList()
        };
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;

        return date.AddDays(-diff).Date;
    }
}