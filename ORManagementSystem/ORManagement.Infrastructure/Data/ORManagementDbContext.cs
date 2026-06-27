using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ORManagement.Infrastructure.Data.Entities;

namespace ORManagement.Infrastructure.Data;

public partial class ORManagementDbContext : DbContext
{
    public ORManagementDbContext(DbContextOptions<ORManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BlockAllocation> BlockAllocations { get; set; }

    public virtual DbSet<BlockException> BlockExceptions { get; set; }

    public virtual DbSet<ForecastRecommendation> ForecastRecommendations { get; set; }

    public virtual DbSet<ORRoomUtilizationRecord> ORRoomUtilizationRecords { get; set; }
    public virtual DbSet<Hospital> Hospitals { get; set; }

    public virtual DbSet<ORRequest> ORRequests { get; set; }

    public virtual DbSet<OperatingRoom> OperatingRooms { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PhiAccessLog> PhiAccessLogs { get; set; }

    public virtual DbSet<RecurringBlockTemplate> RecurringBlockTemplates { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReleasedSlot> ReleasedSlots { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SchedulingCycle> SchedulingCycles { get; set; }

    public virtual DbSet<Surgeon> Surgeons { get; set; }

    public virtual DbSet<SurgicalCase> SurgicalCases { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UtilizationRecord> UtilizationRecords { get; set; }

    public virtual DbSet<WaitlistRequest> WaitlistRequests { get; set; }

    public virtual DbSet<vw_CycleQueue> vw_CycleQueues { get; set; }

    public virtual DbSet<vw_ORCalendar> vw_ORCalendars { get; set; }

    public virtual DbSet<vw_SchedulerDashboard> vw_SchedulerDashboards { get; set; }

    public virtual DbSet<vw_SurgeonBlockPortfolio> vw_SurgeonBlockPortfolios { get; set; }

    public virtual DbSet<vw_UnderutilizedBlock> vw_UnderutilizedBlocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);

            entity.ToTable("AuditLogs", "audit", tb => tb.HasTrigger("trg_AuditLogs_NoChange"));

            entity.HasIndex(e => new { e.HospitalId, e.Entity, e.EntityId, e.CreatedAt }, "IX_AuditLogs_Hosp_Entity");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Entity).HasMaxLength(50);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.Property(e => e.OldValue).HasMaxLength(500);
            entity.Property(e => e.Remarks).HasMaxLength(300);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(300);

            entity.HasOne(d => d.Hospital).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.HospitalId)
                .HasConstraintName("FK_AuditLogs_Hospitals");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_Users");
        });
        modelBuilder.Entity<ORRoomUtilizationRecord>(entity =>
        {
            entity.HasKey(e => e.ORRoomUtilizationId);

            entity.ToTable("ORRoomUtilizationRecords", "analytics");

            entity.HasIndex(
                    e => new
                    {
                        e.HospitalId,
                        e.ORRoomId,
                        e.WeekStartDate
                    },
                    "UQ_ORRoomUtilizationRecords_Hosp_Room_Week")
                .IsUnique();

            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.Property(e => e.UtilStatus)
                .HasMaxLength(20);

            entity.Property(e => e.UtilizationPct)
                .HasComputedColumnSql(
                    "((CONVERT([decimal](9,2),[UsedMinutes])*(100.0))/nullif([AllocatedMinutes],(0)))",
                    true)
                .HasColumnType("numeric(25, 14)");

            entity.HasOne(d => d.Hospital)
                .WithMany()
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORRoomUtilizationRecords_Hospitals");

            entity.HasOne(d => d.ORRoom)
                .WithMany()
                .HasForeignKey(d => d.ORRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORRoomUtilizationRecords_OperatingRooms");
        });

        modelBuilder.Entity<BlockAllocation>(entity =>
        {
            entity.HasKey(e => e.BlockId);

            entity
                .ToTable("BlockAllocations", "scheduling")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("BlockAllocations_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.HospitalId, e.BlockDate }, "IX_BlockAllocations_Hosp_Date");

            entity.HasIndex(e => new { e.ORRoomId, e.BlockDate, e.StartTime }, "UQ_BlockAllocations_Room_Date_Start").IsUnique();

            entity.Property(e => e.BlockStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Allocated");
            entity.Property(e => e.BlockType).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Remarks).HasMaxLength(300);

            entity.HasOne(d => d.Hospital).WithMany(p => p.BlockAllocations)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlockAllocations_Hospitals");

            entity.HasOne(d => d.ModifiedByUser).WithMany(p => p.BlockAllocations)
                .HasForeignKey(d => d.ModifiedByUserId)
                .HasConstraintName("FK_BlockAllocations_Users");

            entity.HasOne(d => d.ORRoom).WithMany(p => p.BlockAllocations)
                .HasForeignKey(d => d.ORRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlockAllocations_OperatingRooms");

            entity.HasOne(d => d.Surgeon).WithMany(p => p.BlockAllocations)
                .HasForeignKey(d => d.SurgeonId)
                .HasConstraintName("FK_BlockAllocations_Surgeons");

            entity.HasOne(d => d.Template).WithMany(p => p.BlockAllocations)
                .HasForeignKey(d => d.TemplateId)
                .HasConstraintName("FK_BlockAllocations_Templates");
        });

        modelBuilder.Entity<BlockException>(entity =>
        {
            entity.HasKey(e => e.ExceptionId);

            entity.ToTable("BlockExceptions", "scheduling");

            entity.HasIndex(e => new { e.TemplateId, e.SkipDate }, "UQ_BlockExceptions_Template_Date").IsUnique();

            entity.Property(e => e.Reason).HasMaxLength(200);

            entity.HasOne(d => d.Template).WithMany(p => p.BlockExceptions)
                .HasForeignKey(d => d.TemplateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BlockExceptions_Templates");
        });

       

        modelBuilder.Entity<ForecastRecommendation>(entity =>
        {
            entity.HasKey(e => e.RecId);

            entity.ToTable("ForecastRecommendations", "analytics");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.RecStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.RuleId).HasMaxLength(10);

            entity.HasOne(d => d.Hospital).WithMany(p => p.ForecastRecommendations)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ForecastRecommendations_Hospitals");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ForecastRecommendations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_ForecastRecommendations_Users");
        });

        modelBuilder.Entity<Hospital>(entity =>
        {
            entity.ToTable("Hospitals", "org");

            entity.HasIndex(e => e.HospitalCode, "UQ_Hospitals_Code").IsUnique();

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.HospitalCode).HasMaxLength(20);
            entity.Property(e => e.HospitalName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Timezone).HasMaxLength(50);
        });

        modelBuilder.Entity<ORRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity
                .ToTable("ORRequests", "scheduling")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("ORRequests_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.CycleId, e.RequestStatus }, "IX_ORRequests_Cycle");

            entity.HasIndex(e => new { e.HospitalId, e.RequestStatus }, "IX_ORRequests_Hospital_Status");

            entity.Property(e => e.AvailableDaysMask).HasDefaultValue(31);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PatientReadiness).HasMaxLength(20);
            entity.Property(e => e.PreferredQuarter).HasMaxLength(10);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.Remarks).HasMaxLength(300);
            entity.Property(e => e.RequestStatus)
                .HasMaxLength(20)
                .HasDefaultValue("PendingReview");
            entity.Property(e => e.SchedulerRemarks).HasMaxLength(300);
            entity.Property(e => e.SurgeryType).HasMaxLength(100);

