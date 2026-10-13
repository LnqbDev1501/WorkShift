using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public int RoleId { get; set; }

    public int? BranchId { get; set; }

    public int? EmployeeTypeId { get; set; }

    public string FullName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string AvatarUrl { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string IdentityNo { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? PreviousStatus { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? PreviousBranchId { get; set; }

    public bool HasPassword { get; set; }

    public virtual ICollection<AttendanceAdjustmentHistory> AttendanceAdjustmentHistories { get; set; } = new List<AttendanceAdjustmentHistory>();

    public virtual ICollection<AttendancePenaltyRule> AttendancePenaltyRuleCancelledByNavigations { get; set; } = new List<AttendancePenaltyRule>();

    public virtual ICollection<AttendancePenaltyRule> AttendancePenaltyRuleCreatedByNavigations { get; set; } = new List<AttendancePenaltyRule>();

    public virtual ICollection<AttendancePenaltyRule> AttendancePenaltyRulePublishedByNavigations { get; set; } = new List<AttendancePenaltyRule>();

    public virtual ICollection<AttendanceRule> AttendanceRuleCancelledByNavigations { get; set; } = new List<AttendanceRule>();

    public virtual ICollection<AttendanceRule> AttendanceRuleCreatedByNavigations { get; set; } = new List<AttendanceRule>();

    public virtual ICollection<AttendanceRule> AttendanceRulePublishedByNavigations { get; set; } = new List<AttendanceRule>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual Branch? Branch { get; set; }

    public virtual ICollection<ChecklistResult> ChecklistResults { get; set; } = new List<ChecklistResult>();

    public virtual ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();

    public virtual ICollection<ContractChangeRequest> ContractChangeRequestRequestedByNavigations { get; set; } = new List<ContractChangeRequest>();

    public virtual ICollection<ContractChangeRequest> ContractChangeRequestReviewedByNavigations { get; set; } = new List<ContractChangeRequest>();

    public virtual ICollection<EmployeeRequest> EmployeeRequestReviewedByNavigations { get; set; } = new List<EmployeeRequest>();

    public virtual ICollection<EmployeeRequest> EmployeeRequestUsers { get; set; } = new List<EmployeeRequest>();

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenaltyApprovedByNavigations { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenaltyCreatedByNavigations { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenaltyDeletedByNavigations { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenaltyRejectedByNavigations { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual ICollection<EmployeeRewardsPenalty> EmployeeRewardsPenaltyUsers { get; set; } = new List<EmployeeRewardsPenalty>();

    public virtual EmployeeType? EmployeeType { get; set; }

    public virtual ICollection<EmploymentContract> EmploymentContractApprovedByNavigations { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<EmploymentContract> EmploymentContractCancelledByNavigations { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<EmploymentContract> EmploymentContractCreatedByNavigations { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<EmploymentContract> EmploymentContractRejectedByNavigations { get; set; } = new List<EmploymentContract>();

    public virtual ICollection<EmploymentContract> EmploymentContractTerminatedByNavigations { get; set; } = new List<EmploymentContract>();

    public virtual EmploymentContract? EmploymentContractUser { get; set; }

    public virtual FaceProfile? FaceProfile { get; set; }

    public virtual ICollection<FaceVerificationSession> FaceVerificationSessions { get; set; } = new List<FaceVerificationSession>();

    public virtual ICollection<HolidayPayRule> HolidayPayRules { get; set; } = new List<HolidayPayRule>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payroll> PayrollApprovedByNavigations { get; set; } = new List<Payroll>();

    public virtual ICollection<Payroll> PayrollCalculatedByNavigations { get; set; } = new List<Payroll>();

    public virtual ICollection<PayrollCarryForwardDeduction> PayrollCarryForwardDeductions { get; set; } = new List<PayrollCarryForwardDeduction>();

    public virtual ICollection<PayrollFeedbackRequest> PayrollFeedbackRequestEmployees { get; set; } = new List<PayrollFeedbackRequest>();

    public virtual ICollection<PayrollFeedbackRequest> PayrollFeedbackRequestProcessedByNavigations { get; set; } = new List<PayrollFeedbackRequest>();

    public virtual ICollection<Payroll> PayrollPaymentConfirmedByNavigations { get; set; } = new List<Payroll>();

    public virtual ICollection<PayrollPeriod> PayrollPeriodApprovedByNavigations { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPeriod> PayrollPeriodCalculatedByNavigations { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPeriod> PayrollPeriodClosedByNavigations { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPeriod> PayrollPeriodCreatedByNavigations { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<PayrollPeriod> PayrollPeriodSubmittedByNavigations { get; set; } = new List<PayrollPeriod>();

    public virtual ICollection<Payroll> PayrollRejectedByNavigations { get; set; } = new List<Payroll>();

    public virtual ICollection<Payroll> PayrollSubmittedByNavigations { get; set; } = new List<Payroll>();

    public virtual ICollection<Payroll> PayrollUsers { get; set; } = new List<Payroll>();

    public virtual Branch? PreviousBranch { get; set; }

    public virtual ICollection<PublicHolidayConfig> PublicHolidayConfigs { get; set; } = new List<PublicHolidayConfig>();

    public virtual ICollection<RegionalMinimumWage> RegionalMinimumWages { get; set; } = new List<RegionalMinimumWage>();

    public virtual ICollection<RequestLimitRule> RequestLimitRuleCreatedByNavigations { get; set; } = new List<RequestLimitRule>();

    public virtual ICollection<RequestLimitRule> RequestLimitRulePublishedByNavigations { get; set; } = new List<RequestLimitRule>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<ShiftAssignment> ShiftAssignmentAssignedByNavigations { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftAssignment> ShiftAssignmentCancelledByNavigations { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftAssignment> ShiftAssignmentUsers { get; set; } = new List<ShiftAssignment>();

    public virtual ICollection<ShiftSwapRequest> ShiftSwapRequestRequesters { get; set; } = new List<ShiftSwapRequest>();

    public virtual ICollection<ShiftSwapRequest> ShiftSwapRequestRespondedByNavigations { get; set; } = new List<ShiftSwapRequest>();

    public virtual ICollection<ShiftSwapRequest> ShiftSwapRequestTargetUsers { get; set; } = new List<ShiftSwapRequest>();

    public virtual ICollection<StaffSalaryHistory> StaffSalaryHistoryCreatedByNavigations { get; set; } = new List<StaffSalaryHistory>();

    public virtual StaffSalaryHistory? StaffSalaryHistoryUser { get; set; }

    public virtual ICollection<TimesheetAdjustmentRequest> TimesheetAdjustmentRequestReviewedByNavigations { get; set; } = new List<TimesheetAdjustmentRequest>();

    public virtual ICollection<TimesheetAdjustmentRequest> TimesheetAdjustmentRequestUsers { get; set; } = new List<TimesheetAdjustmentRequest>();

    public virtual ICollection<Timesheet> TimesheetLockedByNavigations { get; set; } = new List<Timesheet>();

    public virtual ICollection<Timesheet> TimesheetReviewedByNavigations { get; set; } = new List<Timesheet>();

    public virtual ICollection<Timesheet> TimesheetUsers { get; set; } = new List<Timesheet>();

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();

    public virtual ICollection<WorkScheduleWeek> WorkScheduleWeekCancelledByNavigations { get; set; } = new List<WorkScheduleWeek>();

    public virtual ICollection<WorkScheduleWeek> WorkScheduleWeekCreatedByNavigations { get; set; } = new List<WorkScheduleWeek>();

    public virtual ICollection<WorkShift> WorkShiftCancelledByNavigations { get; set; } = new List<WorkShift>();

    public virtual ICollection<WorkShift> WorkShiftCreatedByNavigations { get; set; } = new List<WorkShift>();

    public virtual ICollection<WorkingTimeRule> WorkingTimeRules { get; set; } = new List<WorkingTimeRule>();
}
