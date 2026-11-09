using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AttendanceAdjustmentHistory> AttendanceAdjustmentHistories { get; set; }

    public virtual DbSet<AttendancePenaltyLevel> AttendancePenaltyLevels { get; set; }

    public virtual DbSet<AttendancePenaltyRecord> AttendancePenaltyRecords { get; set; }

    public virtual DbSet<AttendancePenaltyRule> AttendancePenaltyRules { get; set; }

    public virtual DbSet<AttendanceRule> AttendanceRules { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Checklist> Checklists { get; set; }

    public virtual DbSet<ChecklistItem> ChecklistItems { get; set; }

    public virtual DbSet<ChecklistResult> ChecklistResults { get; set; }

    public virtual DbSet<ContractChangeRequest> ContractChangeRequests { get; set; }

    public virtual DbSet<ContractChangeRequestItem> ContractChangeRequestItems { get; set; }

    public virtual DbSet<EmployeeRequest> EmployeeRequests { get; set; }

    public virtual DbSet<EmployeeRewardsPenalty> EmployeeRewardsPenalties { get; set; }

    public virtual DbSet<EmployeeType> EmployeeTypes { get; set; }

    public virtual DbSet<EmploymentContract> EmploymentContracts { get; set; }

    public virtual DbSet<FaceProfile> FaceProfiles { get; set; }

    public virtual DbSet<FaceVerificationSession> FaceVerificationSessions { get; set; }

    public virtual DbSet<HolidayPayRule> HolidayPayRules { get; set; }

    public virtual DbSet<LeaveRequestAssignment> LeaveRequestAssignments { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PayrollCarryForwardApplication> PayrollCarryForwardApplications { get; set; }

    public virtual DbSet<PayrollCarryForwardDeduction> PayrollCarryForwardDeductions { get; set; }

    public virtual DbSet<PayrollCorrectionRequest> PayrollCorrectionRequests { get; set; }

    public virtual DbSet<PayrollDetail> PayrollDetails { get; set; }

    public virtual DbSet<PayrollFeedbackRequest> PayrollFeedbackRequests { get; set; }

    public virtual DbSet<PayrollPeriod> PayrollPeriods { get; set; }

    public virtual DbSet<Payslip> Payslips { get; set; }

    public virtual DbSet<PayslipWorkDetail> PayslipWorkDetails { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<PublicHolidayConfig> PublicHolidayConfigs { get; set; }

    public virtual DbSet<Region> Regions { get; set; }

    public virtual DbSet<RegionalMinimumWage> RegionalMinimumWages { get; set; }

    public virtual DbSet<RequestLimitRule> RequestLimitRules { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryChangeRequest> SalaryChangeRequests { get; set; }

    public virtual DbSet<SalaryIncreaseRequestDetail> SalaryIncreaseRequestDetails { get; set; }

    public virtual DbSet<ShiftAssignment> ShiftAssignments { get; set; }

    public virtual DbSet<ShiftEmployeeRequirement> ShiftEmployeeRequirements { get; set; }

    public virtual DbSet<ShiftSwapRequest> ShiftSwapRequests { get; set; }

    public virtual DbSet<ShiftTemplate> ShiftTemplates { get; set; }

    public virtual DbSet<StaffSalaryHistory> StaffSalaryHistories { get; set; }

    public virtual DbSet<TimeAdjustmentRequestDetail> TimeAdjustmentRequestDetails { get; set; }

    public virtual DbSet<Timesheet> Timesheets { get; set; }

    public virtual DbSet<TimesheetAdjustmentRequest> TimesheetAdjustmentRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserNotification> UserNotifications { get; set; }

    public virtual DbSet<WorkScheduleWeek> WorkScheduleWeeks { get; set; }

    public virtual DbSet<WorkShift> WorkShifts { get; set; }

    public virtual DbSet<WorkingTimeRule> WorkingTimeRules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone", "recovery_code" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in", "like", "ilike", "is", "match", "imatch", "isdistinct" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("vector")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("attendances_pkey");

            entity.ToTable("attendances");

            entity.HasIndex(e => e.AssignmentId, "attendances_assignment_id_key").IsUnique();

            entity.HasIndex(e => e.CheckInTime, "ix_attendances_check_in");

            entity.HasIndex(e => new { e.AttendanceRuleId, e.Status }, "ix_attendances_rule_status");

            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.ActualWorkingMinutes)
                .HasDefaultValue(0)
                .HasColumnName("actual_working_minutes");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.AttendanceRuleId).HasColumnName("attendance_rule_id");
            entity.Property(e => e.BranchAddressSnapshot)
                .HasMaxLength(500)
                .HasColumnName("branch_address_snapshot");
            entity.Property(e => e.BranchLatitudeSnapshot)
                .HasPrecision(9, 6)
                .HasColumnName("branch_latitude_snapshot");
            entity.Property(e => e.BranchLongitudeSnapshot)
                .HasPrecision(10, 6)
                .HasColumnName("branch_longitude_snapshot");
            entity.Property(e => e.BranchNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("branch_name_snapshot");
            entity.Property(e => e.ChargeableEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_early_leave_minutes");
            entity.Property(e => e.ChargeableLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_late_minutes");
            entity.Property(e => e.CheckInAccuracyMeters)
                .HasPrecision(10, 2)
                .HasColumnName("check_in_accuracy_meters");
            entity.Property(e => e.CheckInDistanceMeters)
                .HasPrecision(10, 2)
                .HasColumnName("check_in_distance_meters");
            entity.Property(e => e.CheckInFaceConfidence)
                .HasPrecision(5, 4)
                .HasColumnName("check_in_face_confidence");
            entity.Property(e => e.CheckInFaceSessionId).HasColumnName("check_in_face_session_id");
            entity.Property(e => e.CheckInFaceVerified).HasColumnName("check_in_face_verified");
            entity.Property(e => e.CheckInLatitude)
                .HasPrecision(9, 6)
                .HasColumnName("check_in_latitude");
            entity.Property(e => e.CheckInLongitude)
                .HasPrecision(9, 6)
                .HasColumnName("check_in_longitude");
            entity.Property(e => e.CheckInTime).HasColumnName("check_in_time");
            entity.Property(e => e.CheckOutAccuracyMeters)
                .HasPrecision(10, 2)
                .HasColumnName("check_out_accuracy_meters");
            entity.Property(e => e.CheckOutDistanceMeters)
                .HasPrecision(10, 2)
                .HasColumnName("check_out_distance_meters");
            entity.Property(e => e.CheckOutFaceConfidence)
                .HasPrecision(5, 4)
                .HasColumnName("check_out_face_confidence");
            entity.Property(e => e.CheckOutFaceSessionId).HasColumnName("check_out_face_session_id");
            entity.Property(e => e.CheckOutFaceVerified).HasColumnName("check_out_face_verified");
            entity.Property(e => e.CheckOutLatitude)
                .HasPrecision(9, 6)
                .HasColumnName("check_out_latitude");
            entity.Property(e => e.CheckOutLongitude)
                .HasPrecision(10, 6)
                .HasColumnName("check_out_longitude");
            entity.Property(e => e.CheckOutTime).HasColumnName("check_out_time");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EarlyLeaveRequestId).HasColumnName("early_leave_request_id");
            entity.Property(e => e.EmployeeFullNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("employee_full_name_snapshot");
            entity.Property(e => e.ExcusedEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_early_leave_minutes");
            entity.Property(e => e.ExcusedLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_late_minutes");
            entity.Property(e => e.LateArrivalRequestId).HasColumnName("late_arrival_request_id");
            entity.Property(e => e.RuleSnapshotJson)
                .HasColumnType("jsonb")
                .HasColumnName("rule_snapshot_json");
            entity.Property(e => e.ShiftEndAtSnapshot).HasColumnName("shift_end_at_snapshot");
            entity.Property(e => e.ShiftStartAtSnapshot).HasColumnName("shift_start_at_snapshot");
            entity.Property(e => e.Status)
                .HasMaxLength(40)
                .HasDefaultValueSql("'IN_PROGRESS'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_early_leave_minutes");
            entity.Property(e => e.TotalLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_late_minutes");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Assignment).WithOne(p => p.Attendance)
                .HasForeignKey<Attendance>(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_assignment_id_fkey");

            entity.HasOne(d => d.AttendanceRule).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.AttendanceRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_attendance_rule_id_fkey");

            entity.HasOne(d => d.CheckInFaceSession).WithMany(p => p.AttendanceCheckInFaceSessions)
                .HasForeignKey(d => d.CheckInFaceSessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_check_in_face_session_id_fkey");

            entity.HasOne(d => d.CheckOutFaceSession).WithMany(p => p.AttendanceCheckOutFaceSessions)
                .HasForeignKey(d => d.CheckOutFaceSessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_check_out_face_session_id_fkey");

            entity.HasOne(d => d.EarlyLeaveRequest).WithMany(p => p.AttendanceEarlyLeaveRequests)
                .HasForeignKey(d => d.EarlyLeaveRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_early_leave_request_id_fkey");

            entity.HasOne(d => d.LateArrivalRequest).WithMany(p => p.AttendanceLateArrivalRequests)
                .HasForeignKey(d => d.LateArrivalRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendances_late_arrival_request_id_fkey");
        });

        modelBuilder.Entity<AttendanceAdjustmentHistory>(entity =>
        {
            entity.HasKey(e => e.AdjustmentHistoryId).HasName("attendance_adjustment_histories_pkey");

            entity.ToTable("attendance_adjustment_histories");

            entity.HasIndex(e => new { e.AdjustedBy, e.AdjustedAt }, "ix_attendance_adjustment_history_adjusted_by").IsDescending(false, true);

            entity.HasIndex(e => new { e.AttendanceId, e.AdjustedAt }, "ix_attendance_adjustment_history_attendance").IsDescending(false, true);

            entity.Property(e => e.AdjustmentHistoryId).HasColumnName("adjustment_history_id");
            entity.Property(e => e.AdjustedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("adjusted_at");
            entity.Property(e => e.AdjustedBy).HasColumnName("adjusted_by");
            entity.Property(e => e.AdjustmentSource)
                .HasMaxLength(30)
                .HasColumnName("adjustment_source");
            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.NewActualWorkingMinutes).HasColumnName("new_actual_working_minutes");
            entity.Property(e => e.NewChargeableEarlyLeaveMinutes).HasColumnName("new_chargeable_early_leave_minutes");
            entity.Property(e => e.NewChargeableLateMinutes).HasColumnName("new_chargeable_late_minutes");
            entity.Property(e => e.NewCheckInTime).HasColumnName("new_check_in_time");
            entity.Property(e => e.NewCheckOutTime).HasColumnName("new_check_out_time");
            entity.Property(e => e.NewTotalEarlyLeaveMinutes).HasColumnName("new_total_early_leave_minutes");
            entity.Property(e => e.NewTotalLateMinutes).HasColumnName("new_total_late_minutes");
            entity.Property(e => e.OldActualWorkingMinutes).HasColumnName("old_actual_working_minutes");
            entity.Property(e => e.OldChargeableEarlyLeaveMinutes).HasColumnName("old_chargeable_early_leave_minutes");
            entity.Property(e => e.OldChargeableLateMinutes).HasColumnName("old_chargeable_late_minutes");
            entity.Property(e => e.OldCheckInTime).HasColumnName("old_check_in_time");
            entity.Property(e => e.OldCheckOutTime).HasColumnName("old_check_out_time");
            entity.Property(e => e.OldTotalEarlyLeaveMinutes).HasColumnName("old_total_early_leave_minutes");
            entity.Property(e => e.OldTotalLateMinutes).HasColumnName("old_total_late_minutes");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");

            entity.HasOne(d => d.AdjustedByNavigation).WithMany(p => p.AttendanceAdjustmentHistories)
                .HasForeignKey(d => d.AdjustedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_adjustment_histories_adjusted_by_fkey");

            entity.HasOne(d => d.Attendance).WithMany(p => p.AttendanceAdjustmentHistories)
                .HasForeignKey(d => d.AttendanceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_adjustment_histories_attendance_id_fkey");
        });

        modelBuilder.Entity<AttendancePenaltyLevel>(entity =>
        {
            entity.HasKey(e => e.PenaltyLevelId).HasName("attendance_penalty_levels_pkey");

            entity.ToTable("attendance_penalty_levels");

            entity.HasIndex(e => new { e.PenaltyRuleId, e.ViolationType, e.FromMinutes, e.ToMinutes }, "ix_penalty_levels_lookup");

            entity.HasIndex(e => new { e.PenaltyRuleId, e.ViolationType, e.FromMinutes }, "uq_penalty_level_start").IsUnique();

            entity.Property(e => e.PenaltyLevelId).HasColumnName("penalty_level_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FromMinutes).HasColumnName("from_minutes");
            entity.Property(e => e.PenaltyAmount)
                .HasPrecision(18, 2)
                .HasColumnName("penalty_amount");
            entity.Property(e => e.PenaltyRuleId).HasColumnName("penalty_rule_id");
            entity.Property(e => e.ToMinutes).HasColumnName("to_minutes");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.ViolationType)
                .HasMaxLength(20)
                .HasColumnName("violation_type");

            entity.HasOne(d => d.PenaltyRule).WithMany(p => p.AttendancePenaltyLevels)
                .HasForeignKey(d => d.PenaltyRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_levels_penalty_rule_id_fkey");
        });

        modelBuilder.Entity<AttendancePenaltyRecord>(entity =>
        {
            entity.HasKey(e => e.AttendancePenaltyId).HasName("attendance_penalty_records_pkey");

            entity.ToTable("attendance_penalty_records");

            entity.HasIndex(e => new { e.AttendanceId, e.ViolationType }, "uq_attendance_penalty_type_applied")
                .IsUnique()
                .HasFilter("((status)::text = 'APPLIED'::text)");

            entity.Property(e => e.AttendancePenaltyId).HasColumnName("attendance_penalty_id");
            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PenaltyAmount)
                .HasPrecision(18, 2)
                .HasColumnName("penalty_amount");
            entity.Property(e => e.PenaltyLevelId).HasColumnName("penalty_level_id");
            entity.Property(e => e.PenaltyRuleId).HasColumnName("penalty_rule_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'APPLIED'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.ViolationMinutes).HasColumnName("violation_minutes");
            entity.Property(e => e.ViolationType)
                .HasMaxLength(20)
                .HasColumnName("violation_type");

            entity.HasOne(d => d.Attendance).WithMany(p => p.AttendancePenaltyRecords)
                .HasForeignKey(d => d.AttendanceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_records_attendance_id_fkey");

            entity.HasOne(d => d.PenaltyLevel).WithMany(p => p.AttendancePenaltyRecords)
                .HasForeignKey(d => d.PenaltyLevelId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_records_penalty_level_id_fkey");

            entity.HasOne(d => d.PenaltyRule).WithMany(p => p.AttendancePenaltyRecords)
                .HasForeignKey(d => d.PenaltyRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_records_penalty_rule_id_fkey");
        });

        modelBuilder.Entity<AttendancePenaltyRule>(entity =>
        {
            entity.HasKey(e => e.PenaltyRuleId).HasName("attendance_penalty_rules_pkey");

            entity.ToTable("attendance_penalty_rules");

            entity.HasIndex(e => new { e.Status, e.EffectiveFrom }, "ix_attendance_penalty_rules_due").HasFilter("((status)::text = 'SCHEDULED'::text)");

            entity.Property(e => e.PenaltyRuleId).HasColumnName("penalty_rule_id");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.RuleName)
                .HasMaxLength(150)
                .HasColumnName("rule_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.AttendancePenaltyRuleCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_rules_cancelled_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AttendancePenaltyRuleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_rules_created_by_fkey");

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.AttendancePenaltyRulePublishedByNavigations)
                .HasForeignKey(d => d.PublishedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_penalty_rules_published_by_fkey");
        });

        modelBuilder.Entity<AttendanceRule>(entity =>
        {
            entity.HasKey(e => e.AttendanceRuleId).HasName("attendance_rules_pkey");

            entity.ToTable("attendance_rules");

            entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo }, "ix_attendance_rules_effective");

            entity.Property(e => e.AttendanceRuleId).HasColumnName("attendance_rule_id");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CheckInBeforeMinutes)
                .HasDefaultValue(30)
                .HasColumnName("check_in_before_minutes");
            entity.Property(e => e.CheckInGraceMinutes)
                .HasDefaultValue(0)
                .HasColumnName("check_in_grace_minutes");
            entity.Property(e => e.CheckOutAfterMinutes)
                .HasDefaultValue(30)
                .HasColumnName("check_out_after_minutes");
            entity.Property(e => e.CheckOutGraceMinutes)
                .HasDefaultValue(0)
                .HasColumnName("check_out_grace_minutes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.MaxAttendanceDistanceMeters).HasColumnName("max_attendance_distance_meters");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.RuleName)
                .HasMaxLength(150)
                .HasColumnName("rule_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.AttendanceRuleCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_rules_cancelled_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AttendanceRuleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_rules_created_by_fkey");

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.AttendanceRulePublishedByNavigations)
                .HasForeignKey(d => d.PublishedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attendance_rules_published_by_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => new { e.TableName, e.RecordId, e.CreatedAt }, "ix_audit_logs_table_record").IsDescending(false, false, true);

            entity.Property(e => e.AuditLogId).HasColumnName("audit_log_id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.NewValue)
                .HasColumnType("jsonb")
                .HasColumnName("new_value");
            entity.Property(e => e.OldValue)
                .HasColumnType("jsonb")
                .HasColumnName("old_value");
            entity.Property(e => e.RecordId)
                .HasMaxLength(100)
                .HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("branches_pkey");

            entity.ToTable("branches");

            entity.HasIndex(e => e.BranchCode, "branches_branch_code_key").IsUnique();

            entity.HasIndex(e => new { e.RegionId, e.Status }, "ix_branches_region_status");

            entity.HasIndex(e => e.BranchName, "uq_branch_name").IsUnique();

            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.BranchCode)
                .HasMaxLength(30)
                .HasColumnName("branch_code");
            entity.Property(e => e.BranchName)
                .HasMaxLength(150)
                .HasColumnName("branch_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.Latitude)
                .HasPrecision(9, 6)
                .HasColumnName("latitude");
            entity.Property(e => e.Longitude)
                .HasPrecision(10, 6)
                .HasColumnName("longitude");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Region).WithMany(p => p.Branches)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("branches_region_id_fkey");
        });

        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.HasKey(e => e.ChecklistId).HasName("checklists_pkey");

            entity.ToTable("checklists");

            entity.HasIndex(e => e.ShiftId, "ix_checklists_shift").HasFilter("(is_deleted = false)");

            entity.Property(e => e.ChecklistId).HasColumnName("checklist_id");
            entity.Property(e => e.ChecklistName)
                .HasMaxLength(150)
                .HasColumnName("checklist_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Checklists)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("checklists_created_by_fkey");

            entity.HasOne(d => d.Shift).WithMany(p => p.Checklists)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("checklists_shift_id_fkey");
        });

        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.HasKey(e => e.ChecklistItemId).HasName("checklist_items_pkey");

            entity.ToTable("checklist_items");

            entity.HasIndex(e => new { e.ChecklistId, e.DisplayOrder }, "uq_checklist_item_order").IsUnique();

            entity.Property(e => e.ChecklistItemId).HasColumnName("checklist_item_id");
            entity.Property(e => e.ChecklistId).HasColumnName("checklist_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("is_required");
            entity.Property(e => e.ItemName)
                .HasMaxLength(255)
                .HasColumnName("item_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Checklist).WithMany(p => p.ChecklistItems)
                .HasForeignKey(d => d.ChecklistId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("checklist_items_checklist_id_fkey");
        });

        modelBuilder.Entity<ChecklistResult>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("checklist_results_pkey");

            entity.ToTable("checklist_results");

            entity.HasIndex(e => e.UserId, "ix_checklist_results_user").HasFilter("(is_deleted = false)");

            entity.HasIndex(e => new { e.ChecklistItemId, e.UserId }, "uq_checklist_result_user_item").IsUnique();

            entity.Property(e => e.ResultId).HasColumnName("result_id");
            entity.Property(e => e.ChecklistItemId).HasColumnName("checklist_item_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PhotoUrl)
                .HasMaxLength(500)
                .HasColumnName("photo_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ChecklistItem).WithMany(p => p.ChecklistResults)
                .HasForeignKey(d => d.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("checklist_results_checklist_item_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ChecklistResults)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("checklist_results_user_id_fkey");
        });

        modelBuilder.Entity<ContractChangeRequest>(entity =>
        {
            entity.HasKey(e => e.ChangeRequestId).HasName("contract_change_requests_pkey");

            entity.ToTable("contract_change_requests");

            entity.HasIndex(e => e.ContractId, "uq_contract_open_change_request")
                .IsUnique()
                .HasFilter("((status)::text = ANY ((ARRAY['DRAFT'::character varying, 'SUBMITTED'::character varying, 'APPROVED'::character varying])::text[]))");

            entity.Property(e => e.ChangeRequestId).HasColumnName("change_request_id");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.RequestType)
                .HasMaxLength(30)
                .HasColumnName("request_type");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ReviewComment).HasColumnName("review_comment");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.StaffComment).HasColumnName("staff_comment");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Contract).WithOne(p => p.ContractChangeRequest)
                .HasForeignKey<ContractChangeRequest>(d => d.ContractId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("contract_change_requests_contract_id_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ContractChangeRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("contract_change_requests_requested_by_fkey");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ContractChangeRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("contract_change_requests_reviewed_by_fkey");
        });

        modelBuilder.Entity<ContractChangeRequestItem>(entity =>
        {
            entity.HasKey(e => e.ChangeRequestItemId).HasName("contract_change_request_items_pkey");

            entity.ToTable("contract_change_request_items");

            entity.HasIndex(e => new { e.ChangeRequestId, e.FieldName }, "uq_contract_change_request_field").IsUnique();

            entity.Property(e => e.ChangeRequestItemId).HasColumnName("change_request_item_id");
            entity.Property(e => e.ChangeRequestId).HasColumnName("change_request_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.FieldName)
                .HasMaxLength(100)
                .HasColumnName("field_name");
            entity.Property(e => e.OriginalValue)
                .HasColumnType("jsonb")
                .HasColumnName("original_value");
            entity.Property(e => e.ProposedValue)
                .HasColumnType("jsonb")
                .HasColumnName("proposed_value");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ChangeRequest).WithMany(p => p.ContractChangeRequestItems)
                .HasForeignKey(d => d.ChangeRequestId)
                .HasConstraintName("contract_change_request_items_change_request_id_fkey");
        });

        modelBuilder.Entity<EmployeeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("employee_requests_pkey");

            entity.ToTable("employee_requests");

            entity.HasIndex(e => new { e.BranchId, e.RequestType, e.Status }, "ix_employee_requests_branch_type_status");

            entity.HasIndex(e => new { e.UserId, e.Status, e.CreatedAt }, "ix_employee_requests_user_status_created").IsDescending(false, false, true);

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AppliedRequestLimitRuleId).HasColumnName("applied_request_limit_rule_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RequestType)
                .HasMaxLength(30)
                .HasColumnName("request_type");
            entity.Property(e => e.ReviewNote)
                .HasMaxLength(500)
                .HasColumnName("review_note");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AppliedRequestLimitRule).WithMany(p => p.EmployeeRequests)
                .HasForeignKey(d => d.AppliedRequestLimitRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_requests_applied_request_limit_rule_id_fkey");

            entity.HasOne(d => d.Branch).WithMany(p => p.EmployeeRequests)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_requests_branch_id_fkey");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.EmployeeRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_requests_reviewed_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_requests_user_id_fkey");
        });

        modelBuilder.Entity<EmployeeRewardsPenalty>(entity =>
        {
            entity.HasKey(e => e.RewardPenaltyId).HasName("employee_rewards_penalties_pkey");

            entity.ToTable("employee_rewards_penalties");

            entity.HasIndex(e => e.PayrollId, "ix_rewards_penalties_payroll");

            entity.HasIndex(e => new { e.PayrollPeriodId, e.UserId }, "ix_rewards_penalties_payroll_period_user").HasFilter("(is_deleted = false)");

            entity.Property(e => e.RewardPenaltyId).HasColumnName("reward_penalty_id");
            entity.Property(e => e.AdjustmentName)
                .HasMaxLength(100)
                .HasColumnName("adjustment_name");
            entity.Property(e => e.AdjustmentType)
                .HasMaxLength(20)
                .HasColumnName("adjustment_type");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.ApprovalStatus)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("approval_status");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.DeletionReason)
                .HasMaxLength(500)
                .HasColumnName("deletion_reason");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.PayrollPeriodId).IsRequired(false).HasColumnName("payroll_period_id");
            entity.Property(e => e.AdjustmentDate).HasColumnName("adjustment_date");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RejectedAt).HasColumnName("rejected_at");
            entity.Property(e => e.RejectedBy).HasColumnName("rejected_by");
            entity.Property(e => e.ReviewNote).HasColumnName("review_note");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.EmployeeRewardsPenaltyApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("employee_rewards_penalties_approved_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmployeeRewardsPenaltyCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_rewards_penalties_created_by_fkey");

            entity.HasOne(d => d.DeletedByNavigation).WithMany(p => p.EmployeeRewardsPenaltyDeletedByNavigations)
                .HasForeignKey(d => d.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_rewards_penalties_deleted_by_fkey");

            entity.HasOne(d => d.Payroll).WithMany(p => p.EmployeeRewardsPenalties)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_rewards_penalties_payroll_id_fkey");

            entity.HasOne(d => d.PayrollPeriod).WithMany(p => p.EmployeeRewardsPenalties)
                .HasForeignKey(d => d.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_rewards_penalties_payroll_period_id_fkey");

            entity.HasOne(d => d.RejectedByNavigation).WithMany(p => p.EmployeeRewardsPenaltyRejectedByNavigations)
                .HasForeignKey(d => d.RejectedBy)
                .HasConstraintName("employee_rewards_penalties_rejected_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.EmployeeRewardsPenaltyUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employee_rewards_penalties_user_id_fkey");
        });

        modelBuilder.Entity<EmployeeType>(entity =>
        {
            entity.HasKey(e => e.EmployeeTypeId).HasName("employee_types_pkey");

            entity.ToTable("employee_types");

            entity.HasIndex(e => e.TypeName, "employee_types_type_name_key").IsUnique();

            entity.Property(e => e.EmployeeTypeId).HasColumnName("employee_type_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .HasColumnName("type_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmploymentContract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("employment_contracts_pkey");

            entity.ToTable("employment_contracts");

            entity.HasIndex(e => e.ContractNumber, "employment_contracts_contract_number_key").IsUnique();

            entity.HasIndex(e => new { e.BranchId, e.Status }, "ix_contracts_branch_status");

            entity.HasIndex(e => new { e.UserId, e.Status, e.StartDate, e.EndDate }, "ix_contracts_user_status_dates");

            entity.HasIndex(e => e.CancelledBy, "ix_employment_contracts_cancelled_by");

            entity.HasIndex(e => e.UserId, "uq_one_active_contract_per_staff")
                .IsUnique()
                .HasFilter("((status)::text = 'ACTIVE'::text)");

            entity.HasIndex(e => e.UserId, "uq_one_pending_contract_per_staff")
                .IsUnique()
                .HasFilter("((status)::text = ANY ((ARRAY['DRAFT'::character varying, 'PENDING_STAFF'::character varying, 'PENDING_MANAGER_APPROVAL'::character varying])::text[]))");

            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.BranchAddressSnapshot)
                .HasMaxLength(500)
                .HasColumnName("branch_address_snapshot");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.BranchNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("branch_name_snapshot");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.ContractFilePath)
                .HasMaxLength(500)
                .HasColumnName("contract_file_path");
            entity.Property(e => e.ContractNumber)
                .HasMaxLength(50)
                .HasColumnName("contract_number");
            entity.Property(e => e.ContractTemplateVersion)
                .HasMaxLength(30)
                .HasColumnName("contract_template_version");
            entity.Property(e => e.ContractTerms).HasColumnName("contract_terms");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EmployeeAddressSnapshot)
                .HasMaxLength(500)
                .HasColumnName("employee_address_snapshot");
            entity.Property(e => e.EmployeeFullNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("employee_full_name_snapshot");
            entity.Property(e => e.EmployeeIdentityNoSnapshot)
                .HasMaxLength(30)
                .HasColumnName("employee_identity_no_snapshot");
            entity.Property(e => e.EmployeePhoneSnapshot)
                .HasMaxLength(20)
                .HasColumnName("employee_phone_snapshot");
            entity.Property(e => e.EmployeePositionSnapshot)
                .HasMaxLength(100)
                .HasColumnName("employee_position_snapshot");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.HourlyRate)
                .HasPrecision(18, 2)
                .HasColumnName("hourly_rate");
            entity.Property(e => e.ManagerRejectionComment).HasColumnName("manager_rejection_comment");
            entity.Property(e => e.PdfGeneratedAt).HasColumnName("pdf_generated_at");
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.RegionNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("region_name_snapshot");
            entity.Property(e => e.RejectedAt).HasColumnName("rejected_at");
            entity.Property(e => e.RejectedBy).HasColumnName("rejected_by");
            entity.Property(e => e.ReplacesContractId).HasColumnName("replaces_contract_id");
            entity.Property(e => e.SentToStaffAt).HasColumnName("sent_to_staff_at");
            entity.Property(e => e.StaffAcceptedAt).HasColumnName("staff_accepted_at");
            entity.Property(e => e.StaffRespondedAt).HasColumnName("staff_responded_at");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TerminatedAt).HasColumnName("terminated_at");
            entity.Property(e => e.TerminatedBy).HasColumnName("terminated_by");
            entity.Property(e => e.TerminationEffectiveAt).HasColumnName("termination_effective_at");
            entity.Property(e => e.TerminationReason).HasColumnName("termination_reason");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.EmploymentContractApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_approved_by_fkey");

            entity.HasOne(d => d.Branch).WithMany(p => p.EmploymentContracts)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_branch_id_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.EmploymentContractCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_cancelled_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.EmploymentContractCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_created_by_fkey");

            entity.HasOne(d => d.Region).WithMany(p => p.EmploymentContracts)
                .HasForeignKey(d => d.RegionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_region_id_fkey");

            entity.HasOne(d => d.RejectedByNavigation).WithMany(p => p.EmploymentContractRejectedByNavigations)
                .HasForeignKey(d => d.RejectedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_rejected_by_fkey");

            entity.HasOne(d => d.ReplacesContract).WithMany(p => p.InverseReplacesContract)
                .HasForeignKey(d => d.ReplacesContractId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_replaces_contract_id_fkey");

            entity.HasOne(d => d.TerminatedByNavigation).WithMany(p => p.EmploymentContractTerminatedByNavigations)
                .HasForeignKey(d => d.TerminatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_terminated_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.EmploymentContracts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("employment_contracts_user_id_fkey");
        });

        modelBuilder.Entity<FaceProfile>(entity =>
        {
            entity.HasKey(e => e.FaceProfileId).HasName("face_profiles_pkey");

            entity.ToTable("face_profiles");

            entity.HasIndex(e => e.RegistrationSessionId, "face_profiles_registration_session_id_key").IsUnique();

            entity.HasIndex(e => e.UserId, "uq_active_face_profile_user")
                .IsUnique()
                .HasFilter("((status)::text = 'ACTIVE'::text)");

            entity.HasIndex(e => e.UserId, "ux_face_profiles_one_active_per_user")
                .IsUnique()
                .HasFilter("((status)::text = 'ACTIVE'::text)");

            entity.Property(e => e.FaceProfileId).HasColumnName("face_profile_id");
            entity.Property(e => e.EmbeddingVersion)
                .HasMaxLength(50)
                .HasColumnName("embedding_version");
            entity.Property(e => e.FaceQualityScore)
                .HasPrecision(5, 4)
                .HasColumnName("face_quality_score");
            entity.Property(e => e.LivenessScore)
                .HasPrecision(5, 4)
                .HasColumnName("liveness_score");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("registered_at");
            entity.Property(e => e.RegistrationSessionId).HasColumnName("registration_session_id");
            entity.Property(e => e.RegistrationType)
                .HasMaxLength(20)
                .HasColumnName("registration_type");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.RegistrationSession).WithOne(p => p.FaceProfile)
                .HasForeignKey<FaceProfile>(d => d.RegistrationSessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("face_profiles_registration_session_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.FaceProfile)
                .HasForeignKey<FaceProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("face_profiles_user_id_fkey");
        });

        modelBuilder.Entity<FaceVerificationSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("face_verification_sessions_pkey");

            entity.ToTable("face_verification_sessions");

            entity.HasIndex(e => e.NonceHash, "face_verification_sessions_nonce_hash_key").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.ActionType, e.CreatedAt }, "ix_face_sessions_user_action_created").IsDescending(false, false, true);

            entity.Property(e => e.SessionId)
                .ValueGeneratedNever()
                .HasColumnName("session_id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(30)
                .HasColumnName("action_type");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.ChallengeSequence)
                .HasColumnType("jsonb")
                .HasColumnName("challenge_sequence");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.FaceMatchScore)
                .HasPrecision(5, 4)
                .HasColumnName("face_match_score");
            entity.Property(e => e.FaceMatchVerified)
                .HasDefaultValue(false)
                .HasColumnName("face_match_verified");
            entity.Property(e => e.LivenessScore)
                .HasPrecision(5, 4)
                .HasColumnName("liveness_score");
            entity.Property(e => e.LivenessVerified)
                .HasDefaultValue(false)
                .HasColumnName("liveness_verified");
            entity.Property(e => e.MatchedFaceProfileId).HasColumnName("matched_face_profile_id");
            entity.Property(e => e.NonceHash)
                .HasMaxLength(255)
                .HasColumnName("nonce_hash");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.MatchedFaceProfile).WithMany(p => p.FaceVerificationSessions)
                .HasForeignKey(d => d.MatchedFaceProfileId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("face_verification_sessions_matched_face_profile_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.FaceVerificationSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("face_verification_sessions_user_id_fkey");
        });

        modelBuilder.Entity<HolidayPayRule>(entity =>
        {
            entity.HasKey(e => e.HolidayPayRuleId).HasName("holiday_pay_rules_pkey");

            entity.ToTable("holiday_pay_rules");

            entity.HasIndex(e => e.EffectiveFrom, "idx_holiday_pay_rules_effective_from").IsDescending();

            entity.HasIndex(e => e.EffectiveFrom, "ux_holiday_pay_rules_effective_from_active")
                .IsUnique()
                .HasFilter("((status)::text <> 'INACTIVE'::text)");

            entity.Property(e => e.HolidayPayRuleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("holiday_pay_rule_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.PayMultiplier)
                .HasPrecision(5, 2)
                .HasColumnName("pay_multiplier");
            entity.Property(e => e.ReplacedRuleId).HasColumnName("replaced_rule_id");
            entity.Property(e => e.RuleName)
                .HasColumnType("character varying")
                .HasColumnName("rule_name");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.HolidayPayRules)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("holiday_pay_rules_created_by_fkey");

            entity.HasOne(d => d.ReplacedRule).WithMany(p => p.InverseReplacedRule)
                .HasForeignKey(d => d.ReplacedRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("holiday_pay_rules_replaced_rule_id_fkey");
        });

        modelBuilder.Entity<LeaveRequestAssignment>(entity =>
        {
            entity.HasKey(e => e.LeaveRequestAssignmentId).HasName("leave_request_assignments_pkey");

            entity.ToTable("leave_request_assignments");

            entity.HasIndex(e => e.AssignmentId, "ix_leave_request_assignments_assignment");

            entity.HasIndex(e => new { e.RequestId, e.AssignmentId }, "uq_leave_request_assignment").IsUnique();

            entity.Property(e => e.LeaveRequestAssignmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("leave_request_assignment_id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.RequestId).HasColumnName("request_id");

            entity.HasOne(d => d.Assignment).WithMany(p => p.LeaveRequestAssignments)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("leave_request_assignments_assignment_id_fkey");

            entity.HasOne(d => d.Request).WithMany(p => p.LeaveRequestAssignments)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("leave_request_assignments_request_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.Channels)
                .HasDefaultValueSql("ARRAY['InApp'::text]")
                .HasColumnType("character varying(20)[]")
                .HasColumnName("channels");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.TargetBranchId).HasColumnName("target_branch_id");
            entity.Property(e => e.TargetRegionId).HasColumnName("target_region_id");
            entity.Property(e => e.TargetRoles)
                .HasDefaultValueSql("(ARRAY[]::character varying[])::character varying(20)[]")
                .HasColumnType("character varying(20)[]")
                .HasColumnName("target_roles");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .HasDefaultValueSql("'All'::character varying")
                .HasColumnName("target_type");
            entity.Property(e => e.TerminationNoticeNo).HasColumnName("termination_notice_no");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Contract).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("notifications_contract_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notifications_created_by_fkey");

            entity.HasOne(d => d.TargetBranch).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TargetBranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notifications_target_branch_id_fkey");

            entity.HasOne(d => d.TargetRegion).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TargetRegionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notifications_target_region_id_fkey");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("orders_pkey");

            entity.ToTable("orders");

            entity.HasIndex(e => new { e.BranchId, e.OrderDate }, "ix_orders_branch_date").IsDescending(false, true);

            entity.HasIndex(e => e.OrderNumber, "orders_order_number_key").IsUnique();

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.BranchNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("branch_name_snapshot");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderNumber)
                .HasMaxLength(50)
                .HasColumnName("order_number");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'UNPAID'::character varying")
                .HasColumnName("payment_status");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.Subtotal)
                .HasPrecision(18, 2)
                .HasColumnName("subtotal");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Branch).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("orders_branch_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("orders_created_by_fkey");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("order_items_pkey");

            entity.ToTable("order_items");

            entity.HasIndex(e => e.OrderId, "ix_order_items_order");

            entity.Property(e => e.OrderItemId).HasColumnName("order_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(18, 2)
                .HasColumnName("discount_amount");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("product_name_snapshot");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2)
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("order_items_order_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("order_items_product_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payments_pkey");

            entity.ToTable("payments");

            entity.HasIndex(e => e.OrderId, "ix_payments_order_id");

            entity.HasIndex(e => e.RecordedBy, "ix_payments_recorded_by");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("fk_payments_order");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RecordedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payments_recorded_by");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("payrolls_pkey");

            entity.ToTable("payrolls");

            entity.HasIndex(e => new { e.PayrollPeriodId, e.Status }, "ix_payrolls_period_status");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "ix_payrolls_user_created").IsDescending(false, true);

            entity.HasIndex(e => new { e.UserId, e.PayrollPeriodId, e.VersionNo }, "uq_payroll_employee_period_version").IsUnique();

            entity.HasIndex(e => e.PreviousPayrollId, "uq_payroll_previous").IsUnique();

            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.AttendanceDeduction).HasColumnName("attendance_deduction");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.BranchNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("branch_name_snapshot");
            entity.Property(e => e.CalculatedAt).HasColumnName("calculated_at");
            entity.Property(e => e.CalculatedBy).HasColumnName("calculated_by");
            entity.Property(e => e.CarryForwardDeduction).HasColumnName("carry_forward_deduction");
            entity.Property(e => e.CorrectionReason).HasColumnName("correction_reason");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeferredDeductionAmount).HasColumnName("deferred_deduction_amount");
            entity.Property(e => e.EmployeeFullNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("employee_full_name_snapshot");
            entity.Property(e => e.EmployeePositionSnapshot)
                .HasMaxLength(100)
                .HasColumnName("employee_position_snapshot");
            entity.Property(e => e.GrossSalary).HasColumnName("gross_salary");
            entity.Property(e => e.HourlyRateSnapshot).HasColumnName("hourly_rate_snapshot");
            entity.Property(e => e.NetSalary).HasColumnName("net_salary");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentConfirmedAt).HasColumnName("payment_confirmed_at");
            entity.Property(e => e.PaymentConfirmedBy).HasColumnName("payment_confirmed_by");
            entity.Property(e => e.PaymentNote).HasColumnName("payment_note");
            entity.Property(e => e.PaymentReference)
                .HasColumnType("character varying")
                .HasColumnName("payment_reference");
            entity.Property(e => e.PayrollPeriodId).HasColumnName("payroll_period_id");
            entity.Property(e => e.PreviousPayrollId).HasColumnName("previous_payroll_id");
            entity.Property(e => e.ReplacedAt).HasColumnName("replaced_at");
            entity.Property(e => e.SalaryHistoryId).HasColumnName("salary_history_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'CALCULATED'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            entity.Property(e => e.TimesheetId).HasColumnName("timesheet_id");
            entity.Property(e => e.TotalDeduction).HasColumnName("total_deduction");
            entity.Property(e => e.TotalPenalty).HasColumnName("total_penalty");
            entity.Property(e => e.TotalReward).HasColumnName("total_reward");
            entity.Property(e => e.TotalWorkHours).HasColumnName("total_work_hours");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VersionNo)
                .HasDefaultValue(1)
                .HasColumnName("version_no");
            entity.Property(e => e.WorkInputSnapshot)
                .HasColumnType("jsonb")
                .HasColumnName("work_input_snapshot");
            entity.Property(e => e.WorkPay).HasColumnName("work_pay");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.PayrollApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("payrolls_approved_by_fkey");

            entity.HasOne(d => d.Branch).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payrolls_branch_id_fkey");

            entity.HasOne(d => d.CalculatedByNavigation).WithMany(p => p.PayrollCalculatedByNavigations)
                .HasForeignKey(d => d.CalculatedBy)
                .HasConstraintName("payrolls_calculated_by_fkey");

            entity.HasOne(d => d.PaymentConfirmedByNavigation).WithMany(p => p.PayrollPaymentConfirmedByNavigations)
                .HasForeignKey(d => d.PaymentConfirmedBy)
                .HasConstraintName("payrolls_payment_confirmed_by_fkey");

            entity.HasOne(d => d.PayrollPeriod).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payrolls_payroll_period_id_fkey");

            entity.HasOne(d => d.PreviousPayroll).WithOne(p => p.InversePreviousPayroll)
                .HasForeignKey<Payroll>(d => d.PreviousPayrollId)
                .HasConstraintName("payrolls_previous_payroll_id_fkey");

            entity.HasOne(d => d.SalaryHistory).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.SalaryHistoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payrolls_salary_history_id_fkey");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.PayrollSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .HasConstraintName("payrolls_submitted_by_fkey");

            entity.HasOne(d => d.Timesheet).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.TimesheetId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payrolls_timesheet_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.PayrollUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payrolls_user_id_fkey");
        });

        modelBuilder.Entity<PayrollCarryForwardApplication>(entity =>
        {
            entity.HasKey(e => e.ApplicationId).HasName("payroll_carry_forward_applications_pkey");

            entity.ToTable("payroll_carry_forward_applications");

            entity.HasIndex(e => e.PayrollId, "ix_carry_applications_payroll");

            entity.HasIndex(e => new { e.CarryForwardId, e.PayrollId }, "payroll_carry_forward_applicati_carry_forward_id_payroll_id_key").IsUnique();

            entity.Property(e => e.ApplicationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("application_id");
            entity.Property(e => e.AppliedAmount).HasColumnName("applied_amount");
            entity.Property(e => e.AppliedAt).HasColumnName("applied_at");
            entity.Property(e => e.CarryForwardId).HasColumnName("carry_forward_id");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");

            entity.HasOne(d => d.CarryForward).WithMany(p => p.PayrollCarryForwardApplications)
                .HasForeignKey(d => d.CarryForwardId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_carry_forward_applications_carry_forward_id_fkey");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollCarryForwardApplications)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_carry_forward_applications_payroll_id_fkey");
        });

        modelBuilder.Entity<PayrollCarryForwardDeduction>(entity =>
        {
            entity.HasKey(e => e.CarryForwardId).HasName("payroll_carry_forward_deductions_pkey");

            entity.ToTable("payroll_carry_forward_deductions");

            entity.HasIndex(e => e.UserId, "ix_carry_forward_available").HasFilter("((status)::text = ANY ((ARRAY['OPEN'::character varying, 'PARTIALLY_APPLIED'::character varying])::text[]))");

            entity.HasIndex(e => e.SourcePayrollDetailId, "payroll_carry_forward_deductions_source_payroll_detail_id_key").IsUnique();

            entity.Property(e => e.CarryForwardId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("carry_forward_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.OriginalAmount).HasColumnName("original_amount");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RemainingAmount).HasColumnName("remaining_amount");
            entity.Property(e => e.SourcePayrollDetailId).HasColumnName("source_payroll_detail_id");
            entity.Property(e => e.SourcePayrollId).HasColumnName("source_payroll_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.SourcePayrollDetail).WithOne(p => p.PayrollCarryForwardDeduction)
                .HasForeignKey<PayrollCarryForwardDeduction>(d => d.SourcePayrollDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_carry_forward_deductions_source_payroll_detail_id_fkey");

            entity.HasOne(d => d.SourcePayroll).WithMany(p => p.PayrollCarryForwardDeductions)
                .HasForeignKey(d => d.SourcePayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_carry_forward_deductions_source_payroll_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.PayrollCarryForwardDeductions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_carry_forward_deductions_user_id_fkey");
        });

        modelBuilder.Entity<PayrollCorrectionRequest>(entity =>
        {
            entity.HasKey(e => e.CorrectionRequestId).HasName("payroll_correction_requests_pkey");

            entity.ToTable("payroll_correction_requests");

            entity.HasIndex(e => e.FeedbackId, "ix_payroll_correction_feedback").HasFilter("(feedback_id IS NOT NULL)");

            entity.HasIndex(e => new { e.PayrollId, e.Status }, "ix_payroll_correction_payroll_status");

            entity.HasIndex(e => e.ResponseDeadlineAt, "ix_payroll_correction_requests_open_deadline").HasFilter("((status)::text = 'OPEN'::text)");

            entity.HasIndex(e => e.PreviousRequestId, "payroll_correction_requests_previous_request_id_key").IsUnique();

            entity.Property(e => e.CorrectionRequestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("correction_request_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.PreviousRequestId).HasColumnName("previous_request_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ResolvedPayrollId).HasColumnName("resolved_payroll_id");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by");
            entity.Property(e => e.ResponseAction)
                .HasColumnType("character varying")
                .HasColumnName("response_action");
            entity.Property(e => e.ResponseDeadlineAt).HasColumnName("response_deadline_at");
            entity.Property(e => e.ResponseNote).HasColumnName("response_note");
            entity.Property(e => e.ReviewNote).HasColumnName("review_note");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Feedback).WithMany(p => p.PayrollCorrectionRequests)
                .HasForeignKey(d => d.FeedbackId)
                .HasConstraintName("payroll_correction_requests_feedback_id_fkey");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollCorrectionRequestPayrolls)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_correction_requests_payroll_id_fkey");

            entity.HasOne(d => d.PreviousRequest).WithOne(p => p.InversePreviousRequest)
                .HasForeignKey<PayrollCorrectionRequest>(d => d.PreviousRequestId)
                .HasConstraintName("payroll_correction_requests_previous_request_id_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.PayrollCorrectionRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_correction_requests_requested_by_fkey");

            entity.HasOne(d => d.ResolvedPayroll).WithMany(p => p.PayrollCorrectionRequestResolvedPayrolls)
                .HasForeignKey(d => d.ResolvedPayrollId)
                .HasConstraintName("payroll_correction_requests_resolved_payroll_id_fkey");

            entity.HasOne(d => d.RespondedByNavigation).WithMany(p => p.PayrollCorrectionRequestRespondedByNavigations)
                .HasForeignKey(d => d.RespondedBy)
                .HasConstraintName("payroll_correction_requests_responded_by_fkey");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.PayrollCorrectionRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("payroll_correction_requests_reviewed_by_fkey");
        });

        modelBuilder.Entity<PayrollDetail>(entity =>
        {
            entity.HasKey(e => e.PayrollDetailId).HasName("payroll_details_pkey");

            entity.ToTable("payroll_details");

            entity.HasIndex(e => e.HolidayPayRuleId, "idx_payroll_details_holiday_pay_rule_id");

            entity.HasIndex(e => new { e.PayrollId, e.DisplayOrder }, "ix_payroll_details_payroll_display");

            entity.HasIndex(e => new { e.PayrollId, e.RewardPenaltyId }, "uq_payroll_detail_adjustment")
                .IsUnique()
                .HasFilter("(reward_penalty_id IS NOT NULL)");

            entity.HasIndex(e => new { e.PayrollId, e.AttendancePenaltyId }, "uq_payroll_detail_attendance_penalty")
                .IsUnique()
                .HasFilter("(attendance_penalty_id IS NOT NULL)");

            entity.Property(e => e.PayrollDetailId).HasColumnName("payroll_detail_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.AppliedAmount).HasColumnName("applied_amount");
            entity.Property(e => e.AttendancePenaltyId).HasColumnName("attendance_penalty_id");
            entity.Property(e => e.CarryForwardId).HasColumnName("carry_forward_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.HolidayPayRuleId).HasColumnName("holiday_pay_rule_id");
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .HasColumnName("item_code");
            entity.Property(e => e.Multiplier)
                .HasDefaultValueSql("1")
                .HasColumnName("multiplier");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.PublicHolidayId).HasColumnName("public_holiday_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.RewardPenaltyId).HasColumnName("reward_penalty_id");
            entity.Property(e => e.SalaryHistoryId).HasColumnName("salary_history_id");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
            entity.Property(e => e.WorkFrom).HasColumnName("work_from");
            entity.Property(e => e.WorkTo).HasColumnName("work_to");

            entity.HasOne(d => d.AttendancePenalty).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.AttendancePenaltyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_attendance_penalty_id_fkey");

            entity.HasOne(d => d.CarryForward).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.CarryForwardId)
                .HasConstraintName("payroll_details_carry_forward_id_fkey");

            entity.HasOne(d => d.HolidayPayRule).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.HolidayPayRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_holiday_pay_rule_id_fkey");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_payroll_id_fkey");

            entity.HasOne(d => d.PublicHoliday).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.PublicHolidayId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_public_holiday_id_fkey");

            entity.HasOne(d => d.RewardPenalty).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.RewardPenaltyId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_reward_penalty_id_fkey");

            entity.HasOne(d => d.SalaryHistory).WithMany(p => p.PayrollDetails)
                .HasForeignKey(d => d.SalaryHistoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_details_salary_history_id_fkey");
        });

        modelBuilder.Entity<PayrollFeedbackRequest>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("payroll_feedback_requests_pkey");

            entity.ToTable("payroll_feedback_requests");

            entity.Property(e => e.FeedbackId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("feedback_id");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ClosureReason)
                .HasColumnType("character varying")
                .HasColumnName("closure_reason");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.PreviousFeedbackId).HasColumnName("previous_feedback_id");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.ResolutionNote).HasColumnName("resolution_note");
            entity.Property(e => e.ResultNote).HasColumnName("result_note");
            entity.Property(e => e.ReviewDeadlineAt).HasColumnName("review_deadline_at");
            entity.Property(e => e.ReviewStatus)
                .HasColumnType("character varying")
                .HasColumnName("review_status");
            entity.Property(e => e.RoundNo).HasColumnName("round_no");
            entity.Property(e => e.SentBy).HasColumnName("sent_by");
            entity.Property(e => e.StaffRespondedAt).HasColumnName("staff_responded_at");
            entity.Property(e => e.Status)
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Employee).WithMany(p => p.PayrollFeedbackRequestEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_feedback_requests_employee_id_fkey");

            entity.HasOne(d => d.Payroll).WithMany(p => p.PayrollFeedbackRequests)
                .HasForeignKey(d => d.PayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payroll_feedback_requests_payroll_id_fkey");

            entity.HasOne(d => d.PreviousFeedback).WithMany(p => p.InversePreviousFeedback)
                .HasForeignKey(d => d.PreviousFeedbackId)
                .HasConstraintName("payroll_feedback_requests_previous_feedback_id_fkey");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.PayrollFeedbackRequestProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("payroll_feedback_requests_processed_by_fkey");

            entity.HasOne(d => d.SentByNavigation).WithMany(p => p.PayrollFeedbackRequestSentByNavigations)
                .HasForeignKey(d => d.SentBy)
                .HasConstraintName("payroll_feedback_requests_sent_by_fkey");
        });

        modelBuilder.Entity<PayrollPeriod>(entity =>
        {
            entity.HasKey(e => e.PayrollPeriodId).HasName("payroll_periods_pkey");

            entity.ToTable("payroll_periods");

            entity.HasIndex(e => new { e.BranchId, e.Status, e.StartDate, e.EndDate }, "ix_payroll_periods_branch_status_dates");

            entity.HasIndex(e => e.PeriodCode, "payroll_periods_period_code_key").IsUnique();

            entity.HasIndex(e => new { e.BranchId, e.StartDate, e.EndDate }, "uq_payroll_period_active_branch_dates")
                .IsUnique()
                .HasFilter("((status)::text <> 'CANCELLED'::text)");

            entity.HasIndex(e => e.ScheduleWeekId, "uq_payroll_period_schedule_week").IsUnique();

            entity.Property(e => e.PayrollPeriodId).HasColumnName("payroll_period_id");
            entity.Property(e => e.ApprovalDeadlineAt).HasColumnName("approval_deadline_at");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.BranchNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("branch_name_snapshot");
            entity.Property(e => e.CalculatedAt).HasColumnName("calculated_at");
            entity.Property(e => e.CalculatedBy).HasColumnName("calculated_by");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.ClosedBy).HasColumnName("closed_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.InitialFeedbackDeadlineAt).HasColumnName("initial_feedback_deadline_at");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PeriodCode)
                .HasMaxLength(50)
                .HasColumnName("period_code");
            entity.Property(e => e.PeriodName)
                .HasMaxLength(150)
                .HasColumnName("period_name");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.ScheduleWeekId).HasColumnName("schedule_week_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.PayrollPeriodApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_periods_approved_by_fkey");

            entity.HasOne(d => d.Branch).WithMany(p => p.PayrollPeriods)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_periods_branch_id_fkey");

            entity.HasOne(d => d.CalculatedByNavigation).WithMany(p => p.PayrollPeriodCalculatedByNavigations)
                .HasForeignKey(d => d.CalculatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_periods_calculated_by_fkey");

            entity.HasOne(d => d.ClosedByNavigation).WithMany(p => p.PayrollPeriodClosedByNavigations)
                .HasForeignKey(d => d.ClosedBy)
                .HasConstraintName("payroll_periods_closed_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PayrollPeriodCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_periods_created_by_fkey");

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.PayrollPeriodPublishedByNavigations)
                .HasForeignKey(d => d.PublishedBy)
                .HasConstraintName("payroll_periods_published_by_fkey");

            entity.HasOne(d => d.ScheduleWeek).WithOne(p => p.PayrollPeriod)
                .HasForeignKey<PayrollPeriod>(d => d.ScheduleWeekId)
                .HasConstraintName("payroll_periods_schedule_week_id_fkey");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.PayrollPeriodSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("payroll_periods_submitted_by_fkey");
        });

        modelBuilder.Entity<Payslip>(entity =>
        {
            entity.HasKey(e => e.PayslipId).HasName("payslips_pkey");

            entity.ToTable("payslips");

            entity.HasIndex(e => e.PayrollId, "payslips_payroll_id_key").IsUnique();

            entity.HasIndex(e => e.PreviousPayslipId, "payslips_previous_payslip_id_key").IsUnique();

            entity.Property(e => e.PayslipId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("payslip_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PayrollId).HasColumnName("payroll_id");
            entity.Property(e => e.PeriodCodeSnapshot)
                .HasColumnType("character varying")
                .HasColumnName("period_code_snapshot");
            entity.Property(e => e.PeriodEndSnapshot).HasColumnName("period_end_snapshot");
            entity.Property(e => e.PeriodNameSnapshot)
                .HasColumnType("character varying")
                .HasColumnName("period_name_snapshot");
            entity.Property(e => e.PeriodStartSnapshot).HasColumnName("period_start_snapshot");
            entity.Property(e => e.PreviousPayslipId).HasColumnName("previous_payslip_id");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ReplacedAt).HasColumnName("replaced_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PUBLISHED'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.VersionNo).HasColumnName("version_no");

            entity.HasOne(d => d.Payroll).WithOne(p => p.Payslip)
                .HasForeignKey<Payslip>(d => d.PayrollId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payslips_payroll_id_fkey");

            entity.HasOne(d => d.PreviousPayslip).WithOne(p => p.InversePreviousPayslip)
                .HasForeignKey<Payslip>(d => d.PreviousPayslipId)
                .HasConstraintName("payslips_previous_payslip_id_fkey");
        });

        modelBuilder.Entity<PayslipWorkDetail>(entity =>
        {
            entity.HasKey(e => e.PayslipWorkDetailId).HasName("payslip_work_details_pkey");

            entity.ToTable("payslip_work_details");

            entity.HasIndex(e => new { e.PayslipId, e.SourceAssignmentId }, "payslip_work_details_payslip_id_source_assignment_id_key").IsUnique();

            entity.Property(e => e.PayslipWorkDetailId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("payslip_work_detail_id");
            entity.Property(e => e.ActualWorkingMinutes).HasColumnName("actual_working_minutes");
            entity.Property(e => e.AttendanceStatusSnapshot)
                .HasColumnType("character varying")
                .HasColumnName("attendance_status_snapshot");
            entity.Property(e => e.BreakMinutesSnapshot)
                .HasDefaultValue(0)
                .HasColumnName("break_minutes_snapshot");
            entity.Property(e => e.ChargeableEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_early_leave_minutes");
            entity.Property(e => e.ChargeableLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_late_minutes");
            entity.Property(e => e.CheckInTime).HasColumnName("check_in_time");
            entity.Property(e => e.CheckOutTime).HasColumnName("check_out_time");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue(0)
                .HasColumnName("display_order");
            entity.Property(e => e.ExcusedEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_early_leave_minutes");
            entity.Property(e => e.ExcusedLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_late_minutes");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.PayableMinutes).HasColumnName("payable_minutes");
            entity.Property(e => e.PayslipId).HasColumnName("payslip_id");
            entity.Property(e => e.ScheduledEndAt).HasColumnName("scheduled_end_at");
            entity.Property(e => e.ScheduledStartAt).HasColumnName("scheduled_start_at");
            entity.Property(e => e.ShiftNameSnapshot)
                .HasColumnType("character varying")
                .HasColumnName("shift_name_snapshot");
            entity.Property(e => e.SourceAssignmentId).HasColumnName("source_assignment_id");
            entity.Property(e => e.SourceAttendanceId).HasColumnName("source_attendance_id");
            entity.Property(e => e.TotalEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_early_leave_minutes");
            entity.Property(e => e.TotalLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_late_minutes");
            entity.Property(e => e.WorkDate).HasColumnName("work_date");

            entity.HasOne(d => d.Payslip).WithMany(p => p.PayslipWorkDetails)
                .HasForeignKey(d => d.PayslipId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payslip_work_details_payslip_id_fkey");

            entity.HasOne(d => d.SourceAssignment).WithMany(p => p.PayslipWorkDetails)
                .HasForeignKey(d => d.SourceAssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("payslip_work_details_source_assignment_id_fkey");

            entity.HasOne(d => d.SourceAttendance).WithMany(p => p.PayslipWorkDetails)
                .HasForeignKey(d => d.SourceAttendanceId)
                .HasConstraintName("payslip_work_details_source_attendance_id_fkey");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("products_pkey");

            entity.ToTable("products");

            entity.HasIndex(e => e.BranchId, "ix_products_branch_id");

            entity.HasIndex(e => new { e.CategoryId, e.Status }, "ix_products_category_status");

            entity.HasIndex(e => new { e.BranchId, e.ProductCode }, "uq_products_branch_code").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.Price)
                .HasPrecision(18, 2)
                .HasColumnName("price");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(50)
                .HasColumnName("product_code");
            entity.Property(e => e.ProductName)
                .HasMaxLength(150)
                .HasColumnName("product_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Branch).WithMany(p => p.Products)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("products_branch_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("products_category_id_fkey");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("product_categories_pkey");

            entity.ToTable("product_categories");

            entity.HasIndex(e => e.CategoryName, "product_categories_category_name_key").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(150)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<PublicHolidayConfig>(entity =>
        {
            entity.HasKey(e => e.PublicHolidayId).HasName("public_holiday_configs_pkey");

            entity.ToTable("public_holiday_configs");

            entity.HasIndex(e => e.HolidayDate, "public_holiday_configs_holiday_date_key").IsUnique();

            entity.Property(e => e.PublicHolidayId).HasColumnName("public_holiday_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.HolidayDate).HasColumnName("holiday_date");
            entity.Property(e => e.HolidayName)
                .HasMaxLength(150)
                .HasColumnName("holiday_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PublicHolidayConfigs)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("public_holiday_configs_created_by_fkey");
        });

        modelBuilder.Entity<Region>(entity =>
        {
            entity.HasKey(e => e.RegionId).HasName("regions_pkey");

            entity.ToTable("regions");

            entity.HasIndex(e => e.RegionalMinimumWageId, "idx_regions_regional_minimum_wage_id");

            entity.HasIndex(e => e.RegionCode, "regions_region_code_key").IsUnique();

            entity.HasIndex(e => e.RegionName, "regions_region_name_key").IsUnique();

            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RegionCode)
                .HasMaxLength(20)
                .HasColumnName("region_code");
            entity.Property(e => e.RegionName)
                .HasMaxLength(150)
                .HasColumnName("region_name");
            entity.Property(e => e.RegionalMinimumWageId).HasColumnName("regional_minimum_wage_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.RegionalMinimumWage).WithMany(p => p.Regions)
                .HasForeignKey(d => d.RegionalMinimumWageId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("regions_regional_minimum_wage_id_fkey");
        });

        modelBuilder.Entity<RegionalMinimumWage>(entity =>
        {
            entity.HasKey(e => e.RegionalMinimumWageId).HasName("regional_minimum_wages_pkey");

            entity.ToTable("regional_minimum_wages");

            entity.Property(e => e.RegionalMinimumWageId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("regional_minimum_wage_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.MinimumHourlyRate).HasColumnName("minimum_hourly_rate");
            entity.Property(e => e.ReplacedWageId).HasColumnName("replaced_wage_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.WageName)
                .HasColumnType("character varying")
                .HasColumnName("wage_name");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RegionalMinimumWages)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("regional_minimum_wages_created_by_fkey");

            entity.HasOne(d => d.ReplacedWage).WithMany(p => p.InverseReplacedWage)
                .HasForeignKey(d => d.ReplacedWageId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("regional_minimum_wages_replaced_wage_id_fkey");
        });

        modelBuilder.Entity<RequestLimitRule>(entity =>
        {
            entity.HasKey(e => e.RequestLimitRuleId).HasName("request_limit_rules_pkey");

            entity.ToTable("request_limit_rules");

            entity.HasIndex(e => new { e.Status, e.EffectiveFrom }, "ix_request_limit_rules_due").HasFilter("((status)::text = 'SCHEDULED'::text)");

            entity.HasIndex(e => new { e.BranchId, e.RequestType }, "uq_active_request_limit_rule")
                .IsUnique()
                .HasFilter("((status)::text = 'ACTIVE'::text)");

            entity.Property(e => e.RequestLimitRuleId).HasColumnName("request_limit_rule_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.MaxRequestsPerMonth).HasColumnName("max_requests_per_month");
            entity.Property(e => e.MinimumAdvanceDays).HasColumnName("minimum_advance_days");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.RequestType)
                .HasMaxLength(30)
                .HasColumnName("request_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SubmissionDeadlineDayOfWeek).HasColumnName("submission_deadline_day_of_week");
            entity.Property(e => e.SupersedesRuleId).HasColumnName("supersedes_rule_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Branch).WithMany(p => p.RequestLimitRules)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("request_limit_rules_branch_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RequestLimitRuleCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("request_limit_rules_created_by_fkey");

            entity.HasOne(d => d.PublishedByNavigation).WithMany(p => p.RequestLimitRulePublishedByNavigations)
                .HasForeignKey(d => d.PublishedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("request_limit_rules_published_by_fkey");

            entity.HasOne(d => d.SupersedesRule).WithMany(p => p.InverseSupersedesRule)
                .HasForeignKey(d => d.SupersedesRuleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("request_limit_rules_supersedes_rule_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "roles_role_name_key").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<SalaryChangeRequest>(entity =>
        {
            entity.HasKey(e => e.SalaryChangeRequestId).HasName("salary_change_requests_pkey");

            entity.ToTable("salary_change_requests");

            entity.HasIndex(e => new { e.BranchId, e.Status, e.CreatedAt }, "ix_salary_change_branch_status").IsDescending(false, false, true);

            entity.HasIndex(e => e.EmployeeRequestId, "ix_salary_change_employee_request").HasFilter("(employee_request_id IS NOT NULL)");

            entity.HasIndex(e => e.PreviousRequestId, "ix_salary_change_previous_request").HasFilter("(previous_request_id IS NOT NULL)");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "ix_salary_change_user_history").IsDescending(false, true);

            entity.HasIndex(e => e.UserId, "uq_salary_change_pending_user")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.SalaryChangeRequestId).HasColumnName("salary_change_request_id");
            entity.Property(e => e.ApprovedEffectiveFrom).HasColumnName("approved_effective_from");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentSalaryHistoryId).HasColumnName("current_salary_history_id");
            entity.Property(e => e.EffectiveDateChangeReason).HasColumnName("effective_date_change_reason");
            entity.Property(e => e.EmployeeRequestId).HasColumnName("employee_request_id");
            entity.Property(e => e.PreviousRequestId).HasColumnName("previous_request_id");
            entity.Property(e => e.ProposedEffectiveFrom).HasColumnName("proposed_effective_from");
            entity.Property(e => e.ProposedHourlyRate).HasColumnName("proposed_hourly_rate");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ReviewNote).HasColumnName("review_note");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Branch).WithMany(p => p.SalaryChangeRequests)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("salary_change_requests_branch_id_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.SalaryChangeRequestCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .HasConstraintName("salary_change_requests_cancelled_by_fkey");

            entity.HasOne(d => d.CurrentSalaryHistory).WithMany(p => p.SalaryChangeRequests)
                .HasForeignKey(d => d.CurrentSalaryHistoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("salary_change_requests_current_salary_history_id_fkey");

            entity.HasOne(d => d.EmployeeRequest).WithMany(p => p.SalaryChangeRequests)
                .HasForeignKey(d => d.EmployeeRequestId)
                .HasConstraintName("salary_change_requests_employee_request_id_fkey");

            entity.HasOne(d => d.PreviousRequest).WithMany(p => p.InversePreviousRequest)
                .HasForeignKey(d => d.PreviousRequestId)
                .HasConstraintName("salary_change_requests_previous_request_id_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.SalaryChangeRequestRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("salary_change_requests_requested_by_fkey");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.SalaryChangeRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("salary_change_requests_reviewed_by_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.SalaryChangeRequestUser)
                .HasForeignKey<SalaryChangeRequest>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("salary_change_requests_user_id_fkey");
        });

        modelBuilder.Entity<SalaryIncreaseRequestDetail>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("salary_increase_request_details_pkey");

            entity.ToTable("salary_increase_request_details");

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("request_id");
            entity.Property(e => e.CurrentContractId).HasColumnName("current_contract_id");
            entity.Property(e => e.CurrentHourlyRate)
                .HasPrecision(18, 2)
                .HasColumnName("current_hourly_rate");
            entity.Property(e => e.ProposedHourlyRate)
                .HasPrecision(18, 2)
                .HasColumnName("proposed_hourly_rate");

            entity.HasOne(d => d.CurrentContract).WithMany(p => p.SalaryIncreaseRequestDetails)
                .HasForeignKey(d => d.CurrentContractId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salary_increase_request_details_current_contract_id_fkey");

            entity.HasOne(d => d.Request).WithOne(p => p.SalaryIncreaseRequestDetail)
                .HasForeignKey<SalaryIncreaseRequestDetail>(d => d.RequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("salary_increase_request_details_request_id_fkey");
        });

        modelBuilder.Entity<ShiftAssignment>(entity =>
        {
            entity.HasKey(e => e.AssignmentId).HasName("shift_assignments_pkey");

            entity.ToTable("shift_assignments");

            entity.HasIndex(e => new { e.ShiftRequirementId, e.Status }, "ix_shift_assignments_requirement_status");

            entity.HasIndex(e => new { e.ShiftId, e.Status }, "ix_shift_assignments_shift_status");

            entity.HasIndex(e => new { e.UserId, e.Status }, "ix_shift_assignments_user_status");

            entity.HasIndex(e => new { e.ShiftId, e.UserId }, "uq_shift_assignment").IsUnique();

            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.AssignmentSource)
                .HasMaxLength(30)
                .HasColumnName("assignment_source");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(500)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.ShiftRequirementId).HasColumnName("shift_requirement_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ASSIGNED'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.ShiftAssignmentAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_assignments_assigned_by_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.ShiftAssignmentCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_assignments_cancelled_by_fkey");

            entity.HasOne(d => d.Shift).WithMany(p => p.ShiftAssignments)
                .HasForeignKey(d => d.ShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_assignments_shift_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ShiftAssignmentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_assignments_user_id_fkey");

            entity.HasOne(d => d.ShiftEmployeeRequirement).WithMany(p => p.ShiftAssignments)
                .HasPrincipalKey(p => new { p.ShiftRequirementId, p.ShiftId })
                .HasForeignKey(d => new { d.ShiftRequirementId, d.ShiftId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_shift_assignments_requirement_shift");
        });

        modelBuilder.Entity<ShiftEmployeeRequirement>(entity =>
        {
            entity.HasKey(e => e.ShiftRequirementId).HasName("shift_employee_requirements_pkey");

            entity.ToTable("shift_employee_requirements");

            entity.HasIndex(e => e.EmployeeTypeId, "ix_shift_employee_requirements_employee_type");

            entity.HasIndex(e => e.ShiftId, "ix_shift_employee_requirements_shift");

            entity.HasIndex(e => new { e.ShiftRequirementId, e.ShiftId }, "uq_shift_employee_requirements_requirement_shift").IsUnique();

            entity.HasIndex(e => new { e.ShiftId, e.EmployeeTypeId }, "uq_shift_employee_requirements_shift_type").IsUnique();

            entity.Property(e => e.ShiftRequirementId).HasColumnName("shift_requirement_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EmployeeTypeId).HasColumnName("employee_type_id");
            entity.Property(e => e.RequiredCount).HasColumnName("required_count");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.EmployeeType).WithMany(p => p.ShiftEmployeeRequirements)
                .HasForeignKey(d => d.EmployeeTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_shift_employee_requirements_employee_type");

            entity.HasOne(d => d.Shift).WithMany(p => p.ShiftEmployeeRequirements)
                .HasForeignKey(d => d.ShiftId)
                .HasConstraintName("fk_shift_employee_requirements_shift");
        });

        modelBuilder.Entity<ShiftSwapRequest>(entity =>
        {
            entity.HasKey(e => e.SwapRequestId).HasName("shift_swap_requests_pkey");

            entity.ToTable("shift_swap_requests");

            entity.Property(e => e.SwapRequestId).HasColumnName("swap_request_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RequesterId).HasColumnName("requester_id");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.RespondedBy).HasColumnName("responded_by");
            entity.Property(e => e.ResponseNote)
                .HasMaxLength(500)
                .HasColumnName("response_note");
            entity.Property(e => e.SourceAssignmentId).HasColumnName("source_assignment_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TargetAssignmentId).HasColumnName("target_assignment_id");
            entity.Property(e => e.TargetUserId).HasColumnName("target_user_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Requester).WithMany(p => p.ShiftSwapRequestRequesters)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_swap_requests_requester_id_fkey");

            entity.HasOne(d => d.RespondedByNavigation).WithMany(p => p.ShiftSwapRequestRespondedByNavigations)
                .HasForeignKey(d => d.RespondedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_swap_requests_responded_by_fkey");

            entity.HasOne(d => d.SourceAssignment).WithMany(p => p.ShiftSwapRequestSourceAssignments)
                .HasForeignKey(d => d.SourceAssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_swap_requests_source_assignment_id_fkey");

            entity.HasOne(d => d.TargetAssignment).WithMany(p => p.ShiftSwapRequestTargetAssignments)
                .HasForeignKey(d => d.TargetAssignmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_swap_requests_target_assignment_id_fkey");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.ShiftSwapRequestTargetUsers)
                .HasForeignKey(d => d.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("shift_swap_requests_target_user_id_fkey");
        });

        modelBuilder.Entity<ShiftTemplate>(entity =>
        {
            entity.HasKey(e => e.ShiftTemplateId).HasName("shift_templates_pkey");

            entity.ToTable("shift_templates");

            entity.HasIndex(e => e.ShiftName, "shift_templates_shift_name_key").IsUnique();

            entity.Property(e => e.ShiftTemplateId).HasColumnName("shift_template_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.ShiftName)
                .HasMaxLength(150)
                .HasColumnName("shift_name");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<StaffSalaryHistory>(entity =>
        {
            entity.HasKey(e => e.SalaryHistoryId).HasName("staff_salary_history_pkey");

            entity.ToTable("staff_salary_history");

            entity.HasIndex(e => new { e.Status, e.EffectiveFrom }, "ix_salary_history_due").HasFilter("((status)::text = 'SCHEDULED'::text)");

            entity.HasIndex(e => e.UserId, "uq_active_salary_history_user")
                .IsUnique()
                .HasFilter("((status)::text = 'ACTIVE'::text)");

            entity.HasIndex(e => e.SalaryChangeRequestId, "uq_salary_history_change_request").IsUnique();

            entity.Property(e => e.SalaryHistoryId).HasColumnName("salary_history_id");
            entity.Property(e => e.ChangeType)
                .HasMaxLength(20)
                .HasColumnName("change_type");
            entity.Property(e => e.ContractId).HasColumnName("contract_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.HourlyRate)
                .HasPrecision(18, 2)
                .HasColumnName("hourly_rate");
            entity.Property(e => e.PreviousHourlyRate)
                .HasPrecision(18, 2)
                .HasColumnName("previous_hourly_rate");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.SalaryChangeRequestId)
                .HasComment("UNIQUE: mot proposal toi da mot history. Ban ghi moi dung lien ket nay; salary_increase_request_id cu de NULL, giu cho legacy.")
                .HasColumnName("salary_change_request_id");
            entity.Property(e => e.SalaryIncreaseRequestId).HasColumnName("salary_increase_request_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Contract).WithMany(p => p.StaffSalaryHistories)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("staff_salary_history_contract_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StaffSalaryHistoryCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("staff_salary_history_created_by_fkey");

            entity.HasOne(d => d.SalaryChangeRequest).WithOne(p => p.StaffSalaryHistory)
                .HasForeignKey<StaffSalaryHistory>(d => d.SalaryChangeRequestId)
                .HasConstraintName("staff_salary_history_salary_change_request_id_fkey");

            entity.HasOne(d => d.SalaryIncreaseRequest).WithMany(p => p.StaffSalaryHistories)
                .HasForeignKey(d => d.SalaryIncreaseRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("staff_salary_history_salary_increase_request_id_fkey");

            entity.HasOne(d => d.User).WithOne(p => p.StaffSalaryHistoryUser)
                .HasForeignKey<StaffSalaryHistory>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("staff_salary_history_user_id_fkey");
        });

        modelBuilder.Entity<TimeAdjustmentRequestDetail>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("time_adjustment_request_details_pkey");

            entity.ToTable("time_adjustment_request_details");

            entity.Property(e => e.RequestId)
                .ValueGeneratedNever()
                .HasColumnName("request_id");
            entity.Property(e => e.AssignmentId).HasColumnName("assignment_id");
            entity.Property(e => e.RequestedTime).HasColumnName("requested_time");

            entity.HasOne(d => d.Assignment).WithMany(p => p.TimeAdjustmentRequestDetails)
                .HasForeignKey(d => d.AssignmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("time_adjustment_request_details_assignment_id_fkey");

            entity.HasOne(d => d.Request).WithOne(p => p.TimeAdjustmentRequestDetail)
                .HasForeignKey<TimeAdjustmentRequestDetail>(d => d.RequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("time_adjustment_request_details_request_id_fkey");
        });

        modelBuilder.Entity<Timesheet>(entity =>
        {
            entity.HasKey(e => e.TimesheetId).HasName("timesheets_pkey");

            entity.ToTable("timesheets");

            entity.HasIndex(e => new { e.PayrollPeriodId, e.Status }, "ix_timesheets_period_status");

            entity.HasIndex(e => new { e.UserId, e.PayrollPeriodId }, "uq_timesheet_user_period").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.PayrollPeriodId }, "uq_timesheets_user_payroll_period").IsUnique();

            entity.Property(e => e.TimesheetId).HasColumnName("timesheet_id");
            entity.Property(e => e.AbsentShiftCount)
                .HasDefaultValue(0)
                .HasColumnName("absent_shift_count");
            entity.Property(e => e.ChargeableEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_early_leave_minutes");
            entity.Property(e => e.ChargeableLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("chargeable_late_minutes");
            entity.Property(e => e.CompletedShiftCount)
                .HasDefaultValue(0)
                .HasColumnName("completed_shift_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExcusedEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_early_leave_minutes");
            entity.Property(e => e.ExcusedLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("excused_late_minutes");
            entity.Property(e => e.GeneratedAt).HasColumnName("generated_at");
            entity.Property(e => e.IncompleteShiftCount)
                .HasDefaultValue(0)
                .HasColumnName("incomplete_shift_count");
            entity.Property(e => e.LockedAt).HasColumnName("locked_at");
            entity.Property(e => e.LockedBy).HasColumnName("locked_by");
            entity.Property(e => e.PayrollPeriodId).HasColumnName("payroll_period_id");
            entity.Property(e => e.ReviewDeadline).HasColumnName("review_deadline");
            entity.Property(e => e.ReviewNote)
                .HasMaxLength(500)
                .HasColumnName("review_note");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.StaffReviewNote)
                .HasMaxLength(500)
                .HasColumnName("staff_review_note");
            entity.Property(e => e.StaffReviewStatus)
                .HasMaxLength(30)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("staff_review_status");
            entity.Property(e => e.StaffReviewedAt).HasColumnName("staff_reviewed_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("''::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalAssignedShifts)
                .HasDefaultValue(0)
                .HasColumnName("total_assigned_shifts");
            entity.Property(e => e.TotalEarlyLeaveMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_early_leave_minutes");
            entity.Property(e => e.TotalLateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_late_minutes");
            entity.Property(e => e.TotalScheduledMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_scheduled_minutes");
            entity.Property(e => e.TotalWorkMinutes)
                .HasDefaultValue(0)
                .HasColumnName("total_work_minutes");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.LockedByNavigation).WithMany(p => p.TimesheetLockedByNavigations)
                .HasForeignKey(d => d.LockedBy)
                .HasConstraintName("timesheets_locked_by_fkey");

            entity.HasOne(d => d.PayrollPeriod).WithMany(p => p.Timesheets)
                .HasForeignKey(d => d.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("timesheets_payroll_period_id_fkey");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.TimesheetReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("timesheets_reviewed_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TimesheetUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("timesheets_user_id_fkey");
        });

        modelBuilder.Entity<TimesheetAdjustmentRequest>(entity =>
        {
            entity.HasKey(e => e.AdjustmentRequestId).HasName("timesheet_adjustment_requests_pkey");

            entity.ToTable("timesheet_adjustment_requests");

            entity.HasIndex(e => e.AttendanceId, "ix_timesheet_adj_attendance_id");

            entity.HasIndex(e => e.Status, "ix_timesheet_adj_status");

            entity.HasIndex(e => e.TimesheetId, "ix_timesheet_adj_timesheet_id");

            entity.HasIndex(e => e.AttendanceId, "uq_attendance_pending_adj")
                .IsUnique()
                .HasFilter("((status)::text = 'PENDING'::text)");

            entity.Property(e => e.AdjustmentRequestId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("adjustment_request_id");
            entity.Property(e => e.AdjustCheckIn)
                .HasDefaultValue(false)
                .HasColumnName("adjust_check_in");
            entity.Property(e => e.AdjustCheckOut)
                .HasDefaultValue(false)
                .HasColumnName("adjust_check_out");
            entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CurrentCheckInSnapshot).HasColumnName("current_check_in_snapshot");
            entity.Property(e => e.CurrentCheckOutSnapshot).HasColumnName("current_check_out_snapshot");
            entity.Property(e => e.ManagerNote).HasColumnName("manager_note");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.RequestedCheckInTime).HasColumnName("requested_check_in_time");
            entity.Property(e => e.RequestedCheckOutTime).HasColumnName("requested_check_out_time");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TimesheetId).HasColumnName("timesheet_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Attendance).WithOne(p => p.TimesheetAdjustmentRequest)
                .HasForeignKey<TimesheetAdjustmentRequest>(d => d.AttendanceId)
                .HasConstraintName("fk_timesheet_adj_attendance");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.TimesheetAdjustmentRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("fk_timesheet_adj_reviewer");

            entity.HasOne(d => d.Timesheet).WithMany(p => p.TimesheetAdjustmentRequests)
                .HasForeignKey(d => d.TimesheetId)
                .HasConstraintName("fk_timesheet_adj_timesheet");

            entity.HasOne(d => d.User).WithMany(p => p.TimesheetAdjustmentRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_timesheet_adj_user");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => new { e.BranchId, e.Status }, "ix_users_branch_status");

            entity.HasIndex(e => new { e.RoleId, e.Status }, "ix_users_role_status");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.IdentityNo, "users_identity_no_key").IsUnique();

            entity.HasIndex(e => e.Phone, "users_phone_key").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .HasColumnName("email");
            entity.Property(e => e.EmployeeTypeId).HasColumnName("employee_type_id");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Gender)
                .HasMaxLength(10)
                .HasColumnName("gender");
            entity.Property(e => e.HasPassword)
                .HasDefaultValue(true)
                .HasColumnName("has_password");
            entity.Property(e => e.IdentityNo)
                .HasMaxLength(30)
                .HasColumnName("identity_no");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.PreviousBranchId).HasColumnName("previous_branch_id");
            entity.Property(e => e.PreviousStatus)
                .HasMaxLength(30)
                .HasColumnName("previous_status");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Branch).WithMany(p => p.UserBranches)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_branch_id_fkey");

            entity.HasOne(d => d.EmployeeType).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmployeeTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_employee_type_id_fkey");

            entity.HasOne(d => d.PreviousBranch).WithMany(p => p.UserPreviousBranches)
                .HasForeignKey(d => d.PreviousBranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_previous_branch_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("users_role_id_fkey");
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(e => e.UserNotificationId).HasName("user_notifications_pkey");

            entity.ToTable("user_notifications");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "ix_user_notifications_unread")
                .IsDescending(false, true)
                .HasFilter("((is_read = false) AND (is_deleted = false))");

            entity.HasIndex(e => new { e.NotificationId, e.UserId }, "uq_user_notification").IsUnique();

            entity.Property(e => e.UserNotificationId).HasColumnName("user_notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Notification).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("user_notifications_notification_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("user_notifications_user_id_fkey");
        });

        modelBuilder.Entity<WorkScheduleWeek>(entity =>
        {
            entity.HasKey(e => e.ScheduleWeekId).HasName("work_schedule_weeks_pkey");

            entity.ToTable("work_schedule_weeks");

            entity.HasIndex(e => new { e.BranchId, e.Status, e.WeekStartDate }, "ix_schedule_weeks_branch_status");

            entity.HasIndex(e => new { e.BranchId, e.WeekStartDate }, "uq_schedule_week_branch_start").IsUnique();

            entity.Property(e => e.ScheduleWeekId).HasColumnName("schedule_week_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(500)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.OpenedAt).HasColumnName("opened_at");
            entity.Property(e => e.RegistrationClosedAt).HasColumnName("registration_closed_at");
            entity.Property(e => e.RegistrationDeadline).HasColumnName("registration_deadline");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.WeekEndDate).HasColumnName("week_end_date");
            entity.Property(e => e.WeekStartDate).HasColumnName("week_start_date");

            entity.HasOne(d => d.Branch).WithMany(p => p.WorkScheduleWeeks)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_schedule_weeks_branch_id_fkey");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.WorkScheduleWeekCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_schedule_weeks_cancelled_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.WorkScheduleWeekCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_schedule_weeks_created_by_fkey");
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("work_shifts_pkey");

            entity.ToTable("work_shifts");

            entity.HasIndex(e => new { e.ScheduleWeekId, e.Status, e.ScheduledStartAt }, "ix_work_shifts_week_status");

            entity.Property(e => e.ShiftId).HasColumnName("shift_id");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(500)
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.RequiredEmployees).HasColumnName("required_employees");
            entity.Property(e => e.ScheduleWeekId).HasColumnName("schedule_week_id");
            entity.Property(e => e.ScheduledEndAt).HasColumnName("scheduled_end_at");
            entity.Property(e => e.ScheduledStartAt).HasColumnName("scheduled_start_at");
            entity.Property(e => e.ShiftNameSnapshot)
                .HasMaxLength(150)
                .HasColumnName("shift_name_snapshot");
            entity.Property(e => e.ShiftTemplateId).HasColumnName("shift_template_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CancelledByNavigation).WithMany(p => p.WorkShiftCancelledByNavigations)
                .HasForeignKey(d => d.CancelledBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_shifts_cancelled_by_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.WorkShiftCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_shifts_created_by_fkey");

            entity.HasOne(d => d.ScheduleWeek).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.ScheduleWeekId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_shifts_schedule_week_id_fkey");

            entity.HasOne(d => d.ShiftTemplate).WithMany(p => p.WorkShifts)
                .HasForeignKey(d => d.ShiftTemplateId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("work_shifts_shift_template_id_fkey");
        });

        modelBuilder.Entity<WorkingTimeRule>(entity =>
        {
            entity.HasKey(e => e.WorkingTimeRuleId).HasName("working_time_rules_pkey");

            entity.ToTable("working_time_rules");

            entity.Property(e => e.WorkingTimeRuleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("working_time_rule_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.MaxDailyMinutes).HasColumnName("max_daily_minutes");
            entity.Property(e => e.MaxWeeklyMinutes).HasColumnName("max_weekly_minutes");
            entity.Property(e => e.RuleName)
                .HasColumnType("character varying")
                .HasColumnName("rule_name");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.WorkingTimeRules)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("working_time_rules_created_by_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
