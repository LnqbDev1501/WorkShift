import '../timesheet/staff_timesheet_model.dart';

class StaffAttendanceHistoryModel {
  final int? attendanceId;
  final int assignmentId;
  final int shiftId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final String branchName;
  final String branchAddress;
  final DateTime? checkInTime;
  final DateTime? checkOutTime;
  final int actualWorkingMinutes;
  final String status;
  final int totalLateMinutes;
  final int totalEarlyLeaveMinutes;
  final bool canCheckInNow;
  final String? cannotCheckInReason;
  final bool canCheckOutNow;
  final String? cannotCheckOutReason;
  final DateTime? checkOutCutoffAt;
  final int maxAttendanceDistanceMeters;
  final double? branchLatitude;
  final double? branchLongitude;

  StaffAttendanceHistoryModel({
    this.attendanceId,
    required this.assignmentId,
    required this.shiftId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.branchName,
    required this.branchAddress,
    this.checkInTime,
    this.checkOutTime,
    required this.actualWorkingMinutes,
    required this.status,
    required this.totalLateMinutes,
    required this.totalEarlyLeaveMinutes,
    this.canCheckInNow = false,
    this.cannotCheckInReason,
    required this.canCheckOutNow,
    this.cannotCheckOutReason,
    this.checkOutCutoffAt,
    required this.maxAttendanceDistanceMeters,
    this.branchLatitude,
    this.branchLongitude,
  });

  factory StaffAttendanceHistoryModel.fromJson(Map<String, dynamic> json) {
    return StaffAttendanceHistoryModel(
      attendanceId: json['attendanceId'] as int?,
      assignmentId: json['assignmentId'] as int? ?? 0,
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? 'Work Shift',
      scheduledStartAt:
          DateTime.tryParse(json['scheduledStartAt']?.toString() ?? '') ??
          DateTime.now(),
      scheduledEndAt:
          DateTime.tryParse(json['scheduledEndAt']?.toString() ?? '') ??
          DateTime.now(),
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString() ?? '',
      checkInTime: json['checkInTime'] != null
          ? DateTime.tryParse(json['checkInTime'].toString())
          : null,
      checkOutTime: json['checkOutTime'] != null
          ? DateTime.tryParse(json['checkOutTime'].toString())
          : null,
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      status: json['status']?.toString() ?? 'UNKNOWN',
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      canCheckInNow: json['canCheckInNow'] == true,
      cannotCheckInReason:
          json['canNotCheckInReason']?.toString() ??
          json['cannotCheckInReason']?.toString(),
      canCheckOutNow: json['canCheckOutNow'] == true,
      cannotCheckOutReason: json['cannotCheckOutReason']?.toString(),
      checkOutCutoffAt: json['checkOutCutoffAt'] != null
          ? DateTime.tryParse(json['checkOutCutoffAt'].toString())
          : null,
      maxAttendanceDistanceMeters:
          json['maxAttendanceDistanceMeters'] as int? ?? 20,
      branchLatitude: json['branchLatitude'] != null
          ? double.tryParse(json['branchLatitude'].toString())
          : null,
      branchLongitude: json['branchLongitude'] != null
          ? double.tryParse(json['branchLongitude'].toString())
          : null,
    );
  }
}

class StaffAttendanceDetailModel {
  final int assignmentId;
  final int shiftId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final int scheduleWeekId;
  final String weekPeriodDisplay;
  final String branchName;
  final String? branchAddress;
  final int maxAttendanceDistanceMeters;

  final int? attendanceId;
  final String status;
  final DateTime? checkInTime;
  final double? checkInLatitude;
  final double? checkInLongitude;
  final double? checkInDistanceMeters;
  final double? checkInAccuracyMeters;
  final bool? checkInFaceVerified;
  final double? checkInFaceConfidence;

  final DateTime? checkOutTime;
  final double? checkOutLatitude;
  final double? checkOutLongitude;
  final double? checkOutDistanceMeters;
  final double? checkOutAccuracyMeters;
  final bool? checkOutFaceVerified;
  final double? checkOutFaceConfidence;

  final int actualWorkingMinutes;
  final int totalLateMinutes;
  final int excusedLateMinutes;
  final int chargeableLateMinutes;
  final int totalEarlyLeaveMinutes;
  final int excusedEarlyLeaveMinutes;
  final int chargeableEarlyLeaveMinutes;

  final bool isExcusedLeave;
  final String? leaveReason;

