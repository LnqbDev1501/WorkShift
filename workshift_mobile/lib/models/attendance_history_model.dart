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
      shiftName: json['shiftName']?.toString() ?? 'Ca làm việc',
      scheduledStartAt: DateTime.tryParse(json['scheduledStartAt']?.toString() ?? '') ?? DateTime.now(),
      scheduledEndAt: DateTime.tryParse(json['scheduledEndAt']?.toString() ?? '') ?? DateTime.now(),
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString() ?? '',
      checkInTime: json['checkInTime'] != null ? DateTime.tryParse(json['checkInTime'].toString()) : null,
      checkOutTime: json['checkOutTime'] != null ? DateTime.tryParse(json['checkOutTime'].toString()) : null,
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      status: json['status']?.toString() ?? 'UNKNOWN',
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      canCheckInNow: json['canCheckInNow'] == true,
      cannotCheckInReason: json['canNotCheckInReason']?.toString() ?? json['cannotCheckInReason']?.toString(),
      canCheckOutNow: json['canCheckOutNow'] == true,
      cannotCheckOutReason: json['cannotCheckOutReason']?.toString(),
      checkOutCutoffAt: json['checkOutCutoffAt'] != null ? DateTime.tryParse(json['checkOutCutoffAt'].toString()) : null,
      maxAttendanceDistanceMeters: json['maxAttendanceDistanceMeters'] as int? ?? 20,
      branchLatitude: json['branchLatitude'] != null ? double.tryParse(json['branchLatitude'].toString()) : null,
      branchLongitude: json['branchLongitude'] != null ? double.tryParse(json['branchLongitude'].toString()) : null,
    );
  }
}
