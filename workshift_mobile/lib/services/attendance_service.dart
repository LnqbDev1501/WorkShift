import 'dart:typed_data';
import 'package:dio/dio.dart';
import '../models/attendance_model.dart';
import 'api_client.dart';

class AttendanceService {
  final ApiClient _apiClient = ApiClient();

  /// Create a liveness challenge session for Check-In (TTL: 90s)
  Future<CheckInSessionResponse> createCheckInSession({
    int? assignmentId,
  }) async {
    final response = await _apiClient.post(
      '/api/staff/attendance/check-in/session',
      queryParameters: assignmentId != null
          ? {'assignmentId': assignmentId}
          : null,
    );
    return CheckInSessionResponse.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Submit Check-In with GPS coordinates and captured face image
  Future<CheckInResponse> checkIn({
    required int assignmentId,
    required String sessionId,
    required double latitude,
    required double longitude,
    double? gpsAccuracy,
    required Uint8List imageBytes,
    String filename = 'face_capture.jpg',
  }) async {
    final formData = FormData.fromMap({
      'AssignmentId': assignmentId,
      'SessionId': sessionId,
      'Latitude': latitude,
      'Longitude': longitude,
      'GpsAccuracy': gpsAccuracy ?? 5.0,
      'FaceImage': MultipartFile.fromBytes(imageBytes, filename: filename),
    });

    final response = await _apiClient.post(
      '/api/staff/attendance/check-in',
      data: formData,
    );
    return CheckInResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Create a liveness challenge session for Check-Out (TTL: 90s)
  Future<CheckOutSessionResponse> createCheckOutSession(
    int assignmentId,
  ) async {
    final response = await _apiClient.post(
      '/api/staff/attendance/check-out/session',
      queryParameters: {'assignmentId': assignmentId},
    );
    return CheckOutSessionResponse.fromJson(
      response.data as Map<String, dynamic>,
    );
  }

  /// Submit Check-Out with GPS coordinates and captured face image
  Future<CheckOutResponse> checkOut({
    required int assignmentId,
    required String sessionId,
    required double latitude,
    required double longitude,
    double? gpsAccuracy,
    required Uint8List imageBytes,
    String filename = 'face_capture.jpg',
  }) async {
    final formData = FormData.fromMap({
      'AssignmentId': assignmentId,
      'SessionId': sessionId,
      'Latitude': latitude,
      'Longitude': longitude,
      'GpsAccuracy': gpsAccuracy ?? 5.0,
      'FaceImage': MultipartFile.fromBytes(imageBytes, filename: filename),
    });

    final response = await _apiClient.post(
      '/api/staff/attendance/check-out',
      data: formData,
    );
    return CheckOutResponse.fromJson(response.data as Map<String, dynamic>);
  }
}
