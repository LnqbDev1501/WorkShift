using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int AssignmentId { get; set; }

    public int AttendanceRuleId { get; set; }

    public Guid? CheckInFaceSessionId { get; set; }

    public Guid? CheckOutFaceSessionId { get; set; }

    public string RuleSnapshotJson { get; set; } = null!;

    public string EmployeeFullNameSnapshot { get; set; } = null!;

    public DateTime ShiftStartAtSnapshot { get; set; }

    public DateTime ShiftEndAtSnapshot { get; set; }

    public DateTime? CheckInTime { get; set; }

    public decimal? CheckInLatitude { get; set; }

    public decimal? CheckInLongitude { get; set; }

    public decimal? CheckInDistanceMeters { get; set; }

    public decimal? CheckInAccuracyMeters { get; set; }

    public string BranchNameSnapshot { get; set; } = null!;

    public string? BranchAddressSnapshot { get; set; }

    public decimal? BranchLatitudeSnapshot { get; set; }

    public decimal? BranchLongitudeSnapshot { get; set; }

    public bool? CheckInFaceVerified { get; set; }

    public decimal? CheckInFaceConfidence { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public decimal? CheckOutLatitude { get; set; }

    public decimal? CheckOutLongitude { get; set; }

    public decimal? CheckOutDistanceMeters { get; set; }

    public decimal? CheckOutAccuracyMeters { get; set; }

    public bool? CheckOutFaceVerified { get; set; }

    public decimal? CheckOutFaceConfidence { get; set; }

    public int ActualWorkingMinutes { get; set; }

    public int? LateArrivalRequestId { get; set; }

    public int? EarlyLeaveRequestId { get; set; }

    public string Status { get; set; } = null!;

    public int TotalLateMinutes { get; set; }

    public int ExcusedLateMinutes { get; set; }

    public int ChargeableLateMinutes { get; set; }

    public int TotalEarlyLeaveMinutes { get; set; }

    public int ExcusedEarlyLeaveMinutes { get; set; }

    public int ChargeableEarlyLeaveMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ShiftAssignment Assignment { get; set; } = null!;

    public virtual ICollection<AttendanceAdjustmentHistory> AttendanceAdjustmentHistories { get; set; } = new List<AttendanceAdjustmentHistory>();

    public virtual ICollection<AttendancePenaltyRecord> AttendancePenaltyRecords { get; set; } = new List<AttendancePenaltyRecord>();

    public virtual AttendanceRule AttendanceRule { get; set; } = null!;

    public virtual FaceVerificationSession? CheckInFaceSession { get; set; }

    public virtual FaceVerificationSession? CheckOutFaceSession { get; set; }

    public virtual TimeAdjustmentRequestDetail? EarlyLeaveRequest { get; set; }

    public virtual TimeAdjustmentRequestDetail? LateArrivalRequest { get; set; }

    public virtual ICollection<PayslipWorkDetail> PayslipWorkDetails { get; set; } = new List<PayslipWorkDetail>();

    public virtual ICollection<TimesheetAdjustmentRequest> TimesheetAdjustmentRequests { get; set; } = new List<TimesheetAdjustmentRequest>();
}
