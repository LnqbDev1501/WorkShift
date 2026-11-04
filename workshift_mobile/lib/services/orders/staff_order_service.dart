import '../../models/orders/order_model.dart';
import '../core/api_client.dart';

/// Service for order-related API calls (Staff role)
class StaffOrderService {
  final ApiClient _apiClient = ApiClient();

  /// Create a new order: POST /api/staff/orders
  Future<OrderModel> createOrder(CreateOrderRequest request) async {
    final response = await _apiClient.post(
      '/api/staff/orders',
      data: request.toJson(),
    );
    return OrderModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Get my orders: GET /api/staff/orders
  Future<OrderPagedResponse> getMyOrders(OrderListQuery query) async {
    final response = await _apiClient.get(
      '/api/staff/orders',
      queryParameters: query.toQueryParameters(),
    );
    return OrderPagedResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Get order detail: GET /api/staff/orders/{id}
  Future<OrderModel> getOrder(int id) async {
    final response = await _apiClient.get('/api/staff/orders/$id');
    return OrderModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Complete an order: POST /api/staff/orders/{id}/complete
  Future<void> completeOrder(int id) async {
    await _apiClient.post('/api/staff/orders/$id/complete');
  }

  /// Cancel an order: POST /api/staff/orders/{id}/cancel
  Future<void> cancelOrder(int id) async {
    await _apiClient.post('/api/staff/orders/$id/cancel');
  }
}
