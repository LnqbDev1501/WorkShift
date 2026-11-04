import 'package:flutter/material.dart';
import '../../models/attendance/attendance_shift_model.dart';
import '../../models/auth/user_model.dart';
import '../../models/requests/staff_request_model.dart';
import '../../services/requests/staff_request_service.dart';
import '../../services/auth/auth_service.dart';
import '../../services/home/home_service.dart';

class HomeController extends ChangeNotifier {
  final HomeService _homeService = HomeService();
  final AuthService _authService = AuthService();

  final StaffRequestService _requestService = StaffRequestService();
  List<StaffEmployeeRequestListItemModel> _recentRequests = [];
  String? requestsError;
  String? weeklyHoursError;
  int _weeklyWorkingMinutes = 0;

  List<StaffEmployeeRequestListItemModel> get recentRequests =>
      List.unmodifiable(_recentRequests);
  int get weeklyWorkingMinutes => _weeklyWorkingMinutes;

  DateTime get weekStart {
    final now = DateTime.now();
    return DateTime(now.year, now.month, now.day - now.weekday + 1);
  }

  DateTime get weekEnd => weekStart.add(const Duration(days: 6));

  Future<void> _loadRecentRequests() async {
    requestsError = null;
    try {
      final result = await _requestService.getRequests(
        pageSize: 3,
        sortByUpdatedAt: true,
      );
      _recentRequests = result.items;
    } catch (_) {
      requestsError = 'Unable to load recent requests. Pull down to retry.';
    }
  }

  Future<void> _loadWeeklyHours() async {
    weeklyHoursError = null;
    try {
      final history = await _homeService.getAttendanceHistory(
        fromDate: weekStart,
        toDate: weekEnd,
        throwOnError: true,
      );
      _weeklyWorkingMinutes = history.fold<int>(
        0,
        (total, entry) =>
            total +
            (entry.actualWorkingMinutes > 0 ? entry.actualWorkingMinutes : 0),
      );
    } catch (_) {
      weeklyHoursError = 'Unable to load working hours. Pull down to retry.';
    }
  }

  bool _isLoading = true;
  bool _isCheckingIn = false;
  bool _isCheckingOut = false;
  UserModel? _user;
  EligibleShiftModel? _todayShift;
  String? _errorMessage;
  String? _shiftErrorMessage;
  bool _hasRegisteredFace = true;

  bool get isLoading => _isLoading;
  bool get isCheckingIn => _isCheckingIn;
  bool get isCheckingOut => _isCheckingOut;
  bool get isAttendanceActionInProgress => _isCheckingIn || _isCheckingOut;

  void setCheckingIn(bool value) {
    if (_isCheckingIn != value) {
      _isCheckingIn = value;
      notifyListeners();
    }
  }

  void setCheckingOut(bool value) {
    if (_isCheckingOut != value) {
      _isCheckingOut = value;
      notifyListeners();
    }
  }

  UserModel? get user => _user;
  EligibleShiftModel? get todayShift => _todayShift;
  String? get errorMessage => _errorMessage;
  String? get shiftErrorMessage => _shiftErrorMessage;
  bool get hasRegisteredFace => _hasRegisteredFace;

  /// Lấy ca làm việc trước đó chưa Checkout (nếu có)
  EligibleShiftModel? get unCheckedOutPreviousShift =>
      _todayShift?.unfinishedPreviousShift;

  bool get isFaceNotRegistered =>
      !_hasRegisteredFace ||
      (_shiftErrorMessage != null &&
          (_shiftErrorMessage!.toLowerCase().contains('face profile') ||
              _shiftErrorMessage!.toLowerCase().contains(
                'register your face',
              )));

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
    if (_user != null &&
        _user!.branchName != null &&
        _user!.branchName!.isNotEmpty) {
      return _user!.branchName!;
    }
    return 'Chi nhánh chính';
  }

  /// Cập nhật ngay lập tức trạng thái sau khi Check-In thành công (Optimistic UI Update)
  void markCheckInSuccess({int? assignmentId, DateTime? checkInTime}) {
    final targetId = assignmentId ?? _todayShift?.assignmentId;
    if (_todayShift != null &&
        (_todayShift!.assignmentId == targetId || targetId == null)) {
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
    if (_todayShift != null &&
        (_todayShift!.assignmentId == targetId || targetId == null)) {
      _todayShift = _todayShift!.copyWith(
        hasCheckedOut: true,
        checkOutTime: checkOutTime ?? DateTime.now(),
        canCheckInNow: false,
        canCheckOutNow: false,
        canNotCheckInReason:
            'You have already completed check-out for this shift.',
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
          identityNo: '',
          gender: '',
          dateOfBirth: null,
          address: '',
          avatarUrl: '',
          roleName: savedUser['role']?.toString() ?? '',
          status: 'ACTIVE',
          createdAt: '',
          updatedAt: '',
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
      await Future.wait([_loadRecentRequests(), _loadWeeklyHours()]);

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
