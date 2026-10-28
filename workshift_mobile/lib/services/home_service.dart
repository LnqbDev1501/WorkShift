import '../utils/api_date_format.dart';
import 'package:dio/dio.dart';
import '../models/attendance_history_model.dart';
import '../models/attendance_shift_model.dart';
import '../models/user_model.dart';
import 'api_client.dart';

/// Dịch vụ gọi API dữ liệu cho màn hình Trang chủ
class HomeService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy thông tin cá nhân của nhân viên: GET /api/profile/me
  Future<UserModel?> getMyProfile() async {
    try {
      final response = await _apiClient.get('/api/profile/me');
      if (response.data != null && response.data is Map<String, dynamic>) {
        return UserModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (_) {
      // Nếu lỗi mạng hoặc chưa lấy được profile, trả về null để dùng dữ liệu lưu lúc login
      return null;
    }
  }

  /// Kiểm tra trạng thái đăng ký gương mặt: GET /api/staff/face-registration/status
  Future<bool> checkFaceRegistrationStatus() async {
    try {
      final response = await _apiClient.get(
        '/api/staff/face-registration/status',
      );
      if (response.statusCode == 200 &&
          response.data != null &&
          response.data is Map<String, dynamic>) {
        final data = response.data as Map<String, dynamic>;
        return data['hasRegisteredFace'] == true;
      }
      return false;
    } catch (_) {
      return false;
    }
  }

  /// Lấy thông tin ca làm hôm nay: GET /api/staff/attendance/eligible-shift
  Future<EligibleShiftModel?> getTodayEligibleShift() async {
    try {
      final response = await _apiClient.get(
        '/api/staff/attendance/eligible-shift',
      );
      if (response.statusCode == 200 && response.data != null) {
        return EligibleShiftModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      return null;
    } on String catch (e) {
      final lower = e.toLowerCase();
      // Nếu là lỗi tiên quyết (chưa đăng ký gương mặt, hợp đồng hết hạn...)
      if (lower.contains('face profile') ||
          lower.contains('register your face')) {
        throw 'You must register your face profile before performing attendance check-in.';
      }
      if (lower.contains('contract') ||
          lower.contains('not active') ||
          lower.contains('terminated')) {
        rethrow;
      }
      // 404 hoặc không có ca làm hôm nay
      return null;
    } catch (e) {
      if (e is DioException) {
        final statusCode = e.response?.statusCode;
        if (statusCode == 404 || statusCode == 204) {
          return null;
        }
        final msg = e.response?.data?['message']?.toString() ?? '';
        if (msg.toLowerCase().contains('face profile') ||
            msg.toLowerCase().contains('register your face')) {
          throw 'You must register your face profile before performing attendance check-in.';
        }
      }
      return null;
    }
  }

  /// Lấy lịch sử chấm công của nhân viên: GET /api/staff/attendance/history
  Future<List<StaffAttendanceHistoryModel>> getAttendanceHistory({
    DateTime? fromDate,
    DateTime? toDate,
    bool throwOnError = false,
  }) async {
    try {
      final queryParams = <String, dynamic>{};
      if (fromDate != null) {
        queryParams['fromDate'] = formatApiDate(fromDate);
      }
      if (toDate != null) {
        queryParams['toDate'] = formatApiDate(toDate);
      }

      final response = await _apiClient.get(
        '/api/staff/attendance/history',
        queryParameters: queryParams.isNotEmpty ? queryParams : null,
      );

      if (response.statusCode == 200 &&
          response.data != null &&
          response.data is List) {
        return (response.data as List)
            .map(
              (item) => StaffAttendanceHistoryModel.fromJson(
                item as Map<String, dynamic>,
              ),
            )
            .toList();
      }
      return [];
    } catch (_) {
      if (throwOnError) rethrow;
      return [];
    }
  }

  /// Lấy chi tiết chấm công ca làm việc: GET /api/staff/attendance/history/{assignmentId}
  Future<StaffAttendanceDetailModel?> getStaffAttendanceDetail(
    int assignmentId,
  ) async {
    try {
      final response = await _apiClient.get(
        '/api/staff/attendance/history/$assignmentId',
      );
      if (response.statusCode == 200 &&
          response.data != null &&
          response.data is Map<String, dynamic>) {
        return StaffAttendanceDetailModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      return null;
    } catch (_) {
      return null;
    }
  }
}