  final AttendanceRequestSummaryModel? lateArrivalRequest;
  final AttendanceRequestSummaryModel? earlyLeaveRequest;
  final AttendanceRequestSummaryModel? leaveRequest;

  final StaffTimesheetAdjustmentModel? timesheetAdjustment;

  final List<StaffShiftPenaltyModel> penalties;

  StaffAttendanceDetailModel({
    required this.assignmentId,
    required this.shiftId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.scheduleWeekId,
    required this.weekPeriodDisplay,
    required this.branchName,
    this.branchAddress,
    required this.maxAttendanceDistanceMeters,
    this.attendanceId,
    required this.status,
    this.checkInTime,
    this.checkInLatitude,
    this.checkInLongitude,
    this.checkInDistanceMeters,
    this.checkInAccuracyMeters,
    this.checkInFaceVerified,
    this.checkInFaceConfidence,
    this.checkOutTime,
    this.checkOutLatitude,
    this.checkOutLongitude,
    this.checkOutDistanceMeters,
    this.checkOutAccuracyMeters,
    this.checkOutFaceVerified,
    this.checkOutFaceConfidence,
    required this.actualWorkingMinutes,
    required this.totalLateMinutes,
    required this.excusedLateMinutes,
    required this.chargeableLateMinutes,
    required this.totalEarlyLeaveMinutes,
    required this.excusedEarlyLeaveMinutes,
    required this.chargeableEarlyLeaveMinutes,
    required this.isExcusedLeave,
    this.leaveReason,
    this.lateArrivalRequest,
    this.earlyLeaveRequest,
    this.leaveRequest,
    this.timesheetAdjustment,
    this.penalties = const [],
  });

  List<StaffShiftPenaltyModel> get activePenalties =>
      penalties.where((p) => p.isApplied).toList();
  List<StaffShiftPenaltyModel> get cancelledPenalties =>
      penalties.where((p) => p.isCancelled).toList();
  double get totalActivePenaltyAmount =>
      activePenalties.fold(0.0, (sum, p) => sum + p.penaltyAmount);

  factory StaffAttendanceDetailModel.fromJson(Map<String, dynamic> json) {
    DateTime parseDateTime(dynamic v) {
      if (v == null) return DateTime.now();
      return DateTime.tryParse(v.toString()) ?? DateTime.now();
    }

    DateTime? parseNullableDateTime(dynamic v) {
      if (v == null) return null;
      return DateTime.tryParse(v.toString());
    }

    double? parseDouble(dynamic v) {
      if (v == null) return null;
      return (v as num).toDouble();
    }

    final rawPenalties = json['penalties'] as List<dynamic>? ?? [];

    return StaffAttendanceDetailModel(
      assignmentId: json['assignmentId'] as int? ?? 0,
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? 'Work Shift',
      scheduledStartAt: parseDateTime(json['scheduledStartAt']),
      scheduledEndAt: parseDateTime(json['scheduledEndAt']),
      scheduleWeekId: json['scheduleWeekId'] as int? ?? 0,
      weekPeriodDisplay: json['weekPeriodDisplay']?.toString() ?? '',
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString(),
      maxAttendanceDistanceMeters:
          json['maxAttendanceDistanceMeters'] as int? ?? 20,
      attendanceId: json['attendanceId'] as int?,
      status: json['status']?.toString() ?? 'SCHEDULED',
      checkInTime: parseNullableDateTime(json['checkInTime']),
      checkInLatitude: parseDouble(json['checkInLatitude']),
      checkInLongitude: parseDouble(json['checkInLongitude']),
      checkInDistanceMeters: parseDouble(json['checkInDistanceMeters']),
      checkInAccuracyMeters: parseDouble(json['checkInAccuracyMeters']),
      checkInFaceVerified: json['checkInFaceVerified'] as bool?,
      checkInFaceConfidence: parseDouble(json['checkInFaceConfidence']),
      checkOutTime: parseNullableDateTime(json['checkOutTime']),
      checkOutLatitude: parseDouble(json['checkOutLatitude']),
      checkOutLongitude: parseDouble(json['checkOutLongitude']),
      checkOutDistanceMeters: parseDouble(json['checkOutDistanceMeters']),
      checkOutAccuracyMeters: parseDouble(json['checkOutAccuracyMeters']),
      checkOutFaceVerified: json['checkOutFaceVerified'] as bool?,
      checkOutFaceConfidence: parseDouble(json['checkOutFaceConfidence']),
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      excusedLateMinutes: json['excusedLateMinutes'] as int? ?? 0,
      chargeableLateMinutes: json['chargeableLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      excusedEarlyLeaveMinutes: json['excusedEarlyLeaveMinutes'] as int? ?? 0,
      chargeableEarlyLeaveMinutes:
          json['chargeableEarlyLeaveMinutes'] as int? ?? 0,
      isExcusedLeave: json['isExcusedLeave'] == true,
      leaveReason: json['leaveReason']?.toString(),
      lateArrivalRequest: json['lateArrivalRequest'] != null
          ? AttendanceRequestSummaryModel.fromJson(
              json['lateArrivalRequest'] as Map<String, dynamic>,
            )
          : null,
      earlyLeaveRequest: json['earlyLeaveRequest'] != null
          ? AttendanceRequestSummaryModel.fromJson(
              json['earlyLeaveRequest'] as Map<String, dynamic>,
            )
          : null,
      leaveRequest: json['leaveRequest'] != null
          ? AttendanceRequestSummaryModel.fromJson(
              json['leaveRequest'] as Map<String, dynamic>,
            )
          : null,
      timesheetAdjustment: json['timesheetAdjustment'] != null
          ? StaffTimesheetAdjustmentModel.fromJson(
              json['timesheetAdjustment'] as Map<String, dynamic>,
            )
          : null,
      penalties: rawPenalties
          .map(
            (p) => StaffShiftPenaltyModel.fromJson(p as Map<String, dynamic>),
          )
          .toList(),
    );
  }
}

class AttendanceRequestSummaryModel {
  final int requestId;
  final String requestType;
  final String status;
  final String? reason;
  final DateTime? requestedTime;
  final DateTime createdAt;

