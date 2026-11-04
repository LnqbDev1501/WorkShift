import 'dart:async';
import 'package:flutter/foundation.dart';
import '../../models/shifts/shift_swap_model.dart';
import '../../services/shifts/shift_swap_service.dart';

enum ShiftSwapAction { cancel, accept, reject }

class ShiftSwapDetailController extends ChangeNotifier {
  final ShiftSwapService _service;
  final DateTime Function() _now;
  Timer? _expiryTimer;
  final int requestId;
  ShiftSwapDetailController({
    required this.requestId,
    ShiftSwapService? service,
    DateTime Function()? now,
  }) : _service = service ?? ShiftSwapService(),
       _now = now ?? DateTime.now;
  ShiftSwapModel? request;
  String? userId;
  String? error;
  String? actionError;
  bool isLoading = false;
  bool isSaving = false;
  bool _disposed = false;
  int _generation = 0;
  String? get effectiveStatus => request?.statusAt(_now());
  bool get isSent =>
      request != null &&
      userId != null &&
      request!.requesterId.toLowerCase() == userId!.toLowerCase();
  bool get canCancel =>
      !isLoading && !isSaving && effectiveStatus == 'PENDING' && isSent;
  bool get canRespond =>
      !isLoading &&
      !isSaving &&
      effectiveStatus == 'PENDING' &&
      userId != null &&
      request!.targetUserId.toLowerCase() == userId!.toLowerCase();

  Future<void> refresh() async {
    if (_disposed || isSaving) return;
    final generation = ++_generation;
    isLoading = true;
    error = null;
    actionError = null;
    request = null;
    notifyListeners();
    try {
      final id = await _service.getCurrentUserId();
      final result = await _service.getDetail(requestId);
      if (_disposed || generation != _generation) return;
      userId = id;
      request = result;
      _watchExpiry();
    } catch (e) {
      if (!_disposed && generation == _generation) {
        error = e is String ? e : 'Couldn’t load this request.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<bool> respond(ShiftSwapAction action, {String? note}) async {
    if (_disposed ||
        (action == ShiftSwapAction.cancel ? !canCancel : !canRespond)) {
      return false;
    }
    if ((note?.length ?? 0) > 500) {
      actionError = 'Note cannot exceed 500 characters.';
      notifyListeners();
      return false;
    }
    isSaving = true;
    actionError = null;
    ++_generation;
    notifyListeners();
    try {
      final result = await switch (action) {
        ShiftSwapAction.cancel => _service.cancel(requestId, _clean(note)),
        ShiftSwapAction.accept => _service.accept(requestId),
        ShiftSwapAction.reject => _service.reject(requestId, _clean(note)),
      };
      if (_disposed) return false;
      request = result;
      _watchExpiry();
      return true;
    } catch (e) {
      if (!_disposed) {
        actionError = e is String
            ? e
            : 'Couldn’t update this request. Please refresh and try again.';
      }
      return false;
    } finally {
      if (!_disposed) {
        isSaving = false;
        notifyListeners();
      }
    }
  }

  String? _clean(String? value) =>
      value == null || value.trim().isEmpty ? null : value.trim();

  void _watchExpiry() {
    _expiryTimer?.cancel();
    if (effectiveStatus != 'PENDING') return;
    _expiryTimer = Timer(request!.expiresAt.difference(_now().toUtc()), () {
      if (!_disposed) notifyListeners();
    });
  }

  @override
  void dispose() {
    _disposed = true;
    _expiryTimer?.cancel();
    _generation++;
    super.dispose();
  }
}
