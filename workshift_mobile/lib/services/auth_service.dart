import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/auth_model.dart';
import 'api_client.dart';

/// Dịch vụ xử lý đăng nhập, đăng xuất, quên mật khẩu
class AuthService {
  final ApiClient _apiClient = ApiClient();
  static const String userProfileKey = 'user_profile';

  /// Gọi API Đăng nhập lên Backend .NET: POST /api/auth/login
  Future<LoginResponse> login(LoginRequest request) async {
    // 1. Gửi request đăng nhập qua ApiClient
    final response = await _apiClient.post(
      '/api/auth/login',
      data: request.toJson(),
    );

    // 2. Chuyển đổi dữ liệu JSON trả về thành LoginResponse
    final loginResponse = LoginResponse.fromJson(
      response.data as Map<String, dynamic>,
    );

    // 3. Tự động lưu Access Token vào máy để các request sau tự động có Token
    await _apiClient.saveToken(loginResponse.accessToken);

    // 4. Lưu thông tin cơ bản của User vào máy
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      userProfileKey,
      jsonEncode({
        'userId': loginResponse.userId,
        'email': loginResponse.email,
        'fullName': loginResponse.fullName,
        'role': loginResponse.role,
      }),
    );

    return loginResponse;
  }

  /// Gọi API Quên mật khẩu: POST /api/auth/forgot-password
  /// Backend luôn trả về 204 NoContent để bảo vệ thông tin tài khoản
  Future<ForgotPasswordResponse> forgotPassword(
    ForgotPasswordRequest request,
  ) async {
    try {
      await _apiClient.post(
        '/api/auth/forgot-password',
        data: request.toJson(),
      );
      return ForgotPasswordResponse.success();
    } catch (e) {
      // Backend luôn trả 204, nhưng nếu có lỗi network thì ném ra
      rethrow;
    }
  }

  /// Đăng xuất: Xóa Token và thông tin User trong máy
  Future<void> logout() async {
    await _apiClient.clearToken();
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(userProfileKey);
  }

  /// Lấy thông tin User đã lưu nếu đã đăng nhập trước đó
  Future<Map<String, dynamic>?> getSavedUser() async {
    final prefs = await SharedPreferences.getInstance();
    final userJson = prefs.getString(userProfileKey);
    if (userJson != null) {
      return jsonDecode(userJson) as Map<String, dynamic>;
    }
    return null;
  }
}
