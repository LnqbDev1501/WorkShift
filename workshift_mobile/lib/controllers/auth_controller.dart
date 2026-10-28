import 'package:flutter/material.dart';
import '../models/auth_model.dart';
import '../services/auth_service.dart';

/// Bộ não điều khiển màn hình Đăng nhập (Quản lý trạng thái loading, lỗi, dữ liệu user)
class AuthController extends ChangeNotifier {
  final AuthService _authService = AuthService();

  bool _isLoading = false;
  String? _errorMessage;
  String? _successMessage;
  LoginResponse? _currentUser;

  // Getters cho giao diện lắng nghe
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  String? get successMessage => _successMessage;
  LoginResponse? get currentUser => _currentUser;

  /// Hàm thực hiện đăng nhập
  /// Trả về `true` nếu thành công, `false` nếu thất bại
  Future<bool> login({required String email, required String password}) async {
    _isLoading = true;
    _errorMessage = null;
    _successMessage = null;
    notifyListeners(); // Báo cho giao diện biết để hiện vòng xoay loading

    try {
      final request = LoginRequest(email: email, password: password);
      _currentUser = await _authService.login(request);
      _isLoading = false;
      notifyListeners(); // Báo cho giao diện tắt loading
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners(); // Báo cho giao diện hiện thông báo lỗi
      return false;
    }
  }

  /// Hàm thực hiện quên mật khẩu
  /// Trả về `true` nếu thành công, `false` nếu thất bại
  Future<bool> forgotPassword({required String email}) async {
    _isLoading = true;
    _errorMessage = null;
    _successMessage = null;
    notifyListeners();

    try {
      final request = ForgotPasswordRequest(email: email);
      final response = await _authService.forgotPassword(request);
      _successMessage = response.message;
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Xóa thông báo lỗi khi người dùng gõ lại
  void clearError() {
    if (_errorMessage != null) {
      _errorMessage = null;
      notifyListeners();
    }
  }

  /// Xóa thông báo thành công
  void clearSuccess() {
    if (_successMessage != null) {
      _successMessage = null;
      notifyListeners();
    }
  }

  /// Đăng xuất khỏi hệ thống
  Future<void> logout() async {
    await _authService.logout();
    _currentUser = null;
    notifyListeners();
  }
}
