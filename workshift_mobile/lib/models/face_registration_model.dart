import 'attendance_model.dart';

class FaceProfileStatusModel {
  final bool hasRegisteredFace;
  final int? faceProfileId;
  final String? status;
  final DateTime? registeredAt;
  final String? registrationType;
  final bool canRegisterOrUpdate;
  final DateTime? nextAllowedRegistrationDate;
  final String? message;

  FaceProfileStatusModel({
    required this.hasRegisteredFace,
    this.faceProfileId,
    this.status,
    this.registeredAt,
    this.registrationType,
    this.canRegisterOrUpdate = true,
    this.nextAllowedRegistrationDate,
    this.message,
  });

  factory FaceProfileStatusModel.fromJson(Map<String, dynamic> json) {
    return FaceProfileStatusModel(
      hasRegisteredFace: json['hasRegisteredFace'] == true,
      faceProfileId: (json['faceProfileId'] as num?)?.toInt(),
      status: json['status']?.toString(),
      registeredAt: json['registeredAt'] != null
          ? DateTime.tryParse(json['registeredAt'].toString())
          : null,
      registrationType: json['registrationType']?.toString(),
      canRegisterOrUpdate: json['canRegisterOrUpdate'] == true,
      nextAllowedRegistrationDate: json['nextAllowedRegistrationDate'] != null
          ? DateTime.tryParse(json['nextAllowedRegistrationDate'].toString())
          : null,
      message: json['message']?.toString(),
    );
  }
}

class FaceRegistrationSessionModel {
  final String sessionId;
  final DateTime expiresAt;
  final int expiresInSeconds;
  final String actionType;
  final List<FaceChallengeItem> challenges;

  FaceRegistrationSessionModel({
    required this.sessionId,
    required this.expiresAt,
    required this.expiresInSeconds,
    required this.actionType,
    required this.challenges,
  });

  factory FaceRegistrationSessionModel.fromJson(Map<String, dynamic> json) {
    final rawChallenges = json['challenges'] as List<dynamic>? ?? [];
    return FaceRegistrationSessionModel(
      sessionId: json['sessionId']?.toString() ?? '',
      expiresAt:
          DateTime.tryParse(json['expiresAt']?.toString() ?? '') ??
          DateTime.now(),
      expiresInSeconds: (json['expiresInSeconds'] as num?)?.toInt() ?? 90,
      actionType: json['actionType']?.toString() ?? 'REGISTRATION',
      challenges: rawChallenges
          .map((c) => FaceChallengeItem.fromJson(c as Map<String, dynamic>))
          .toList(),
    );
  }
}

class FaceRegistrationVerifyResult {
  final bool success;
  final String message;
  final int faceProfileId;
  final String status;
  final double? faceQualityScore;
  final double livenessScore;
  final DateTime registeredAt;

  FaceRegistrationVerifyResult({
    required this.success,
    required this.message,
    required this.faceProfileId,
    required this.status,
    this.faceQualityScore,
    required this.livenessScore,
    required this.registeredAt,
  });

  factory FaceRegistrationVerifyResult.fromJson(Map<String, dynamic> json) {
    return FaceRegistrationVerifyResult(
      success: json['success'] == true,
      message: json['message']?.toString() ?? '',
      faceProfileId: (json['faceProfileId'] as num?)?.toInt() ?? 0,
      status: json['status']?.toString() ?? 'ACTIVE',
      faceQualityScore: (json['faceQualityScore'] as num?)?.toDouble(),
      livenessScore: (json['livenessScore'] as num?)?.toDouble() ?? 1.0,
      registeredAt:
          DateTime.tryParse(json['registeredAt']?.toString() ?? '') ??
          DateTime.now(),
    );
  }
}
