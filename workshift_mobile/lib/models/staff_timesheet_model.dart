import 'dart:developer';

class StaffWeeklyTimesheetModel {
  final int scheduleWeekId;
  final int? payrollPeriodId;
  final int? timesheetId;
  final String period;
  final String weekStartDate;
  final String weekEndDate;
  final int branchId;
  final String branchName;
  final String status;
  final String staffReviewStatus;
  final bool canConfirm;
  final bool isLocked;
  final DateTime? reviewDeadline;
  final bool isPastDeadline;
  final String? reviewDeadlineDisplay;
  final int pendingAdjustmentCount;
  final int checkInBeforeMinutes;
  final int checkOutAfterMinutes;

  String get formattedDeadline {
    if (reviewDeadlineDisplay != null && reviewDeadlineDisplay!.isNotEmpty) {
      return reviewDeadlineDisplay!;
    }
    if (reviewDeadline == null) return '';
    final local = reviewDeadline!.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year;
    return '$hour:$minute, $day/$month/$year';
  }

  final int totalAssignedShifts;
  final int completedShifts;
  final int absentShifts;
  final int incompleteShifts;
  final double totalWorkHours;
  final int totalWorkMinutes;
  final int totalLateMinutes;
  final int totalEarlyLeaveMinutes;
  final String totalWorkTimeDisplay;
  final String totalLateDisplay;
  final String totalEarlyLeaveDisplay;
  final List<StaffShiftAttendanceModel> shifts;
  final List<StaffTimesheetWeekOptionModel> availableWeeks;

  StaffWeeklyTimesheetModel({
    required this.scheduleWeekId,
    this.payrollPeriodId,
    this.timesheetId,
    required this.period,
    required this.weekStartDate,
    required this.weekEndDate,
    required this.branchId,
    required this.branchName,
    required this.status,
    required this.staffReviewStatus,
    required this.canConfirm,
    required this.isLocked,
    this.reviewDeadline,
    required this.isPastDeadline,
    this.reviewDeadlineDisplay,
    required this.pendingAdjustmentCount,
    this.checkInBeforeMinutes = 30,
    this.checkOutAfterMinutes = 60,
    required this.totalAssignedShifts,
    required this.completedShifts,
    required this.absentShifts,
    required this.incompleteShifts,
    required this.totalWorkHours,
    required this.totalWorkMinutes,
    required this.totalLateMinutes,
    required this.totalEarlyLeaveMinutes,
    required this.totalWorkTimeDisplay,
    required this.totalLateDisplay,
    required this.totalEarlyLeaveDisplay,
    required this.shifts,
    required this.availableWeeks,
  });

