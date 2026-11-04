import 'dart:typed_data';
import 'package:dio/dio.dart';
import '../models/face_registration_model.dart';
import 'api_client.dart';

class FaceRegistrationService {
  final ApiClient _apiClient = ApiClient();

  /// Check current staff member's face registration status
  Future<FaceProfileStatusModel> getStatus() async {
    final response = await _apiClient.get(
      '/api/staff/face-registration/status',
    );
    return FaceProfileStatusModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Create a new face registration session with challenges (TTL: 90s)
  Future<FaceRegistrationSessionModel> createSession() async {
    final response = await _apiClient.post(
      '/api/staff/face-registration/session',
    );
    return FaceRegistrationSessionModel.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Verify and register biometric face image
  Future<FaceRegistrationVerifyResult> verifyAndRegister({
    required String sessionId,
    required Uint8List imageBytes,
    String filename = 'live_face.jpg',
  }) async {
    final formData = FormData.fromMap({
      'SessionId': sessionId,
      'FaceImage': MultipartFile.fromBytes(imageBytes, filename: filename),
    });

    final response = await _apiClient.post(
      '/api/staff/face-registration/verify',
      data: formData,
    );
    return FaceRegistrationVerifyResult.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Cancel an active face registration session
  Future<void> cancelSession(String sessionId) async {
    try {
      await _apiClient.post(
        '/api/staff/face-registration/cancel',
        data: {'sessionId': sessionId},
      );
    } catch (_) {}
  }
}
