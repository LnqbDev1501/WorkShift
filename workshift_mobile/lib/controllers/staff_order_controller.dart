import 'package:flutter/material.dart';
import '../models/order_model.dart';
import '../services/staff_order_service.dart';

/// Controller for Staff Order management
class StaffOrderController extends ChangeNotifier {
  final StaffOrderService _orderService = StaffOrderService();

  bool _isLoading = false;
  String? _errorMessage;
  OrderPagedResponse? _ordersResponse;
  OrderModel? _selectedOrder;

  // Getters
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  OrderPagedResponse? get ordersResponse => _ordersResponse;
  List<OrderModel> get orders => _ordersResponse?.items ?? [];
  OrderModel? get selectedOrder => _selectedOrder;
  bool get hasMore => _ordersResponse != null && 
      _ordersResponse!.pageNumber * _ordersResponse!.pageSize < _ordersResponse!.totalItems;

  /// Load orders with optional query
  Future<void> loadOrders({OrderListQuery? query, bool refresh = false}) async {
    if (refresh) {
      _ordersResponse = null;
    }
    
    if (_isLoading) return;
    
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final queryToUse = query ?? OrderListQuery(pageNumber: (_ordersResponse?.pageNumber ?? 0) + 1);
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
      // Refresh list
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