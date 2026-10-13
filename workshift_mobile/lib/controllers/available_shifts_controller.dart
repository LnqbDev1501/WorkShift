import 'package:flutter/foundation.dart';

import '../models/available_shift_model.dart';
import '../services/available_shifts_service.dart';

enum AvailableShiftFilter { allUpcoming, thisWeek, nextWeek, custom }

class AvailableShiftsController extends ChangeNotifier {
  final AvailableShiftsService _service;
  final DateTime Function() _now;
  final List<AvailableShiftModel> _shifts = [];
  AvailableShiftFilter _filter = AvailableShiftFilter.allUpcoming;
  DateTime? _fromDate;
  DateTime? _toDate;
  int _pageNumber = 0;
  int _totalPages = 0;
  int _totalItems = 0;
  int _generation = 0;
  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _disposed = false;
  String? _errorMessage;
  String? _loadMoreError;

  AvailableShiftsController({
    AvailableShiftsService? service,
    DateTime Function()? now,
  }) : _service = service ?? AvailableShiftsService(),
       _now = now ?? DateTime.now;

  List<AvailableShiftModel> get shifts => List.unmodifiable(_shifts);
  AvailableShiftFilter get filter => _filter;
  DateTime? get fromDate => _fromDate;
  DateTime? get toDate => _toDate;
  int get totalItems => _totalItems;
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  bool get hasMore => _pageNumber < _totalPages;
  String? get errorMessage => _errorMessage;
  String? get loadMoreError => _loadMoreError;

  DateTime get today {
    final vietnamTime = _now().toUtc().add(const Duration(hours: 7));
    return DateTime(vietnamTime.year, vietnamTime.month, vietnamTime.day);
  }

  Future<void> selectFilter(AvailableShiftFilter filter) async {
    if (filter == AvailableShiftFilter.custom || filter == _filter) return;
    _filter = filter;
    final currentDay = today;
    final monday = DateTime(
      currentDay.year,
      currentDay.month,
      currentDay.day - currentDay.weekday + 1,
    );
    switch (filter) {
      case AvailableShiftFilter.allUpcoming:
        _fromDate = null;
        _toDate = null;
      case AvailableShiftFilter.thisWeek:
        _fromDate = monday;
        _toDate = DateTime(monday.year, monday.month, monday.day + 6);
      case AvailableShiftFilter.nextWeek:
        _fromDate = DateTime(monday.year, monday.month, monday.day + 7);
        _toDate = DateTime(monday.year, monday.month, monday.day + 13);
      case AvailableShiftFilter.custom:
        return;
    }
    await _loadFirstPage(clearItems: true);
  }

  Future<void> selectDateRange(DateTime from, DateTime to) async {
    final fromDay = DateTime(from.year, from.month, from.day);
    final toDay = DateTime(to.year, to.month, to.day);
    if (fromDay.isAfter(toDay)) {
      throw ArgumentError('Start date must be before the end date.');
    }
    _filter = AvailableShiftFilter.custom;
    _fromDate = fromDay;
    _toDate = toDay;
    await _loadFirstPage(clearItems: true);
  }

  Future<void> refresh() => _loadFirstPage(clearItems: false);

  bool isOpen(AvailableShiftModel shift) {
    final now = _now().toUtc();
    return shift.status == 'SCHEDULED' &&
        shift.scheduleWeekStatus == 'OPEN_REGISTRATION' &&
        shift.registrationDeadline.isAfter(now) &&
        shift.scheduledStartAt.isAfter(now);
  }

  bool isClosingSoon(AvailableShiftModel shift) =>
      isOpen(shift) &&
      shift.registrationDeadline.difference(_now().toUtc()) <=
          const Duration(hours: 24);

  Future<void> _loadFirstPage({required bool clearItems}) async {
    if (_disposed) return;
    final generation = ++_generation;
    _isLoading = true;
    _isLoadingMore = false;
    _errorMessage = null;
    _loadMoreError = null;
    if (clearItems) {
      _shifts.clear();
      _totalItems = 0;
      _pageNumber = 0;
      _totalPages = 0;
    }
    notifyListeners();
    try {
      final page = await _service.getAvailableShifts(
        fromDate: _fromDate,
        toDate: _toDate,
      );
      if (!_isCurrent(generation)) return;
      _shifts
        ..clear()
        ..addAll(page.items);
      _pageNumber = page.pageNumber;
      _totalPages = page.totalPages;
      _totalItems = page.totalItems;
    } catch (error) {
      if (_isCurrent(generation)) _errorMessage = _message(error);
    } finally {
      if (_isCurrent(generation)) {
        _isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<void> loadMore() async {
    if (_disposed || _isLoading || _isLoadingMore || !hasMore) return;
    final generation = _generation;
    _isLoadingMore = true;
    _loadMoreError = null;
    notifyListeners();
    try {
      final page = await _service.getAvailableShifts(
        pageNumber: _pageNumber + 1,
        fromDate: _fromDate,
        toDate: _toDate,
      );
      if (!_isCurrent(generation)) return;
      final existingIds = _shifts.map((shift) => shift.shiftId).toSet();
      _shifts.addAll(
        page.items.where((shift) => existingIds.add(shift.shiftId)),
      );
      _pageNumber = page.pageNumber;
      _totalPages = page.totalPages;
      _totalItems = page.totalItems;
    } catch (error) {
      if (_isCurrent(generation)) _loadMoreError = _message(error);
    } finally {
      if (_isCurrent(generation)) {
        _isLoadingMore = false;
        notifyListeners();
      }
    }
  }

  bool _isCurrent(int generation) => !_disposed && generation == _generation;

  String _message(Object error) => error is String && error.trim().isNotEmpty
      ? error
      : 'Unable to load shifts. Please try again.';

  @override
  void dispose() {
    _disposed = true;
    _generation++;
    super.dispose();
  }
}
