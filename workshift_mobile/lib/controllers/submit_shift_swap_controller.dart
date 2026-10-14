import 'package:flutter/foundation.dart';
import '../models/available_shift_model.dart';
import '../models/shift_swap_model.dart';
import '../services/my_shifts_service.dart';
import '../services/shift_swap_service.dart';

class SubmitShiftSwapController extends ChangeNotifier {
  final ShiftSwapService _service;
  final MyShiftsService _schedule;
  final DateTime Function() _now;
  SubmitShiftSwapController({
    ShiftSwapService? service,
    MyShiftsService? scheduleService,
    DateTime Function()? now,
  }) : _service = service ?? ShiftSwapService(),
       _schedule = scheduleService ?? MyShiftsService(),
       _now = now ?? DateTime.now;
  List<AvailableShiftModel> sources = const [];
  List<ShiftSwapCandidateModel> candidates = const [];
  AvailableShiftModel? source;
  ShiftSwapCandidateModel? target;
  bool isLoading = false;
  bool isLoadingCandidates = false;
  bool isSaving = false;
  String? error;
  String? candidateError;
  String? submitError;
  int _generation = 0;
  bool _disposed = false;
  bool get canSubmit =>
      !_disposed &&
      !isLoading &&
      !isLoadingCandidates &&
      !isSaving &&
      source != null &&
      target != null;

  Future<void> loadSources() async {
    if (_disposed || isSaving) return;
    final generation = ++_generation;
    source = null;
    target = null;
    sources = const [];
    candidates = const [];
    isLoading = true;
    isLoadingCandidates = false;
    error = null;
    candidateError = null;
    notifyListeners();
    final now = _now().toUtc();
    final local = now.add(const Duration(hours: 7));
    try {
      final items = <AvailableShiftModel>[];
      var pageNumber = 1;
      while (true) {
        final page = await _schedule.getMyShifts(
          pageNumber: pageNumber,
          fromDate: DateTime(local.year, local.month, local.day),
        );
        if (_disposed || generation != _generation) return;
        items.addAll(
          page.items.where(
            (s) =>
                s.myRegistration?.status == 'ASSIGNED' &&
                s.status == 'SCHEDULED' &&
                s.scheduledStartAt.isAfter(now) &&
                [
                  'OPEN_REGISTRATION',
                  'REGISTRATION_CLOSED',
                ].contains(s.scheduleWeekStatus),
          ),
        );
        if (pageNumber >= page.totalPages) break;
        pageNumber++;
      }
      sources = List.unmodifiable(items);
    } catch (e) {
      if (!_disposed && generation == _generation) {
        error = e is String ? e : 'Couldn’t load your shifts.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<void> selectSource(AvailableShiftModel value) async {
    if (_disposed ||
        isSaving ||
        !sources.any((s) => s.shiftId == value.shiftId)) {
      return;
    }
    final generation = ++_generation;
    source = value;
    target = null;
    candidates = const [];
    candidateError = null;
    submitError = null;
    isLoadingCandidates = true;
    notifyListeners();
    try {
      final result = await _service.getCandidates(
        value.myRegistration!.assignmentId,
      );
      if (!_disposed && generation == _generation) candidates = result;
    } catch (e) {
      if (!_disposed && generation == _generation) {
        candidateError = e is String ? e : 'Couldn’t find matching shifts.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        isLoadingCandidates = false;
        notifyListeners();
      }
    }
  }

  void selectTarget(ShiftSwapCandidateModel value) {
    if (_disposed ||
        isSaving ||
        isLoadingCandidates ||
        !candidates.contains(value)) {
      return;
    }
    target = value;
    submitError = null;
    notifyListeners();
  }

  Future<ShiftSwapModel?> submit(String reason) async {
    if (!canSubmit) return null;
    if (reason.length > 500) {
      submitError = 'Reason cannot exceed 500 characters.';
      notifyListeners();
      return null;
    }
    isSaving = true;
    submitError = null;
    notifyListeners();
    try {
      final result = await _service.submit(
        sourceAssignmentId: source!.myRegistration!.assignmentId,
        targetAssignmentId: target!.assignment.assignmentId,
        reason: reason.trim().isEmpty ? null : reason.trim(),
      );
      return _disposed ? null : result;
    } catch (e) {
      if (!_disposed) {
        submitError = e is String
            ? e
            : 'Couldn’t send this swap request. Try again.';
      }
      return null;
    } finally {
      if (!_disposed) {
        isSaving = false;
        notifyListeners();
      }
    }
  }

  @override
  void dispose() {
    _disposed = true;
    _generation++;
    super.dispose();
  }
}
