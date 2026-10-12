import 'package:dio/dio.dart';
import '../models/shift_model.dart';
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

  /// Lấy thông tin ca làm hôm nay: GET /api/staff/attendance/eligible-shift
  Future<EligibleShiftModel?> getTodayEligibleShift() async {
    try {
      final response = await _apiClient.get('/api/staff/attendance/eligible-shift');
      if (response.statusCode == 200 && response.data != null) {
        return EligibleShiftModel.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } on String catch (_) {
      // Backend trả về 404 nghĩa là hôm nay nhân viên không có ca làm
      return null;
    } catch (e) {
      if (e is DioException && (e.response?.statusCode == 404 || e.response?.statusCode == 204)) {
        return null;
      }
      return null;
    }
  }
}
