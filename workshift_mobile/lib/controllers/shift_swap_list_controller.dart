import 'package:flutter/foundation.dart';
import '../models/shift_swap_model.dart';
import '../services/shift_swap_service.dart';

class ShiftSwapListController extends ChangeNotifier {
  final ShiftSwapService _service;
  ShiftSwapListController({ShiftSwapService? service})
    : _service = service ?? ShiftSwapService();
  final List<ShiftSwapModel> _items = [];
  List<ShiftSwapModel> get items => List.unmodifiable(_items);
  String? userId;
  String direction = 'ALL';
  String? status;
  bool isLoading = false;
  bool isLoadingMore = false;
  String? error;
  String? loadMoreError;
  int totalItems = 0;
  int _page = 0;
  int _totalPages = 0;
  int _generation = 0;
  bool _disposed = false;
  bool get hasMore => _page < _totalPages;
  bool isSent(ShiftSwapModel request) =>
      userId != null &&
      request.requesterId.toLowerCase() == userId!.toLowerCase();

  Future<void> setFilters({required String direction, String? status}) async {
    if (_disposed || (this.direction == direction && this.status == status)) {
      return;
    }
    this.direction = direction;
    this.status = status;
    await refresh();
  }

  Future<void> refresh() async {
    if (_disposed) return;
    final generation = ++_generation;
    _items.clear();
    _page = 0;
    _totalPages = 0;
    totalItems = 0;
    isLoading = true;
    isLoadingMore = false;
    error = null;
    loadMoreError = null;
    notifyListeners();
    try {
      final id = await _service.getCurrentUserId();
      final page = await _service.getRequests(
        direction: direction,
        status: status,
      );
      if (_disposed || generation != _generation) return;
      userId = id;
      _items.addAll(page.items);
      _page = 1;
      _totalPages = page.totalPages;
      totalItems = page.totalItems;
    } catch (e) {
      if (!_disposed && generation == _generation) {
        error = e is String
            ? e
            : 'Couldn’t load swap requests. Please try again.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        isLoading = false;
        notifyListeners();
      }
    }
  }

  Future<void> loadMore() async {
    if (_disposed || isLoading || isLoadingMore || !hasMore) return;
    final generation = _generation;
    isLoadingMore = true;
    loadMoreError = null;
    notifyListeners();
    try {
      final page = await _service.getRequests(
        pageNumber: _page + 1,
        direction: direction,
        status: status,
      );
      if (_disposed || generation != _generation) return;
      final ids = _items.map((r) => r.swapRequestId).toSet();
      _items.addAll(page.items.where((r) => ids.add(r.swapRequestId)));
      _page++;
      _totalPages = page.totalPages;
      totalItems = page.totalItems;
    } catch (e) {
      if (!_disposed && generation == _generation) {
        loadMoreError = e is String ? e : 'Couldn’t load more requests.';
      }
    } finally {
      if (!_disposed && generation == _generation) {
        isLoadingMore = false;
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
