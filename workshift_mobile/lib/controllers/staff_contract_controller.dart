import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../models/staff_contract_model.dart';
import '../services/staff_contract_service.dart';

/// Controller quản lý state và nghiệp vụ cho Hợp đồng lao động của Staff
class StaffContractController extends ChangeNotifier {
  final StaffContractService _service;

  StaffContractController({StaffContractService? service})
      : _service = service ?? StaffContractService();

  // Danh sách hợp đồng
  final List<StaffContractListItemModel> _contracts = [];
  bool _isLoading = false;
  String? _errorMessage;

  // Search & Filter
  String _searchQuery = '';
  String _selectedStatus = 'ALL';
  DateTime? _startDateFrom;
  DateTime? _startDateTo;

  // Chi tiết hợp đồng
  StaffContractDetailModel? _selectedContract;
  bool _isLoadingDetail = false;
  bool _isAccepting = false;
  bool _isSubmittingChangeRequest = false;
  String? _actionErrorMessage;

  // Lịch sử yêu cầu thay đổi
  List<StaffContractChangeRequestModel> _changeRequests = [];
  bool _isLoadingChangeRequests = false;

  // PDF
  bool _isDownloadingPdf = false;

  // Getters
  List<StaffContractListItemModel> get contracts => List.unmodifiable(_contracts);
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  String get searchQuery => _searchQuery;
  String get selectedStatus => _selectedStatus;
  DateTime? get startDateFrom => _startDateFrom;
  DateTime? get startDateTo => _startDateTo;
  bool get hasActiveFilters =>
      _searchQuery.isNotEmpty ||
      _selectedStatus != 'ALL' ||
      _startDateFrom != null ||
      _startDateTo != null;

  StaffContractDetailModel? get selectedContract => _selectedContract;
  bool get isLoadingDetail => _isLoadingDetail;
  bool get isAccepting => _isAccepting;
  bool get isSubmittingChangeRequest => _isSubmittingChangeRequest;
  String? get actionErrorMessage => _actionErrorMessage;

  List<StaffContractChangeRequestModel> get changeRequests =>
      List.unmodifiable(_changeRequests);
  bool get isLoadingChangeRequests => _isLoadingChangeRequests;
  bool get isDownloadingPdf => _isDownloadingPdf;

  /// Nạp danh sách hợp đồng
  Future<void> loadContracts({bool refresh = false}) async {
    if (!refresh) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final list = await _service.getMyContracts(
        status: _selectedStatus == 'ALL' ? null : _selectedStatus,
        search: _searchQuery.isEmpty ? null : _searchQuery,
        startDateFrom: _startDateFrom,
        startDateTo: _startDateTo,
      );

      _contracts.clear();
      _contracts.addAll(list);
      _errorMessage = null;
    } catch (e) {
      _errorMessage = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Đặt từ khóa tìm kiếm (Search View Employment Contract List for Staff)
  Future<void> setSearchQuery(String query) async {
    if (_searchQuery == query) return;
    _searchQuery = query;
    await loadContracts();
  }

  /// Lọc nhanh theo trạng thái (Filter View Employment Contract List for Staff)
  Future<void> setStatusFilter(String status) async {
    if (_selectedStatus == status) return;
    _selectedStatus = status;
    await loadContracts();
  }

  /// Đặt khoảng ngày bắt đầu hợp đồng
  Future<void> setDateRange(DateTime? from, DateTime? to) async {
    _startDateFrom = from;
    _startDateTo = to;
    await loadContracts();
  }

  /// Đặt lại toàn bộ bộ lọc
  Future<void> resetFilters() async {
    _searchQuery = '';
    _selectedStatus = 'ALL';
    _startDateFrom = null;
    _startDateTo = null;
    await loadContracts();
  }

  /// Nạp chi tiết một hợp đồng (View Employment Contract Details for Staff)
  Future<void> loadContractDetail(int contractId) async {
    _isLoadingDetail = true;
    _actionErrorMessage = null;
    _selectedContract = null;
    notifyListeners();

    try {
      _selectedContract = await _service.getMyContractById(contractId);
      // Nạp kèm lịch sử các yêu cầu thay đổi (nếu có)
      await loadChangeRequests(contractId);
    } catch (e) {
      _actionErrorMessage = e.toString();
    } finally {
      _isLoadingDetail = false;
      notifyListeners();
    }
  }

  /// Ký / Chấp nhận hợp đồng (Accept Employment Contract)
  Future<bool> acceptContract(int contractId) async {
    _isAccepting = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final updated = await _service.acceptContract(contractId);
      _selectedContract = updated;

      // Cập nhật lại trong danh sách
      final index = _contracts.indexWhere((c) => c.contractId == contractId);
      if (index != -1) {
        _contracts[index] = StaffContractListItemModel(
          contractId: updated.contractId,
          contractNumber: updated.contractNumber,
          status: updated.status,
          startDate: updated.startDate,
          endDate: updated.endDate,
          hourlyRate: updated.hourlyRate,
          employeePosition: updated.employeePosition,
          branchId: updated.branchId,
          branchName: updated.branchName,
          regionId: updated.regionId,
          regionName: updated.regionName,
          createdAt: updated.createdAt,
          sentToStaffAt: updated.sentToStaffAt,
          staffAcceptedAt: updated.staffAcceptedAt,
          hasPdf: updated.hasPdf,
        );
      }

      _isAccepting = false;
      notifyListeners();
      return true;
    } catch (e) {
      _actionErrorMessage = e.toString();
      _isAccepting = false;
      notifyListeners();
      return false;
    }
  }

  /// Gửi yêu cầu thay đổi thông tin hợp đồng (Submit Employment Contract Change Request)
  Future<bool> submitChangeRequest({
    required int contractId,
    String? staffComment,
    required List<ContractChangeRequestItemInputModel> items,
  }) async {
    _isSubmittingChangeRequest = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      await _service.submitChangeRequest(
        contractId: contractId,
        staffComment: staffComment,
        items: items,
      );

      // Nạp lại chi tiết hợp đồng để cập nhật trạng thái Change Request
      await loadContractDetail(contractId);
      await loadContracts(refresh: true);

      _isSubmittingChangeRequest = false;
      notifyListeners();
      return true;
    } catch (e) {
      _actionErrorMessage = e.toString();
      _isSubmittingChangeRequest = false;
      notifyListeners();
      return false;
    }
  }

  /// Nạp lịch sử yêu cầu thay đổi của hợp đồng
  Future<void> loadChangeRequests(int contractId) async {
    _isLoadingChangeRequests = true;
    notifyListeners();

    try {
      _changeRequests = await _service.getMyContractChangeRequests(contractId);
    } catch (_) {
      _changeRequests = [];
    } finally {
      _isLoadingChangeRequests = false;
      notifyListeners();
    }
  }

  /// Lấy dữ liệu file PDF của hợp đồng (View and Download Employment Contract PDF)
  Future<Uint8List?> fetchPdfBytes(int contractId, {bool isDownload = false}) async {
    _isDownloadingPdf = true;
    _actionErrorMessage = null;
    notifyListeners();

    try {
      final bytes = await _service.getPdfBytes(contractId, isDownload: isDownload);
      _isDownloadingPdf = false;
      notifyListeners();
      return bytes;
    } catch (e) {
      _actionErrorMessage = e.toString();
      _isDownloadingPdf = false;
      notifyListeners();
      return null;
    }
  }
}
