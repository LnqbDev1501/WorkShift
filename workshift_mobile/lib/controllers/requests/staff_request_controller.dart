import 'dart:async';
import 'package:flutter/material.dart';
import '../../models/requests/staff_request_model.dart';
import '../../services/requests/staff_request_service.dart';

/// Controller managing business logic and UI state for Staff Requests
class StaffRequestController extends ChangeNotifier {
  final StaffRequestService _service;

  StaffRequestController({StaffRequestService? service})
    : _service = service ?? StaffRequestService();

  // List state
  final List<StaffEmployeeRequestListItemModel> _requests = [];
  bool _isLoading = false;
  bool _isLoadingMore = false;
  String? _errorMessage;

  int _pageNumber = 1;
  final int _pageSize = 10;
  int _totalPages = 1;
  int _totalItems = 0;

  // Search & Filter state
  String _searchQuery = '';
  String _selectedRequestType = 'ALL';
  String _selectedStatus = 'ALL';
  DateTime? _fromDate;
  DateTime? _toDate;

  // Detail & Action state
  StaffEmployeeRequestDetailModel? _selectedDetail;
  bool _isLoadingDetail = false;
  bool _isCancelling = false;
  bool _isSubmitting = false;
  String? _actionErrorMessage;

  // Eligible shifts state (for creating new request)
  List<StaffEligibleShiftModel> _eligibleShifts = [];
  bool _isLoadingEligibleShifts = false;

  // Getters
  List<StaffEmployeeRequestListItemModel> get requests =>
      List.unmodifiable(_requests);
  bool get isLoading => _isLoading;
  bool get isLoadingMore => _isLoadingMore;
  String? get errorMessage => _errorMessage;
  bool get hasMore => _pageNumber < _totalPages;
  int get pageNumber => _pageNumber;
  int get totalItems => _totalItems;
  int get totalPages => _totalPages;

  String get searchQuery => _searchQuery;
  String get selectedRequestType => _selectedRequestType;
  String get selectedStatus => _selectedStatus;
  DateTime? get fromDate => _fromDate;
  DateTime? get toDate => _toDate;
  bool get hasActiveFilters =>
      _searchQuery.isNotEmpty ||
      _selectedRequestType != 'ALL' ||
      _selectedStatus != 'ALL' ||
      _fromDate != null ||
      _toDate != null;

  StaffEmployeeRequestDetailModel? get selectedDetail => _selectedDetail;
  bool get isLoadingDetail => _isLoadingDetail;
  bool get isCancelling => _isCancelling;
  bool get isSubmitting => _isSubmitting;
  String? get actionErrorMessage => _actionErrorMessage;

  List<StaffEligibleShiftModel> get eligibleShifts =>
      List.unmodifiable(_eligibleShifts);
  bool get isLoadingEligibleShifts => _isLoadingEligibleShifts;

