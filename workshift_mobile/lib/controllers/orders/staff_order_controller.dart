import 'package:flutter/material.dart';
import '../../models/orders/order_model.dart';
import '../../services/orders/staff_order_service.dart';

/// Controller for Staff Order management
class StaffOrderController extends ChangeNotifier {
  final StaffOrderService _orderService = StaffOrderService();

  bool _isLoading = false;
  String? _errorMessage;
  OrderPagedResponse? _ordersResponse;
  OrderModel? _selectedOrder;
  OrderListQuery? _currentQuery;

  // Getters
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  OrderPagedResponse? get ordersResponse => _ordersResponse;
  OrderListQuery? get currentQuery => _currentQuery;

  /// Orders list: Pending orders first (FIFO - oldest first), completed/cancelled after (newest first)
  List<OrderModel> get orders {
    final list = List<OrderModel>.from(_ordersResponse?.items ?? []);
    list.sort((a, b) {
      final aPending = a.status.toUpperCase() == 'PENDING';
      final bPending = b.status.toUpperCase() == 'PENDING';
      if (aPending && !bPending) return -1;
      if (!aPending && bPending) return 1;
      if (aPending && bPending) {
        // Pending: Oldest order first (FIFO to prepare and deliver first)
        return a.orderDate.compareTo(b.orderDate);
      }
      // Completed / Cancelled: Newest order first
      return b.orderDate.compareTo(a.orderDate);
    });
    return list;
  }

  OrderModel? get selectedOrder => _selectedOrder;
  bool get hasMore =>
      _ordersResponse != null &&
      _ordersResponse!.pageNumber * _ordersResponse!.pageSize <
          _ordersResponse!.totalItems;

  /// Load orders with optional query
  Future<void> loadOrders({OrderListQuery? query, bool refresh = false}) async {
    if (query != null) {
      _currentQuery = query;
    }

    if (refresh) {
      _ordersResponse = null;
    }

    if (_isLoading) return;

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final base = _currentQuery;
      final nextPage = (_ordersResponse?.pageNumber ?? 0) + 1;
      final targetPage =
          refresh ? 1 : (query != null ? query.pageNumber : nextPage);

      final queryToUse = OrderListQuery(
        search: base?.search,
        branchId: base?.branchId,
        status: base?.status,
        paymentStatus: base?.paymentStatus,
        dateFrom: base?.dateFrom,
        dateTo: base?.dateTo,
        sortBy: base?.sortBy ?? 'oldest_pending_first',
        pageNumber: targetPage,
        pageSize: base?.pageSize ?? 20,
      );

      final response = await _orderService.getMyOrders(queryToUse);

      if (refresh || _ordersResponse == null) {
        _ordersResponse = response;
      } else {
        // Append for pagination
        _ordersResponse = OrderPagedResponse(
          items: [..._ordersResponse!.items, ...response.items],
          pageNumber: response.pageNumber,
          pageSize: response.pageSize,
          totalItems: response.totalItems,
        );
      }

      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Load order detail
  Future<void> loadOrderDetail(int id) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _selectedOrder = await _orderService.getOrder(id);
      _isLoading = false;
      notifyListeners();
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Create new order
  Future<bool> createOrder(CreateOrderRequest request) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final order = await _orderService.createOrder(request);
      _selectedOrder = order;
      _isLoading = false;
      // Refresh list with current filter
      await loadOrders(refresh: true);
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Complete order
  Future<bool> completeOrder(int id) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _orderService.completeOrder(id);
      // Refresh both list and detail
      await loadOrders(refresh: true);
      if (_selectedOrder?.orderId == id) {
        await loadOrderDetail(id);
      }
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Cancel order
  Future<bool> cancelOrder(int id) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _orderService.cancelOrder(id);
      // Refresh both list and detail
      await loadOrders(refresh: true);
      if (_selectedOrder?.orderId == id) {
        await loadOrderDetail(id);
      }
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  void clearError() {
    if (_errorMessage != null) {
      _errorMessage = null;
      notifyListeners();
    }
  }

  void clearSelectedOrder() {
    _selectedOrder = null;
    notifyListeners();
  }
}