  AttendanceRequestSummaryModel({
    required this.requestId,
    required this.requestType,
    required this.status,
    this.reason,
    this.requestedTime,
    required this.createdAt,
  });

  factory AttendanceRequestSummaryModel.fromJson(Map<String, dynamic> json) {
    return AttendanceRequestSummaryModel(
      requestId: json['requestId'] as int? ?? 0,
      requestType: json['requestType']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      reason: json['reason']?.toString(),
      requestedTime: json['requestedTime'] != null
          ? DateTime.tryParse(json['requestedTime'].toString())
          : null,
      createdAt:
          DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
          DateTime.now(),
    );
  }
}

class StaffTimesheetAdjustmentModel {
  final int adjustmentRequestId;
  final String status;
  final DateTime? originalCheckInTime;
  final DateTime? originalCheckOutTime;
  final DateTime? requestedCheckInTime;
  final DateTime? requestedCheckOutTime;
  final bool adjustCheckIn;
  final bool adjustCheckOut;
  final String reason;
  final String? managerNote;
  final DateTime? reviewedAt;
  final DateTime createdAt;

  StaffTimesheetAdjustmentModel({
    required this.adjustmentRequestId,
    required this.status,
    this.originalCheckInTime,
    this.originalCheckOutTime,
    this.requestedCheckInTime,
    this.requestedCheckOutTime,
    required this.adjustCheckIn,
    required this.adjustCheckOut,
    required this.reason,
    this.managerNote,
    this.reviewedAt,
    required this.createdAt,
  });

  bool get isApproved => status.toUpperCase() == 'APPROVED';
  bool get isPending => status.toUpperCase() == 'PENDING';
  bool get isRejected => status.toUpperCase() == 'REJECTED';

  factory StaffTimesheetAdjustmentModel.fromJson(Map<String, dynamic> json) {
    DateTime? parseNullableDateTime(dynamic v) {
      if (v == null) return null;
      return DateTime.tryParse(v.toString());
    }

    return StaffTimesheetAdjustmentModel(
      adjustmentRequestId: json['adjustmentRequestId'] as int? ?? 0,
      status: json['status']?.toString() ?? '',
      originalCheckInTime: parseNullableDateTime(json['originalCheckInTime']),
      originalCheckOutTime: parseNullableDateTime(json['originalCheckOutTime']),
      requestedCheckInTime: parseNullableDateTime(json['requestedCheckInTime']),
      requestedCheckOutTime: parseNullableDateTime(
        json['requestedCheckOutTime'],
      ),
      adjustCheckIn: json['adjustCheckIn'] == true,
      adjustCheckOut: json['adjustCheckOut'] == true,
      reason: json['reason']?.toString() ?? '',
      managerNote: json['managerNote']?.toString(),
      reviewedAt: parseNullableDateTime(json['reviewedAt']),
      createdAt:
          DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
          DateTime.now(),
    );
  }
}
