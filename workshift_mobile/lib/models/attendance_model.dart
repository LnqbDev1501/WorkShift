class FaceChallengeItem {
  final int order;
  final String action;
  final String instruction;

  FaceChallengeItem({
    required this.order,
    required this.action,
    required this.instruction,
  });

  factory FaceChallengeItem.fromJson(Map<String, dynamic> json) {
    return FaceChallengeItem(
      order: (json['order'] as num?)?.toInt() ?? 0,
      action: json['action']?.toString() ?? '',
      instruction: json['instruction']?.toString() ?? '',
    );
  }
}

class CheckInSessionResponse {
  final String sessionId;
  final DateTime expiresAt;
  final int expiresInSeconds;
  final String actionType;
  final List<FaceChallengeItem> challenges;

  CheckInSessionResponse({
    required this.sessionId,
    required this.expiresAt,
    required this.expiresInSeconds,
    required this.actionType,
    required this.challenges,
  });

  factory CheckInSessionResponse.fromJson(Map<String, dynamic> json) {
    final rawChallenges = json['challenges'] as List<dynamic>? ?? [];
    return CheckInSessionResponse(
      sessionId: json['sessionId']?.toString() ?? '',
      expiresAt:
          DateTime.tryParse(json['expiresAt']?.toString() ?? '') ??
          DateTime.now(),
      expiresInSeconds: (json['expiresInSeconds'] as num?)?.toInt() ?? 90,
      actionType: json['actionType']?.toString() ?? 'CHECK_IN',
      challenges: rawChallenges
          .map((c) => FaceChallengeItem.fromJson(c as Map<String, dynamic>))
          .toList(),
    );
  }
}

class CheckOutSessionResponse {
  final String sessionId;
  final DateTime expiresAt;
  final int expiresInSeconds;
  final String actionType;
  final List<FaceChallengeItem> challenges;

  CheckOutSessionResponse({
    required this.sessionId,
    required this.expiresAt,
    required this.expiresInSeconds,
    required this.actionType,
    required this.challenges,
  });

  factory CheckOutSessionResponse.fromJson(Map<String, dynamic> json) {
    final rawChallenges = json['challenges'] as List<dynamic>? ?? [];
    return CheckOutSessionResponse(
      sessionId: json['sessionId']?.toString() ?? '',
      expiresAt:
          DateTime.tryParse(json['expiresAt']?.toString() ?? '') ??
          DateTime.now(),
      expiresInSeconds: (json['expiresInSeconds'] as num?)?.toInt() ?? 90,
      actionType: json['actionType']?.toString() ?? 'CHECK_OUT',
      challenges: rawChallenges
          .map((c) => FaceChallengeItem.fromJson(c as Map<String, dynamic>))
          .toList(),
    );
  }
}

class CheckInResponse {
  final int attendanceId;
  final int assignmentId;
  final DateTime checkInTime;
  final String status;
  final double checkInDistanceMeters;
  final bool isLate;
  final int totalLateMinutes;
  final int excusedLateMinutes;
  final int chargeableLateMinutes;
  final double? faceMatchConfidence;
  final String message;

  CheckInResponse({
    required this.attendanceId,
    required this.assignmentId,
    required this.checkInTime,
    required this.status,
    required this.checkInDistanceMeters,
    required this.isLate,
    required this.totalLateMinutes,
    required this.excusedLateMinutes,
    required this.chargeableLateMinutes,
    this.faceMatchConfidence,
    required this.message,
  });

  factory CheckInResponse.fromJson(Map<String, dynamic> json) {
    return CheckInResponse(
      attendanceId: (json['attendanceId'] as num?)?.toInt() ?? 0,
      assignmentId: (json['assignmentId'] as num?)?.toInt() ?? 0,
      checkInTime:
          DateTime.tryParse(json['checkInTime']?.toString() ?? '') ??
          DateTime.now(),
      status: json['status']?.toString() ?? 'IN_PROGRESS',
      checkInDistanceMeters:
          (json['checkInDistanceMeters'] as num?)?.toDouble() ?? 0.0,
      isLate: json['isLate'] == true,
      totalLateMinutes: (json['totalLateMinutes'] as num?)?.toInt() ?? 0,
      excusedLateMinutes: (json['excusedLateMinutes'] as num?)?.toInt() ?? 0,
      chargeableLateMinutes:
          (json['chargeableLateMinutes'] as num?)?.toInt() ?? 0,
      faceMatchConfidence: (json['faceMatchConfidence'] as num?)?.toDouble(),
      message: json['message']?.toString() ?? 'Check-in processed successfully',
    );
  }
}

class CheckOutResponse {
  final int attendanceId;
  final int assignmentId;
  final DateTime checkInTime;
  final DateTime checkOutTime;
  final int actualWorkingMinutes;
  final double workingHours;
  final String status;
  final double checkOutDistanceMeters;
  final bool isEarlyLeave;
  final int totalEarlyLeaveMinutes;
  final int excusedEarlyLeaveMinutes;
  final int chargeableEarlyLeaveMinutes;
  final double? faceMatchConfidence;
  final String message;

  CheckOutResponse({
    required this.attendanceId,
    required this.assignmentId,
    required this.checkInTime,
    required this.checkOutTime,
    required this.actualWorkingMinutes,
    required this.workingHours,
    required this.status,
    required this.checkOutDistanceMeters,
    required this.isEarlyLeave,
    required this.totalEarlyLeaveMinutes,
    required this.excusedEarlyLeaveMinutes,
    required this.chargeableEarlyLeaveMinutes,
    this.faceMatchConfidence,
    required this.message,
  });

  factory CheckOutResponse.fromJson(Map<String, dynamic> json) {
    return CheckOutResponse(
      attendanceId: (json['attendanceId'] as num?)?.toInt() ?? 0,
      assignmentId: (json['assignmentId'] as num?)?.toInt() ?? 0,
      checkInTime:
          DateTime.tryParse(json['checkInTime']?.toString() ?? '') ??
          DateTime.now(),
      checkOutTime:
          DateTime.tryParse(json['checkOutTime']?.toString() ?? '') ??
          DateTime.now(),
      actualWorkingMinutes:
          (json['actualWorkingMinutes'] as num?)?.toInt() ?? 0,
      workingHours: (json['workingHours'] as num?)?.toDouble() ?? 0.0,
      status: json['status']?.toString() ?? 'COMPLETED',
      checkOutDistanceMeters:
          (json['checkOutDistanceMeters'] as num?)?.toDouble() ?? 0.0,
      isEarlyLeave: json['isEarlyLeave'] == true,
      totalEarlyLeaveMinutes:
          (json['totalEarlyLeaveMinutes'] as num?)?.toInt() ?? 0,
      excusedEarlyLeaveMinutes:
          (json['excusedEarlyLeaveMinutes'] as num?)?.toInt() ?? 0,
      chargeableEarlyLeaveMinutes:
          (json['chargeableEarlyLeaveMinutes'] as num?)?.toInt() ?? 0,
      faceMatchConfidence: (json['faceMatchConfidence'] as num?)?.toDouble(),
      message:
          json['message']?.toString() ?? 'Check-out processed successfully',
    );
  }
}
