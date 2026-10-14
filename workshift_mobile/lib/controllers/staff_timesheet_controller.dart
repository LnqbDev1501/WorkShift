import 'package:flutter/foundation.dart';
import '../models/staff_timesheet_model.dart';
import '../services/staff_timesheet_service.dart';

class StaffTimesheetController extends ChangeNotifier {
  final StaffTimesheetService _service = StaffTimesheetService();

  bool _isLoading = false;
  bool _isConfirming = false;
  bool _isSubmittingAdjustment = false;
  String? _errorMessage;
  StaffWeeklyTimesheetModel? _timesheet;
  int? _selectedWeekId;

  bool get isLoading => _isLoading;
  bool get isConfirming => _isConfirming;
  bool get isSubmittingAdjustment => _isSubmittingAdjustment;
  String? get errorMessage => _errorMessage;
  StaffWeeklyTimesheetModel? get timesheet => _timesheet;
  int? get selectedWeekId => _selectedWeekId;

  List<StaffTimesheetWeekOptionModel> get availableWeeks => _timesheet?.availableWeeks ?? [];

  /// Loads weekly timesheet data for the staff member
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

  /// Switches to another schedule week
  Future<void> selectWeek(int weekId) async {
    if (_selectedWeekId == weekId && _timesheet != null) return;
    _selectedWeekId = weekId;
    await loadTimesheet(scheduleWeekId: weekId);
  }

  /// Navigates to previous or next schedule week
  Future<void> navigateWeek({required bool next}) async {
    if (_timesheet == null || availableWeeks.isEmpty) return;

    final currentIndex = availableWeeks.indexWhere((w) => w.scheduleWeekId == _selectedWeekId);
    if (currentIndex == -1) return;

    // availableWeeks is sorted by WeekStartDate DESC:
    // index 0 is newest week, higher index is older week.
    // "next" (more recent) means decreasing index
    // "prev" (older) means increasing index
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

  /// Confirms weekly timesheet
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

  /// Submits one or multiple timesheet adjustment requests
  Future<String?> submitAdjustments({
    required List<ShiftAdjustmentItemInput> adjustments,
  }) async {
    if (_timesheet == null) return 'Timesheet data not loaded.';

    _isSubmittingAdjustment = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final updated = await _service.submitAdjustments(
        scheduleWeekId: _timesheet!.scheduleWeekId,
        adjustments: adjustments,
      );
      _timesheet = updated;
      _selectedWeekId = updated.scheduleWeekId;
      return null;
    } catch (e) {
      _errorMessage = e.toString();
      return _errorMessage;
    } finally {
      _isSubmittingAdjustment = false;
      notifyListeners();
    }
  }
}
