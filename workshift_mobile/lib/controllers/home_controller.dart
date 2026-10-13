import 'package:flutter/material.dart';
import '../models/attendance_shift_model.dart';
import '../models/user_model.dart';
import '../services/auth_service.dart';
import '../services/home_service.dart';

class HomeController extends ChangeNotifier {
  final HomeService _homeService = HomeService();
  final AuthService _authService = AuthService();

  bool _isLoading = true;
  UserModel? _user;
  EligibleShiftModel? _todayShift;
  String? _errorMessage;
  String? _shiftErrorMessage;
  bool _hasRegisteredFace = true;

  bool get isLoading => _isLoading;
  UserModel? get user => _user;
  EligibleShiftModel? get todayShift => _todayShift;
  String? get errorMessage => _errorMessage;
  String? get shiftErrorMessage => _shiftErrorMessage;
  bool get hasRegisteredFace => _hasRegisteredFace;

  /// Lấy ca làm việc trước đó chưa Checkout (nếu có)
  EligibleShiftModel? get unCheckedOutPreviousShift => _todayShift?.unfinishedPreviousShift;

  bool get isFaceNotRegistered =>
      !_hasRegisteredFace ||
      (_shiftErrorMessage != null &&
          (_shiftErrorMessage!.toLowerCase().contains('face profile') ||
              _shiftErrorMessage!.toLowerCase().contains('register your face')));

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

  /// Cập nhật ngay lập tức trạng thái sau khi Check-In thành công (Optimistic UI Update)
  void markCheckInSuccess({int? assignmentId, DateTime? checkInTime}) {
    final targetId = assignmentId ?? _todayShift?.assignmentId;
    if (_todayShift != null && (_todayShift!.assignmentId == targetId || targetId == null)) {
      _todayShift = _todayShift!.copyWith(
        hasCheckedIn: true,
        checkInTime: checkInTime ?? DateTime.now(),
        canCheckInNow: false,
        canNotCheckInReason: 'You have already checked in for this shift.',
        canCheckOutNow: true,
      );
    }
    notifyListeners();
  }

  /// Cập nhật ngay lập tức trạng thái sau khi Check-Out thành công (Optimistic UI Update)
  void markCheckOutSuccess({int? assignmentId, DateTime? checkOutTime}) {
    final targetId = assignmentId ?? _todayShift?.assignmentId;
    if (_todayShift != null && (_todayShift!.assignmentId == targetId || targetId == null)) {
      _todayShift = _todayShift!.copyWith(
        hasCheckedOut: true,
        checkOutTime: checkOutTime ?? DateTime.now(),
        canCheckInNow: false,
        canCheckOutNow: false,
        canNotCheckInReason: 'You have already completed check-out for this shift.',
      );
    }
    notifyListeners();
  }

  /// Nạp toàn bộ dữ liệu trang chủ (hỗ trợ silent refresh ngầm)
  Future<void> loadHomeData({bool silent = false}) async {
    if (!silent) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

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

      // 2. Gọi API song song: Profile và kiểm tra trạng thái đăng ký khuôn mặt
      _shiftErrorMessage = null;
      final results = await Future.wait([
        _homeService.getMyProfile(),
        _homeService.checkFaceRegistrationStatus(),
      ]);

      if (results[0] != null) {
        _user = results[0] as UserModel;
      }
      _hasRegisteredFace = results[1] as bool;

      // 3. Lấy ca làm việc hợp lệ hôm nay
      try {
        _todayShift = await _homeService.getTodayEligibleShift();
      } catch (shiftErr) {
        _shiftErrorMessage = shiftErr.toString();
        _todayShift = null;
        if (_shiftErrorMessage!.toLowerCase().contains('face profile') ||
            _shiftErrorMessage!.toLowerCase().contains('register your face')) {
          _hasRegisteredFace = false;
        }
      }

      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }
}
