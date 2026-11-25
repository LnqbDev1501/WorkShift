import 'package:flutter/foundation.dart';
import '../../models/attendance/attendance_history_model.dart';
import '../../models/shifts/available_shift_model.dart';
import '../../services/home/home_service.dart';
import '../../services/shifts/my_shifts_service.dart';

class ShiftDetailController extends ChangeNotifier {
  final int shiftId;
  final MyShiftsService _service;
  final HomeService _homeService;
  AvailableShiftModel? _shift;
  StaffAttendanceDetailModel? _attendanceDetail;
  String? _error;
  bool _isLoading = false;
  bool _disposed = false;
  int _generation = 0;

  ShiftDetailController({
    required this.shiftId,
    MyShiftsService? service,
    HomeService? homeService,
  }) : _service = service ?? MyShiftsService(),
       _homeService = homeService ?? HomeService();

  AvailableShiftModel? get shift => _shift;
  StaffAttendanceDetailModel? get attendanceDetail => _attendanceDetail;
  String? get error => _error;
  bool get isLoading => _isLoading;

  Future<void> refresh() async {
    if (_disposed) return;
    final generation = ++_generation;
    _error = null;
    _isLoading = true;
    notifyListeners();
    try {
      if (shiftId <= 0) throw 'Invalid shift. Please return to the shift list.';
      final result = await _service.getDetail(shiftId);
      if (!_current(generation)) return;
      if (result.shiftId != shiftId) {
        throw const FormatException('Unexpected shift response.');
      }
      _shift = result;

      // Try fetching attendance detail if an assignment exists
      final assignmentId = result.myRegistration?.assignmentId;
      if (assignmentId != null && assignmentId > 0) {
        try {
          _attendanceDetail = await _homeService.getStaffAttendanceDetail(
            assignmentId,
          );
        } catch (_) {
          _attendanceDetail = null;
        }
      } else {
        _attendanceDetail = null;
      }
    } catch (error) {
      if (_current(generation)) {
        _shift = null;
        _attendanceDetail = null;
        _error = error is String
            ? error
            : 'Unable to load shift details. Please try again.';
      }
    } finally {
      if (_current(generation)) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  bool _current(int generation) => !_disposed && _generation == generation;

  @override
  void dispose() {
    _disposed = true;
    _generation++;
    super.dispose();
  }
}
