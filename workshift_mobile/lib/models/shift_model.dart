/// Model ca làm việc hôm nay (khớp với EligibleShiftResponse từ Backend .NET)
class EligibleShiftModel {
  final int assignmentId;
  final int shiftId;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final int branchId;
  final String branchName;
  final String branchAddress;
  final bool canCheckInNow;
  final String? canNotCheckInReason;
  final bool hasApprovedLateArrival;
  final bool hasApprovedLeave;

  EligibleShiftModel({
    required this.assignmentId,
    required this.shiftId,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.branchId,
    required this.branchName,
    required this.branchAddress,
    required this.canCheckInNow,
    this.canNotCheckInReason,
    required this.hasApprovedLateArrival,
    required this.hasApprovedLeave,
  });

  factory EligibleShiftModel.fromJson(Map<String, dynamic> json) {
    return EligibleShiftModel(
      assignmentId: (json['assignmentId'] as num?)?.toInt() ?? 0,
      shiftId: (json['shiftId'] as num?)?.toInt() ?? 0,
      shiftName: json['shiftName']?.toString() ?? 'Ca làm việc',
      scheduledStartAt: DateTime.tryParse(json['scheduledStartAt']?.toString() ?? '') ?? DateTime.now(),
      scheduledEndAt: DateTime.tryParse(json['scheduledEndAt']?.toString() ?? '') ?? DateTime.now(),
      branchId: (json['branchId'] as num?)?.toInt() ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString() ?? '',
      canCheckInNow: json['canCheckInNow'] == true,
      canNotCheckInReason: json['canNotCheckInReason']?.toString(),
      hasApprovedLateArrival: json['hasApprovedLateArrival'] == true,
      hasApprovedLeave: json['hasApprovedLeave'] == true,
    );
  }

  // Format giờ làm dạng 08:00 - 16:00 (chuyển sang giờ thiết bị)
  String get formattedTimeRange {
    final localStart = scheduledStartAt.toLocal();
    final localEnd = scheduledEndAt.toLocal();
    final startHour = localStart.hour.toString().padLeft(2, '0');
    final startMin = localStart.minute.toString().padLeft(2, '0');
    final endHour = localEnd.hour.toString().padLeft(2, '0');
    final endMin = localEnd.minute.toString().padLeft(2, '0');
    return '$startHour:$startMin – $endHour:$endMin';
  }

  // Thời lượng ca làm (ví dụ: 8 giờ hoặc 8h 30p)
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
