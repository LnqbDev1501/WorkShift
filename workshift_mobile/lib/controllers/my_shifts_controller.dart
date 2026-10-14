import 'package:flutter/foundation.dart';

import '../models/available_shift_model.dart';
import '../services/my_shifts_service.dart';
import '../services/shift_registration_service.dart';

enum MySchedulePeriod { upcoming, thisWeek, nextWeek, custom }

class MyShiftsController extends ChangeNotifier {
  final MyShiftsService _service;
  final ShiftRegistrationService _registrationService;
  final DateTime Function() _now;
  final List<AvailableShiftModel> _shifts = [];
  int _generation = 0;
  int _page = 0;
  int _totalPages = 0;
  int _totalItems = 0;
  bool _disposed = false;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  int? _processingShiftId;
  String? _error;
  String? _loadMoreError;
  String? _actionError;
  MySchedulePeriod _period = MySchedulePeriod.upcoming;
  DateTime? _fromDate;
  DateTime? _toDate;
  DateTime? _selectedDay;

  MyShiftsController({
    MyShiftsService? service,
    ShiftRegistrationService? registrationService,
    DateTime Function()? now,
  }) : _service = service ?? MyShiftsService(),
       _registrationService = registrationService ?? ShiftRegistrationService(),
       _now = now ?? DateTime.now;

  List<AvailableShiftModel> get shifts => List.unmodifiable(_shifts);
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get isProcessing => _processingShiftId != null;
  int? get processingShiftId => _processingShiftId;
  int get totalItems => _totalItems;
  bool get hasMore => _page < _totalPages;
  String? get error => _error;
  String? get loadMoreError => _loadMoreError;
  String? get actionError => _actionError;
  MySchedulePeriod get period => _period;
  DateTime get fromDate => _fromDate ?? today;
  DateTime? get toDate => _toDate;
  DateTime get weekStart {
    final date = fromDate;
    return DateTime(date.year, date.month, date.day - date.weekday + 1);
  }

  DateTime get weekEnd =>
      DateTime(weekStart.year, weekStart.month, weekStart.day + 6);
  bool get isWeekView =>
      _toDate != null && fromDate == weekStart && _toDate == weekEnd;
  List<DateTime> get weekDays => List.generate(
    7,
    (index) => DateTime(weekStart.year, weekStart.month, weekStart.day + index),
  );
  DateTime get selectedDay => _selectedDay ?? today;

  Future<void> showWeek(DateTime date) async {
    final start = DateTime(date.year, date.month, date.day - date.weekday + 1);
    _selectedDay =
        today.isBefore(start) ||
            today.isAfter(DateTime(start.year, start.month, start.day + 6))
        ? start
        : today;
    await selectDateRange(
      start,
      DateTime(start.year, start.month, start.day + 6),
    );
  }

  void selectDay(DateTime date) {
    if (_disposed) return;
    _selectedDay = DateTime(date.year, date.month, date.day);
    notifyListeners();
  }

  List<AvailableShiftModel> shiftsForDay(DateTime day) => _shifts.where((
    shift,
  ) {
    final date = shift.scheduledStartAt.toUtc().add(const Duration(hours: 7));
    return date.year == day.year &&
        date.month == day.month &&
        date.day == day.day;
  }).toList();

  Future<void> selectPeriod(MySchedulePeriod period) async {
    if (_disposed || period == _period || period == MySchedulePeriod.custom) {
      return;
    }
    _period = period;
    final date = today;
    final monday = DateTime(date.year, date.month, date.day - date.weekday + 1);
    switch (period) {
      case MySchedulePeriod.upcoming:
        _fromDate = null;
        _toDate = null;
      case MySchedulePeriod.thisWeek:
        _fromDate = monday;
        _toDate = DateTime(monday.year, monday.month, monday.day + 6);
      case MySchedulePeriod.nextWeek:
        _fromDate = DateTime(monday.year, monday.month, monday.day + 7);
        _toDate = DateTime(monday.year, monday.month, monday.day + 13);
      case MySchedulePeriod.custom:
        return;
    }
    _clearPeriodResults();
    await refresh();
  }

  Future<void> selectDateRange(DateTime from, DateTime to) async {
    if (_disposed) return;
    final first = DateTime(from.year, from.month, from.day);
    final last = DateTime(to.year, to.month, to.day);
    if (first.isAfter(last)) throw ArgumentError('Invalid date range.');
    _period = MySchedulePeriod.custom;
    _fromDate = first;
    _toDate = last;
    _clearPeriodResults();
    await refresh();
  }

  void _clearPeriodResults() {
    _shifts.clear();
    _page = 0;
    _totalPages = 0;
    _totalItems = 0;
  }