  factory StaffWeeklyTimesheetModel.fromJson(Map<String, dynamic> json) {
    try {
      final rawShifts = json['shifts'] as List<dynamic>? ?? [];
      final rawWeeks = json['availableWeeks'] as List<dynamic>? ?? [];

      DateTime? parseNullableDateTime(dynamic v) {
        if (v == null) return null;
        return DateTime.tryParse(v.toString());
      }

      return StaffWeeklyTimesheetModel(
        scheduleWeekId: json['scheduleWeekId'] as int? ?? 0,
        payrollPeriodId: json['payrollPeriodId'] as int?,
        timesheetId: json['timesheetId'] as int?,
        period: json['period']?.toString() ?? '',
        weekStartDate: json['weekStartDate']?.toString() ?? '',
        weekEndDate: json['weekEndDate']?.toString() ?? '',
        branchId: json['branchId'] as int? ?? 0,
        branchName: json['branchName']?.toString() ?? '',
        status: json['status']?.toString() ?? 'NOT GENERATED',
        staffReviewStatus: json['staffReviewStatus']?.toString() ?? '-',
        canConfirm: json['canConfirm'] == true,
        isLocked: json['isLocked'] == true,
        reviewDeadline: parseNullableDateTime(json['reviewDeadline']),
        isPastDeadline: json['isPastDeadline'] == true,
        reviewDeadlineDisplay: json['reviewDeadlineDisplay']?.toString(),
        pendingAdjustmentCount: json['pendingAdjustmentCount'] as int? ?? 0,
        checkInBeforeMinutes: json['checkInBeforeMinutes'] as int? ?? 30,
        checkOutAfterMinutes: json['checkOutAfterMinutes'] as int? ?? 60,
        totalAssignedShifts: json['totalAssignedShifts'] as int? ?? 0,
        completedShifts: json['completedShifts'] as int? ?? 0,
        absentShifts: json['absentShifts'] as int? ?? 0,
        incompleteShifts: json['incompleteShifts'] as int? ?? 0,
        totalWorkHours: (json['totalWorkHours'] as num?)?.toDouble() ?? 0.0,
        totalWorkMinutes: json['totalWorkMinutes'] as int? ?? 0,
        totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
        totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
        totalWorkTimeDisplay: json['totalWorkTimeDisplay']?.toString() ?? '0h 0m',
        totalLateDisplay: json['totalLateDisplay']?.toString() ?? '0h 0m',
        totalEarlyLeaveDisplay: json['totalEarlyLeaveDisplay']?.toString() ?? '0h 0m',
        shifts: rawShifts
            .map((s) => StaffShiftAttendanceModel.fromJson(s as Map<String, dynamic>))
            .toList(),
        availableWeeks: rawWeeks
            .map((w) => StaffTimesheetWeekOptionModel.fromJson(w as Map<String, dynamic>))
            .toList(),
      );
    } catch (e, stack) {
      log('Error parsing StaffWeeklyTimesheetModel: $e\n$stack');
      rethrow;
    }
  }
}

class StaffShiftAttendanceModel {
  final int assignmentId;
  final int? attendanceId;
  final int shiftId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final DateTime? checkInTime;
  final DateTime? checkOutTime;
  final int actualWorkingMinutes;
  final int totalLateMinutes;
  final int totalEarlyLeaveMinutes;
  final String attendanceStatus;
  final String assignmentStatus;
  final String dateDisplay;
  final String scheduledRangeDisplay;
  final String checkInDisplay;
  final String checkOutDisplay;
  final String workTimeDisplay;
  final String lateDisplay;
  final String earlyLeaveDisplay;

  // Adjustment fields
  final bool hasAdjustmentRequest;
  final int? adjustmentRequestId;
  final String? adjustmentStatus; // PENDING, APPROVED, REJECTED, null
  final DateTime? requestedCheckInTime;
  final DateTime? requestedCheckOutTime;
  final String? adjustmentReason;
  final String? managerNote;
  final bool canRequestAdjustment;

  StaffShiftAttendanceModel({
    required this.assignmentId,
    this.attendanceId,
    required this.shiftId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    this.checkInTime,
    this.checkOutTime,
    required this.actualWorkingMinutes,
    required this.totalLateMinutes,
    required this.totalEarlyLeaveMinutes,
    required this.attendanceStatus,
    required this.assignmentStatus,
    required this.dateDisplay,
    required this.scheduledRangeDisplay,
    required this.checkInDisplay,
    required this.checkOutDisplay,
    required this.workTimeDisplay,
    required this.lateDisplay,
    required this.earlyLeaveDisplay,
    required this.hasAdjustmentRequest,
    this.adjustmentRequestId,
    this.adjustmentStatus,
    this.requestedCheckInTime,
    this.requestedCheckOutTime,
    this.adjustmentReason,
    this.managerNote,
    required this.canRequestAdjustment,
  });