            entity.HasOne(d => d.Cycle).WithMany(p => p.ORRequestCycles)
                .HasForeignKey(d => d.CycleId)
                .HasConstraintName("FK_ORRequests_Cycle");

            entity.HasOne(d => d.Hospital).WithMany(p => p.ORRequests)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORRequests_Hospitals");

            entity.HasOne(d => d.ModifiedByUser).WithMany(p => p.ORRequests)
                .HasForeignKey(d => d.ModifiedByUserId)
                .HasConstraintName("FK_ORRequests_Users");

            entity.HasOne(d => d.OriginalCycle).WithMany(p => p.ORRequestOriginalCycles)
                .HasForeignKey(d => d.OriginalCycleId)
                .HasConstraintName("FK_ORRequests_OriginalCycle");

            entity.HasOne(d => d.Patient).WithMany(p => p.ORRequests)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORRequests_Patients");

            entity.HasOne(d => d.Surgeon).WithMany(p => p.ORRequests)
                .HasForeignKey(d => d.SurgeonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORRequests_Surgeons");
        });

        modelBuilder.Entity<OperatingRoom>(entity =>
        {
            entity.HasKey(e => e.ORRoomId);

            entity
                .ToTable("OperatingRooms", "facility")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("OperatingRooms_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.HospitalId, e.RoomName }, "UQ_OperatingRooms_Hospital_Name").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.RoomName).HasMaxLength(50);
            entity.Property(e => e.RoomType).HasMaxLength(50);

            entity.HasOne(d => d.Hospital).WithMany(p => p.OperatingRooms)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OperatingRooms_Hospitals");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity
                .ToTable("Patients", "clinical")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("Patients_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.HospitalId, e.MRN }, "UQ_Patients_Hospital_MRN").IsUnique();

            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MRN).HasMaxLength(20);

            entity.HasOne(d => d.Hospital).WithMany(p => p.Patients)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patients_Hospitals");
        });

        modelBuilder.Entity<PhiAccessLog>(entity =>
        {
            entity.HasKey(e => e.AccessId);

            entity.ToTable("PhiAccessLogs", "audit", tb => tb.HasTrigger("trg_PhiAccessLogs_NoChange"));

            entity.HasIndex(e => new { e.PatientId, e.AccessedAt }, "IX_PhiAccessLogs_Patient");

            entity.Property(e => e.AccessType).HasMaxLength(20);
            entity.Property(e => e.AccessedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Context).HasMaxLength(200);
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.UserAgent).HasMaxLength(300);

            entity.HasOne(d => d.Hospital).WithMany(p => p.PhiAccessLogs)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhiAccessLogs_Hospitals");

            entity.HasOne(d => d.Patient).WithMany(p => p.PhiAccessLogs)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhiAccessLogs_Patients");

            entity.HasOne(d => d.User).WithMany(p => p.PhiAccessLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PhiAccessLogs_Users");
        });

        modelBuilder.Entity<RecurringBlockTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);

            entity.ToTable("RecurringBlockTemplates", "scheduling");

            entity.Property(e => e.BlockType)
                .HasMaxLength(30)
                .HasDefaultValue("Recurring");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Specialty).HasMaxLength(100);

            entity.HasOne(d => d.ORRoom).WithMany(p => p.RecurringBlockTemplates)
                .HasForeignKey(d => d.ORRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecurringBlockTemplates_OperatingRooms");

            entity.HasOne(d => d.Surgeon).WithMany(p => p.RecurringBlockTemplates)
                .HasForeignKey(d => d.SurgeonId)
                .HasConstraintName("FK_RecurringBlockTemplates_Surgeons");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "auth");

            entity.HasIndex(e => new { e.TokenHash, e.ExpiresAt, e.RevokedAt }, "IX_RefreshTokens_Active");

            entity.HasIndex(e => new { e.UserId, e.ExpiresAt }, "IX_RefreshTokens_UserId");

            entity.HasIndex(e => e.TokenHash, "UQ_RefreshTokens_TokenHash").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(128);
            entity.Property(e => e.RevokedByIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
            entity.Property(e => e.UserAgent).HasMaxLength(300);

            entity.HasOne(d => d.Hospital).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.HospitalId)
                .HasConstraintName("FK_RefreshTokens_Hospitals");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<ReleasedSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId);

            entity.ToTable("ReleasedSlots", "scheduling");

            entity.HasIndex(e => new { e.HospitalId, e.SlotState, e.SlotDate }, "IX_ReleasedSlots_Hosp_State");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SlotState)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
            entity.Property(e => e.Source).HasMaxLength(30);

            entity.HasOne(d => d.Block).WithMany(p => p.ReleasedSlots)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReleasedSlots_Blocks");

            entity.HasOne(d => d.Hospital).WithMany(p => p.ReleasedSlots)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReleasedSlots_Hospitals");

            entity.HasOne(d => d.ORRoom).WithMany(p => p.ReleasedSlots)
                .HasForeignKey(d => d.ORRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReleasedSlots_Rooms");

            entity.HasOne(d => d.ReleasedByUser).WithMany(p => p.ReleasedSlots)
                .HasForeignKey(d => d.ReleasedByUserId)
                .HasConstraintName("FK_ReleasedSlots_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "auth");

            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SchedulingCycle>(entity =>
        {
            entity.HasKey(e => e.CycleId);

            entity.ToTable("SchedulingCycles", "scheduling");

            entity.HasIndex(e => new { e.HospitalId, e.CycleStatus }, "IX_SchedulingCycles_Hosp_Status");

            entity.HasIndex(e => new { e.HospitalId, e.WeekStartDate }, "UQ_SchedulingCycles_Hospital_Week").IsUnique();

            entity.Property(e => e.CycleStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Open");

            entity.HasOne(d => d.Hospital).WithMany(p => p.SchedulingCycles)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SchedulingCycles_Hospitals");
        });

        modelBuilder.Entity<Surgeon>(entity =>
        {
            entity.ToTable("Surgeons", "clinical");

            entity.HasIndex(e => e.UserId, "UQ_Surgeons_UserId").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Specialty).HasMaxLength(100);

            entity.HasOne(d => d.Hospital).WithMany(p => p.Surgeons)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Surgeons_Hospitals");

            entity.HasOne(d => d.User).WithOne(p => p.Surgeon)
                .HasForeignKey<Surgeon>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Surgeons_Users");
        });

        modelBuilder.Entity<SurgicalCase>(entity =>
        {
            entity.HasKey(e => e.SurgeryId);

            entity
                .ToTable("SurgicalCases", "scheduling")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("SurgicalCases_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.BlockId, "IX_SurgicalCases_Block");

            entity.HasIndex(e => new { e.HospitalId, e.CaseStatus, e.ScheduledStart }, "IX_SurgicalCases_Hosp_Status");

            entity.HasIndex(e => e.RequestId, "UQ_SurgicalCases_RequestId").IsUnique();

            entity.Property(e => e.CancellationReason).HasMaxLength(50);
            entity.Property(e => e.CaseStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Scheduled");

            entity.HasOne(d => d.Block).WithMany(p => p.SurgicalCases)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SurgicalCases_Blocks");

            entity.HasOne(d => d.Hospital).WithMany(p => p.SurgicalCases)
                .HasForeignKey(d => d.HospitalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SurgicalCases_Hospitals");

            entity.HasOne(d => d.ModifiedByUser).WithMany(p => p.SurgicalCases)
                .HasForeignKey(d => d.ModifiedByUserId)
                .HasConstraintName("FK_SurgicalCases_Users");

            entity.HasOne(d => d.ORRoom).WithMany(p => p.SurgicalCases)
                .HasForeignKey(d => d.ORRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SurgicalCases_Rooms");

            entity.HasOne(d => d.Request).WithOne(p => p.SurgicalCase)
                .HasForeignKey<SurgicalCase>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SurgicalCases_ORRequests");

            entity.HasOne(d => d.Surgeon).WithMany(p => p.SurgicalCases)
                .HasForeignKey(d => d.SurgeonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SurgicalCases_Surgeons");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId);

            entity.ToTable("SystemSettings", "config");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SettingKey).HasMaxLength(50);
            entity.Property(e => e.SettingValue).HasMaxLength(100);

            entity.HasOne(d => d.Hospital).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.HospitalId)
                .HasConstraintName("FK_SystemSettings_Hospitals");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity
                .ToTable("Users", "auth")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("Users_History", "history");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasOne(d => d.Hospital).WithMany(p => p.Users)
                .HasForeignKey(d => d.HospitalId)
                .HasConstraintName("FK_Users_Hospitals");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<UtilizationRecord>(entity =>
        {
            entity.HasKey(e => e.UtilizationId);

            entity.ToTable("UtilizationRecords", "analytics");

            entity.HasIndex(e => new { e.BlockId, e.CalculatedAt }, "IX_UtilizationRecords_Block");

            entity.HasIndex(e => e.BlockId, "UQ_UtilizationRecords_BlockId")
                .IsUnique();

            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.Property(e => e.UtilStatus)
                .HasMaxLength(20);

            entity.Property(e => e.UtilizationPct)
                .HasComputedColumnSql(
                    "((CONVERT(9,2,[UsedMinutes])*(100.0))/nullif([AllocatedMinutes],(0)))",
                    true)
                .HasColumnType("numeric(25, 14)");

            entity.HasOne(d => d.Block).WithMany(p => p.UtilizationRecords)
                .HasForeignKey(d => d.BlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UtilizationRecords_Blocks");
        });



        modelBuilder.Entity<WaitlistRequest>(entity =>
        {
            entity.HasKey(e => e.WaitlistId);

            entity.ToTable("WaitlistRequests", "scheduling");

            entity.HasIndex(e => e.RequestId, "UQ_WaitlistRequests_RequestId").IsUnique();

            entity.Property(e => e.MatchScore).HasColumnType("decimal(9, 2)");
            entity.Property(e => e.WaitingSince).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.MatchedSlot).WithMany(p => p.WaitlistRequests)
                .HasForeignKey(d => d.MatchedSlotId)
                .HasConstraintName("FK_WaitlistRequests_Slots");

            entity.HasOne(d => d.Request).WithOne(p => p.WaitlistRequest)
                .HasForeignKey<WaitlistRequest>(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_WaitlistRequests_ORRequests");
        });

        modelBuilder.Entity<vw_CycleQueue>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CycleQueue", "scheduling");

            entity.Property(e => e.AvailableDaysDisplay)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PatientReadiness).HasMaxLength(20);
            entity.Property(e => e.PreferredQuarter).HasMaxLength(10);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.SurgeonName).HasMaxLength(150);
            entity.Property(e => e.SurgeryType).HasMaxLength(100);
        });

        modelBuilder.Entity<vw_ORCalendar>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ORCalendar", "scheduling");

            entity.Property(e => e.BlockStatus).HasMaxLength(20);
            entity.Property(e => e.CaseStatus).HasMaxLength(20);
            entity.Property(e => e.RoomName).HasMaxLength(50);
            entity.Property(e => e.SurgeonName).HasMaxLength(150);
        });

        modelBuilder.Entity<vw_SchedulerDashboard>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SchedulerDashboard", "analytics");

            entity.Property(e => e.HospitalId).ValueGeneratedOnAdd();
            entity.Property(e => e.HospitalName).HasMaxLength(150);
        });

        modelBuilder.Entity<vw_SurgeonBlockPortfolio>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_SurgeonBlockPortfolio", "analytics");

            entity.Property(e => e.BlockStatus).HasMaxLength(20);
            entity.Property(e => e.RoomName).HasMaxLength(50);
            entity.Property(e => e.SurgeonName).HasMaxLength(150);
        });

        modelBuilder.Entity<vw_UnderutilizedBlock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_UnderutilizedBlocks", "analytics");

            entity.Property(e => e.UtilStatus).HasMaxLength(20);
            entity.Property(e => e.UtilizationPct).HasColumnType("numeric(25, 14)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
