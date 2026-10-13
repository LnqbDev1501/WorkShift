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

  StaffShiftAttendanceModel({
    required this.assignmentId,
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