  factory StaffShiftAttendanceModel.fromJson(Map<String, dynamic> json) {
    DateTime parseDateTime(dynamic v) {
      if (v == null) return DateTime.now();
      return DateTime.tryParse(v.toString()) ?? DateTime.now();
    }

    DateTime? parseNullableDateTime(dynamic v) {
      if (v == null) return null;
      return DateTime.tryParse(v.toString());
    }

    return StaffShiftAttendanceModel(
      assignmentId: json['assignmentId'] as int? ?? 0,
      attendanceId: json['attendanceId'] as int?,
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? 'Shift',
      scheduledStartAt: parseDateTime(json['scheduledStartAt']),
      scheduledEndAt: parseDateTime(json['scheduledEndAt']),
      checkInTime: parseNullableDateTime(json['checkInTime']),
      checkOutTime: parseNullableDateTime(json['checkOutTime']),
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      attendanceStatus: json['attendanceStatus']?.toString() ?? 'SCHEDULED',
      assignmentStatus: json['assignmentStatus']?.toString() ?? 'ASSIGNED',
      dateDisplay: json['dateDisplay']?.toString() ?? '',
      scheduledRangeDisplay: json['scheduledRangeDisplay']?.toString() ?? '',
      checkInDisplay: json['checkInDisplay']?.toString() ?? '-',
      checkOutDisplay: json['checkOutDisplay']?.toString() ?? '-',
      workTimeDisplay: json['workTimeDisplay']?.toString() ?? '0h 0m',
      lateDisplay: json['lateDisplay']?.toString() ?? '0h 0m',
      earlyLeaveDisplay: json['earlyLeaveDisplay']?.toString() ?? '0h 0m',
      hasAdjustmentRequest: json['hasAdjustmentRequest'] == true,
      adjustmentRequestId: json['adjustmentRequestId'] as int?,
      adjustmentStatus: json['adjustmentStatus']?.toString(),
      requestedCheckInTime: parseNullableDateTime(json['requestedCheckInTime']),
      requestedCheckOutTime: parseNullableDateTime(json['requestedCheckOutTime']),
      adjustmentReason: json['adjustmentReason']?.toString(),
      managerNote: json['managerNote']?.toString(),
      canRequestAdjustment: json['canRequestAdjustment'] == true,
    );
  }
}

class StaffTimesheetWeekOptionModel {
  final int scheduleWeekId;
  final String period;
  final String weekStartDate;
  final String weekEndDate;
  final bool hasTimesheet;
  final String status;

  StaffTimesheetWeekOptionModel({
    required this.scheduleWeekId,
    required this.period,
    required this.weekStartDate,
    required this.weekEndDate,
    required this.hasTimesheet,
    required this.status,
  });

  factory StaffTimesheetWeekOptionModel.fromJson(Map<String, dynamic> json) {
    return StaffTimesheetWeekOptionModel(
      scheduleWeekId: json['scheduleWeekId'] as int? ?? 0,
      period: json['period']?.toString() ?? '',
      weekStartDate: json['weekStartDate']?.toString() ?? '',
      weekEndDate: json['weekEndDate']?.toString() ?? '',
      hasTimesheet: json['hasTimesheet'] == true,
      status: json['status']?.toString() ?? 'NOT GENERATED',
    );
  }
}

class ShiftAdjustmentItemInput {
  final int assignmentId;
  final int? attendanceId;
  final bool adjustCheckIn;
  final DateTime? requestedCheckInTime;
  final bool adjustCheckOut;
  final DateTime? requestedCheckOutTime;
  final String reason;

  ShiftAdjustmentItemInput({
    required this.assignmentId,
    this.attendanceId,
    required this.adjustCheckIn,
    this.requestedCheckInTime,
    required this.adjustCheckOut,
    this.requestedCheckOutTime,
    required this.reason,
  });

  Map<String, dynamic> toJson() => {
    'assignmentId': assignmentId,
    if (attendanceId != null) 'attendanceId': attendanceId,
    'adjustCheckIn': adjustCheckIn,
    if (requestedCheckInTime != null) 'requestedCheckInTime': requestedCheckInTime!.toIso8601String(),
    'adjustCheckOut': adjustCheckOut,
    if (requestedCheckOutTime != null) 'requestedCheckOutTime': requestedCheckOutTime!.toIso8601String(),
    'reason': reason,
  };
}
