import 'package:flutter/material.dart';
import '../models/shift_model.dart';
import '../models/user_model.dart';
import '../services/auth_service.dart';
import '../services/home_service.dart';

/// Bộ não điều khiển màn hình Trang chủ (Quản lý trạng thái tải ca làm và thông tin nhân viên)
class HomeController extends ChangeNotifier {
  final HomeService _homeService = HomeService();
  final AuthService _authService = AuthService();

  bool _isLoading = true;
  UserModel? _user;
  EligibleShiftModel? _todayShift;
  String? _errorMessage;

  bool get isLoading => _isLoading;
  UserModel? get user => _user;
  EligibleShiftModel? get todayShift => _todayShift;
  String? get errorMessage => _errorMessage;

  // Lấy tên hiển thị
  String get displayName {
    if (_user != null && _user!.fullName.isNotEmpty) {
      return _user!.fullName;
    }
    return 'Bạn';
  }

  // Lấy tên chi nhánh
  String get branchName {
    if (_todayShift != null && _todayShift!.branchName.isNotEmpty) {
      return _todayShift!.branchName;
    }
    if (_user != null && _user!.branchName != null && _user!.branchName!.isNotEmpty) {
      return _user!.branchName!;
    }
    return 'Chi nhánh chính';
  }

  /// Nạp toàn bộ dữ liệu trang chủ
  Future<void> loadHomeData() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // 1. Thử lấy profile từ bộ nhớ cache lưu lúc đăng nhập trước
      final savedUser = await _authService.getSavedUser();
      if (savedUser != null) {
        _user = UserModel(
          userId: savedUser['userId']?.toString() ?? '',
          fullName: savedUser['fullName']?.toString() ?? '',
          email: savedUser['email']?.toString() ?? '',
          phone: '',
          avatarUrl: '',
          roleName: savedUser['role']?.toString() ?? '',
        );
      }

      // 2. Gọi API song song: Lấy profile mới nhất và ca làm hôm nay
      final results = await Future.wait([
        _homeService.getMyProfile(),
        _homeService.getTodayEligibleShift(),
      ]);

      if (results[0] != null) {
        _user = results[0] as UserModel;
      }
      _todayShift = results[1] as EligibleShiftModel?;

      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }
}
