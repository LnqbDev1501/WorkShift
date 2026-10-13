import 'package:flutter/foundation.dart';
import '../models/staff_timesheet_model.dart';
import '../services/staff_timesheet_service.dart';

class StaffTimesheetController extends ChangeNotifier {
  final StaffTimesheetService _service = StaffTimesheetService();

  bool _isLoading = false;
  bool _isConfirming = false;
  String? _errorMessage;
  StaffWeeklyTimesheetModel? _timesheet;
  int? _selectedWeekId;

  bool get isLoading => _isLoading;
  bool get isConfirming => _isConfirming;
  String? get errorMessage => _errorMessage;
  StaffWeeklyTimesheetModel? get timesheet => _timesheet;
  int? get selectedWeekId => _selectedWeekId;

  List<StaffTimesheetWeekOptionModel> get availableWeeks => _timesheet?.availableWeeks ?? [];

  /// Tải thông tin bảng công tuần
  Future<void> loadTimesheet({int? scheduleWeekId}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final result = await _service.getMyWeeklyTimesheet(scheduleWeekId: scheduleWeekId);
      _timesheet = result;
      if (result != null) {
        _selectedWeekId = result.scheduleWeekId;
      }
    } catch (e) {
      _errorMessage = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Chuyển sang tuần khác
  Future<void> selectWeek(int weekId) async {
    if (_selectedWeekId == weekId && _timesheet != null) return;
    _selectedWeekId = weekId;
    await loadTimesheet(scheduleWeekId: weekId);
  }

  /// Chuyển sang tuần liền trước hoặc liền sau
  Future<void> navigateWeek({required bool next}) async {
    if (_timesheet == null || availableWeeks.isEmpty) return;

    final currentIndex = availableWeeks.indexWhere((w) => w.scheduleWeekId == _selectedWeekId);
    if (currentIndex == -1) return;

    // availableWeeks is sorted by WeekStartDate DESC:
    // index 0 is newest week, higher index is older week.
    // "next" (tương lai hơn) means decreasing index
    // "prev" (quá khứ hơn) means increasing index
    final targetIndex = next ? currentIndex - 1 : currentIndex + 1;
    if (targetIndex >= 0 && targetIndex < availableWeeks.length) {
      final targetWeek = availableWeeks[targetIndex];
      await selectWeek(targetWeek.scheduleWeekId);
    }
  }

  bool get hasPreviousWeek {
    if (_timesheet == null || availableWeeks.isEmpty) return false;
    final currentIndex = availableWeeks.indexWhere((w) => w.scheduleWeekId == _selectedWeekId);
    return currentIndex != -1 && currentIndex < availableWeeks.length - 1;
  }

  bool get hasNextWeek {
    if (_timesheet == null || availableWeeks.isEmpty) return false;
    final currentIndex = availableWeeks.indexWhere((w) => w.scheduleWeekId == _selectedWeekId);
    return currentIndex != -1 && currentIndex > 0;
  }

  /// Xác nhận bảng công tuần
  Future<bool> confirmTimesheet({String? note}) async {
    if (_timesheet == null) return false;

    _isConfirming = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final updated = await _service.confirmTimesheet(
        scheduleWeekId: _timesheet!.scheduleWeekId,
        note: note,
      );
      _timesheet = updated;
      _selectedWeekId = updated.scheduleWeekId;
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      return false;
    } finally {
      _isConfirming = false;
      notifyListeners();
    }
  }
}
