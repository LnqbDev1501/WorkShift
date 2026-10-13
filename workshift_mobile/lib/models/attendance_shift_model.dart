/// Shift model matching EligibleShiftResponse from ASP.NET Core backend
class EligibleShiftModel {
  final int assignmentId;
  final int shiftId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final int branchId;
  final String branchName;
  final String branchAddress;
  final double? branchLatitude;
  final double? branchLongitude;
  final int maxAttendanceDistanceMeters;
  final bool canCheckInNow;
  final String? canNotCheckInReason;
  final bool hasApprovedLeave;
  final DateTime? checkInCutoffAt;
  final DateTime? checkInWindowOpensAt;

  // Trạng thái Chấm công & Check-out
  final bool hasCheckedIn;
  final bool hasCheckedOut;
  final DateTime? checkInTime;
  final DateTime? checkOutTime;
  final bool canCheckOutNow;
  final String? canNotCheckOutReason;
  final DateTime? checkOutCutoffAt;
  final String? attendanceStatus;
  final EligibleShiftModel? unfinishedPreviousShift;

  EligibleShiftModel({
    required this.assignmentId,
    required this.shiftId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.branchId,
    required this.branchName,
    required this.branchAddress,
    this.branchLatitude,
    this.branchLongitude,
    this.maxAttendanceDistanceMeters = 100,
    required this.canCheckInNow,
    this.canNotCheckInReason,
    required this.hasApprovedLeave,
    this.hasCheckedIn = false,
    this.hasCheckedOut = false,
    this.checkInTime,
    this.checkOutTime,
    this.canCheckOutNow = false,
    this.canNotCheckOutReason,
    this.checkInCutoffAt,
    this.checkInWindowOpensAt,
    this.checkOutCutoffAt,
    this.attendanceStatus,
    this.unfinishedPreviousShift,
  });

  EligibleShiftModel copyWith({
    int? assignmentId,
    int? shiftId,
    String? shiftName,
    DateTime? scheduledStartAt,
    DateTime? scheduledEndAt,
    int? branchId,
    String? branchName,
    String? branchAddress,
    double? branchLatitude,
    double? branchLongitude,
    int? maxAttendanceDistanceMeters,
    bool? canCheckInNow,
    String? canNotCheckInReason,
    bool? hasApprovedLeave,
    bool? hasCheckedIn,
    bool? hasCheckedOut,
    DateTime? checkInTime,
    DateTime? checkOutTime,
    bool? canCheckOutNow,
    String? canNotCheckOutReason,
    DateTime? checkInCutoffAt,
    DateTime? checkInWindowOpensAt,
    DateTime? checkOutCutoffAt,
    String? attendanceStatus,
    EligibleShiftModel? unfinishedPreviousShift,
  }) {
    return EligibleShiftModel(
      assignmentId: assignmentId ?? this.assignmentId,
      shiftId: shiftId ?? this.shiftId,
      shiftName: shiftName ?? this.shiftName,
      scheduledStartAt: scheduledStartAt ?? this.scheduledStartAt,
      scheduledEndAt: scheduledEndAt ?? this.scheduledEndAt,
      branchId: branchId ?? this.branchId,
      branchName: branchName ?? this.branchName,
      branchAddress: branchAddress ?? this.branchAddress,
      branchLatitude: branchLatitude ?? this.branchLatitude,
      branchLongitude: branchLongitude ?? this.branchLongitude,
      maxAttendanceDistanceMeters: maxAttendanceDistanceMeters ?? this.maxAttendanceDistanceMeters,
      canCheckInNow: canCheckInNow ?? this.canCheckInNow,
      canNotCheckInReason: canNotCheckInReason ?? this.canNotCheckInReason,
      hasApprovedLeave: hasApprovedLeave ?? this.hasApprovedLeave,
      hasCheckedIn: hasCheckedIn ?? this.hasCheckedIn,
      hasCheckedOut: hasCheckedOut ?? this.hasCheckedOut,
      checkInTime: checkInTime ?? this.checkInTime,
      checkOutTime: checkOutTime ?? this.checkOutTime,
      canCheckOutNow: canCheckOutNow ?? this.canCheckOutNow,
      canNotCheckOutReason: canNotCheckOutReason ?? this.canNotCheckOutReason,
      checkInCutoffAt: checkInCutoffAt ?? this.checkInCutoffAt,
      checkInWindowOpensAt: checkInWindowOpensAt ?? this.checkInWindowOpensAt,
      checkOutCutoffAt: checkOutCutoffAt ?? this.checkOutCutoffAt,
      attendanceStatus: attendanceStatus ?? this.attendanceStatus,
      unfinishedPreviousShift: unfinishedPreviousShift ?? this.unfinishedPreviousShift,
    );
  }