  String? cancellationBlockedReason(AvailableShiftModel shift) {
    final own = shift.myRegistration;
    if (own == null) return 'Your registration could not be found.';
    if (own.assignmentSource != 'SELF_REGISTRATION') {
      return 'This shift was assigned to you. Contact your manager to make changes.';
    }
    if (own.status != 'ASSIGNED') {
      return 'Only active registrations can be cancelled.';
    }
    if (shift.status != 'SCHEDULED') {
      return 'This shift is no longer scheduled.';
    }
    if (shift.scheduleWeekStatus != 'OPEN_REGISTRATION') {
      return 'Registration has closed. Contact your manager.';
    }
    if (!shift.scheduledStartAt.isAfter(_now().toUtc())) {
      return 'This shift has already started.';
    }
    if (!shift.registrationDeadline.isAfter(_now().toUtc())) {
      return 'The cancellation deadline has passed.';
    }
    if (!shift.canCancelRegistration) {
      return shift.cancellationBlockedReason ??
          'Cancellation is unavailable. Refresh or contact your manager.';
    }
    return null;
  }

  bool canCancel(AvailableShiftModel shift) =>
      !_disposed &&
      !isProcessing &&
      !_isLoading &&
      cancellationBlockedReason(shift) == null;

  String? validateReason(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Please enter a cancellation reason.';
    }
    if (value.length > 500) return 'The reason must not exceed 500 characters.';
    return null;
  }

  DateTime get today {
    final date = _now().toUtc().add(const Duration(hours: 7));
    return DateTime(date.year, date.month, date.day);
  }

  Future<void> refresh() => _load(reset: true);
  Future<void> loadMore() async {
    if (_disposed || _isLoading || _isLoadingMore || !hasMore) return;
    await _load(reset: false);
  }

  Future<void> _load({required bool reset}) async {
    if (_disposed) return;
    final generation = reset ? ++_generation : _generation;
    if (reset) {
      _isLoading = true;
      _isLoadingMore = false;
      _error = null;
    } else {
      _isLoadingMore = true;
    }
    _loadMoreError = null;
    notifyListeners();
    try {
      final page = await _service.getMyShifts(
        pageNumber: reset ? 1 : _page + 1,
        fromDate: fromDate,
        toDate: toDate,
      );
      if (!_current(generation)) return;
      if (reset) _shifts.clear();
      final ids = _shifts.map((shift) => shift.shiftId).toSet();
      _shifts.addAll(page.items.where((shift) => ids.add(shift.shiftId)));
      _page = page.pageNumber;
      _totalPages = page.totalPages;
      _totalItems = page.totalItems;
    } catch (error) {
      if (_current(generation)) {
        if (reset) {
          _error = _message(error);
        } else {
          _loadMoreError = _message(error);
        }
      }
    } finally {
      if (_current(generation)) {
        _isLoading = false;
        _isLoadingMore = false;
        notifyListeners();
      }
    }
  }

  // Recheck server eligibility before opening the reason form.
  Future<AvailableShiftModel?> prepareCancellation(
    AvailableShiftModel shift,
  ) async {
    if (_disposed || isProcessing) return null;
    _processingShiftId = shift.shiftId;
    _actionError = null;
    notifyListeners();
    try {
      final latest = await _service.getDetail(shift.shiftId);
      if (_disposed) return null;
      if (latest.myRegistration?.assignmentId !=
          shift.myRegistration?.assignmentId) {
        _actionError =
            'Your assignment has changed. Refresh your shifts before cancelling.';
        return null;
      }
      final index = _shifts.indexWhere(
        (item) => item.shiftId == latest.shiftId,
      );
      if (index >= 0) _shifts[index] = latest;
      _actionError = cancellationBlockedReason(latest);
      return _actionError == null ? latest : null;
    } catch (error) {
      if (!_disposed) _actionError = _message(error);
      return null;
    } finally {
      if (!_disposed) {
        _processingShiftId = null;
        notifyListeners();
      }
    }
  }

  Future<bool> cancel(AvailableShiftModel shift, String reason) async {
    if (_disposed || isProcessing) return false;
    _actionError = validateReason(reason) ?? cancellationBlockedReason(shift);
    if (_actionError != null) {
      notifyListeners();
      return false;
    }
    _processingShiftId = shift.shiftId;
    notifyListeners();
    try {
      final result = await _registrationService.cancel(
        shift.shiftId,
        reason.trim(),
      );
      if (_disposed) return false;
      if (result.status != 'CANCELLED' ||
          result.shiftId != shift.shiftId ||
          result.assignmentId != shift.myRegistration?.assignmentId) {
        throw const FormatException('Unexpected cancellation response.');
      }
      _generation++;
      final exists = _shifts.any((item) => item.shiftId == shift.shiftId);
      _shifts.removeWhere((item) => item.shiftId == shift.shiftId);
      if (exists && _totalItems > 0) _totalItems--;
      notifyListeners();
      await refresh();
      return true;
    } catch (error) {
      if (!_disposed) {
        _actionError = error is String
            ? error
            : 'Unable to confirm cancellation. Refresh your shifts before trying again.';
      }
      return false;
    } finally {
      if (!_disposed) {
        _processingShiftId = null;
        notifyListeners();
      }
    }
  }

  bool _current(int generation) => !_disposed && generation == _generation;
  String _message(Object error) =>
      error is String ? error : 'Unable to load your shifts. Please try again.';
  @override
  void dispose() {
    _disposed = true;
    _generation++;
    super.dispose();
  }
}