  /// Load first page of requests list
  Future<void> loadRequests({bool refresh = false}) async {
    if (!refresh) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    _pageNumber = 1;

    try {
      final result = await _service.getRequests(
        reason: _searchQuery.isEmpty ? null : _searchQuery,
        requestType: _selectedRequestType == 'ALL'
            ? null
            : _selectedRequestType,
        status: _selectedStatus == 'ALL' ? null : _selectedStatus,
        fromDate: _fromDate,
        toDate: _toDate,
        pageNumber: _pageNumber,
        pageSize: _pageSize,
      );

      _requests.clear();
      _requests.addAll(result.items);
      _totalPages = result.totalPages;
      _totalItems = result.totalItems;
      _errorMessage = null;
    } catch (e) {
      _errorMessage = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Load next page (Load more / Infinite scroll)
  Future<void> loadMore() async {
    if (_isLoadingMore || !hasMore || _isLoading) return;

    _isLoadingMore = true;
    notifyListeners();

    final nextPage = _pageNumber + 1;

    try {
      final result = await _service.getRequests(
        reason: _searchQuery.isEmpty ? null : _searchQuery,
        requestType: _selectedRequestType == 'ALL'
            ? null
            : _selectedRequestType,
        status: _selectedStatus == 'ALL' ? null : _selectedStatus,
        fromDate: _fromDate,
        toDate: _toDate,
        pageNumber: nextPage,
        pageSize: _pageSize,
      );

      _pageNumber = nextPage;
      _requests.addAll(result.items);
      _totalPages = result.totalPages;
      _totalItems = result.totalItems;
    } catch (e) {
      // Error during load more
    } finally {
      _isLoadingMore = false;
      notifyListeners();
    }
  }

  /// Set keyword search by reason
  Future<void> setSearchQuery(String query) async {
    if (_searchQuery == query) return;
    _searchQuery = query;
    await loadRequests();
  }

  /// Quick filter by status (Tab or Chip: PENDING, APPROVED, REJECTED, CANCELLED, ALL)
  Future<void> setStatusFilter(String status) async {
    if (_selectedStatus == status) return;
    _selectedStatus = status;
    await loadRequests();
  }

  /// Set advanced filter
  Future<void> applyAdvancedFilters({
    String? requestType,
    String? status,
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    if (requestType != null) _selectedRequestType = requestType;
    if (status != null) _selectedStatus = status;
    _fromDate = fromDate;
    _toDate = toDate;
    await loadRequests();
  }

  /// Reset all filters
  Future<void> resetFilters() async {
    _searchQuery = '';
    _selectedRequestType = 'ALL';
    _selectedStatus = 'ALL';
    _fromDate = null;
    _toDate = null;
    await loadRequests();
  }

  /// Load details of a single request
  Future<void> loadRequestDetail(int requestId) async {
    _isLoadingDetail = true;
    _actionErrorMessage = null;
    _selectedDetail = null;
    notifyListeners();

    try {
      _selectedDetail = await _service.getRequestDetail(requestId);
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
    } finally {
      _isLoadingDetail = false;
      notifyListeners();
    }
  }

  /// Cancel employee request
  Future<bool> cancelRequest(int requestId) async {
    _isCancelling = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final updated = await _service.cancelRequest(requestId);

      // Optimistically update item in list
      final index = _requests.indexWhere((r) => r.requestId == requestId);
      if (index != -1) {
        final old = _requests[index];
        _requests[index] = StaffEmployeeRequestListItemModel(
          requestId: old.requestId,
          userId: old.userId,
          branchId: old.branchId,
          branchName: old.branchName,
          branchCode: old.branchCode,
          requestType: old.requestType,
          reason: old.reason,
          status: 'CANCELLED',
          createdAt: old.createdAt,
          reviewedBy: updated.reviewedBy,
          reviewerFullName: updated.reviewerFullName,
          reviewedAt: updated.reviewedAt,
          reviewNote: updated.reviewNote,
          shiftSummary: old.shiftSummary,
        );
      }

      // Update selectedDetail if currently open
      if (_selectedDetail != null && _selectedDetail!.requestId == requestId) {
        _selectedDetail = updated;
      }

      _isCancelling = false;
      notifyListeners();
      return true;
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
      _isCancelling = false;
      notifyListeners();
      return false;
    }
  }

  /// Load list of shifts eligible for request creation
  Future<void> loadEligibleShifts(String requestType) async {
    if (requestType == 'SALARY_INCREASE') {
      _isLoadingEligibleShifts = false;
      _eligibleShifts = [];
      _actionErrorMessage = null;
      notifyListeners();
      return;
    }

    _isLoadingEligibleShifts = true;
    _eligibleShifts = [];
    _actionErrorMessage = null;
    notifyListeners();

    try {
      _eligibleShifts = await _service.getEligibleShifts(requestType);
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
    } finally {
      _isLoadingEligibleShifts = false;
      notifyListeners();
    }
  }

  /// Submit leave request (LEAVE)
  Future<StaffEmployeeRequestDetailModel?> submitLeaveRequest({
    required List<int> assignmentIds,
    required String reason,
  }) async {
    _isSubmitting = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final result = await _service.submitLeaveRequest(
        assignmentIds: assignmentIds,
        reason: reason,
      );
      _isSubmitting = false;
      // Reload list after creating new request
      await loadRequests(refresh: true);
      return result;
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
      _isSubmitting = false;
      notifyListeners();
      return null;
    }
  }

  /// Submit late arrival request (LATE_ARRIVAL)
  Future<StaffEmployeeRequestDetailModel?> submitLateArrivalRequest({
    required int assignmentId,
    required DateTime requestedTime,
    required String reason,
  }) async {
    _isSubmitting = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final result = await _service.submitLateArrivalRequest(
        assignmentId: assignmentId,
        requestedTime: requestedTime,
        reason: reason,
      );
      _isSubmitting = false;
      await loadRequests(refresh: true);
      return result;
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
      _isSubmitting = false;
      notifyListeners();
      return null;
    }
  }

  /// Submit early leave request (EARLY_LEAVE)
  Future<StaffEmployeeRequestDetailModel?> submitEarlyLeaveRequest({
    required int assignmentId,
    required DateTime requestedTime,
    required String reason,
  }) async {
    _isSubmitting = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final result = await _service.submitEarlyLeaveRequest(
        assignmentId: assignmentId,
        requestedTime: requestedTime,
        reason: reason,
      );
      _isSubmitting = false;
      await loadRequests(refresh: true);
      return result;
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
      _isSubmitting = false;
      notifyListeners();
      return null;
    }
  }

  /// Submit salary increase request (SALARY_INCREASE)
  Future<StaffEmployeeRequestDetailModel?> submitSalaryIncreaseRequest({
    required double proposedHourlyRate,
    required String reason,
  }) async {
    _isSubmitting = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final result = await _service.submitSalaryIncreaseRequest(
        proposedHourlyRate: proposedHourlyRate,
        reason: reason,
      );
      _isSubmitting = false;
      await loadRequests(refresh: true);
      return result;
    } catch (e) {
      _actionErrorMessage = _cleanErrorMessage(e);
      _isSubmitting = false;
      notifyListeners();
      return null;
    }
  }

  String _cleanErrorMessage(dynamic e) {
    var msg = e.toString();
    if (msg.startsWith('Exception: ')) {
      msg = msg.substring('Exception: '.length);
    }
    return msg.trim();
  }
}