  factory EligibleShiftModel.fromJson(Map<String, dynamic> json) {
    return EligibleShiftModel(
      assignmentId: (json['assignmentId'] as num?)?.toInt() ?? 0,
      shiftId: (json['shiftId'] as num?)?.toInt() ?? 0,
      shiftName: json['shiftName']?.toString() ?? 'Work Shift',
      scheduledStartAt: DateTime.tryParse(json['scheduledStartAt']?.toString() ?? '') ?? DateTime.now(),
      scheduledEndAt: DateTime.tryParse(json['scheduledEndAt']?.toString() ?? '') ?? DateTime.now(),
      branchId: (json['branchId'] as num?)?.toInt() ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString() ?? '',
      branchLatitude: (json['branchLatitude'] as num?)?.toDouble(),
      branchLongitude: (json['branchLongitude'] as num?)?.toDouble(),
      maxAttendanceDistanceMeters: (json['maxAttendanceDistanceMeters'] as num?)?.toInt() ?? 100,
      canCheckInNow: json['canCheckInNow'] == true,
      canNotCheckInReason: json['canNotCheckInReason']?.toString(),
      hasApprovedLeave: json['hasApprovedLeave'] == true,
      hasCheckedIn: json['hasCheckedIn'] == true,
      hasCheckedOut: json['hasCheckedOut'] == true,
      checkInTime: DateTime.tryParse(json['checkInTime']?.toString() ?? ''),
      checkOutTime: DateTime.tryParse(json['checkOutTime']?.toString() ?? ''),
      canCheckOutNow: json['canCheckOutNow'] == true,
      canNotCheckOutReason: json['canNotCheckOutReason']?.toString(),
      checkInCutoffAt: DateTime.tryParse(json['checkInCutoffAt']?.toString() ?? ''),
      checkInWindowOpensAt: DateTime.tryParse(json['checkInWindowOpensAt']?.toString() ?? ''),
      checkOutCutoffAt: DateTime.tryParse(json['checkOutCutoffAt']?.toString() ?? ''),
      attendanceStatus: json['attendanceStatus']?.toString(),
      unfinishedPreviousShift: json['unfinishedPreviousShift'] != null
          ? EligibleShiftModel.fromJson(json['unfinishedPreviousShift'] as Map<String, dynamic>)
          : null,
    );
  }

  /// Kiểm tra xem ca đã qua hạn chót check-in chưa
  bool get isCheckInExpired {
    if (checkInCutoffAt != null) {
      return DateTime.now().isAfter(checkInCutoffAt!.toLocal());
    }
    return false;
  }

  /// Kiểm tra xem ca đã qua hạn chót check-out chưa
  bool get isCheckoutExpired {
    if (checkOutCutoffAt != null) {
      return DateTime.now().isAfter(checkOutCutoffAt!.toLocal());
    }
    return DateTime.now().isAfter(scheduledEndAt.toLocal().add(const Duration(minutes: 15)));
  }

  /// Ca đã qua giờ checkout mà có check-in nhưng không check-out
  bool get isIncomplete => (hasCheckedIn && !hasCheckedOut && isCheckoutExpired) || attendanceStatus?.toUpperCase() == 'INCOMPLETE';

  /// Ca đã qua giờ checkout mà không check-in lẫn check-out
  bool get isAbsent => (!hasCheckedIn && isCheckoutExpired) || attendanceStatus?.toUpperCase() == 'ABSENT';

  // Format shift time range (e.g., 08:00 – 16:00 in device local time)
  String get formattedTimeRange {
    final localStart = scheduledStartAt.toLocal();
    final localEnd = scheduledEndAt.toLocal();
    final startHour = localStart.hour.toString().padLeft(2, '0');
    final startMin = localStart.minute.toString().padLeft(2, '0');
    final endHour = localEnd.hour.toString().padLeft(2, '0');
    final endMin = localEnd.minute.toString().padLeft(2, '0');
    return '$startHour:$startMin – $endHour:$endMin';
  }

  // Shift duration formatted string (e.g., 8 hrs or 8h 30m)
  String get durationText {
    final diff = scheduledEndAt.difference(scheduledStartAt);
    final hours = diff.inHours;
    final mins = diff.inMinutes % 60;
    if (mins == 0) {
      return '$hours hrs';
    }
    return '${hours}h ${mins}m';
  }
}
