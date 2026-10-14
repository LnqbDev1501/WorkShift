import '../models/product_model.dart';
import 'api_client.dart';

class ProductService {
  final ApiClient _apiClient = ApiClient();

  Future<ProductPagedResponse> getProducts({int pageNumber = 1, int pageSize = 50}) async {
    final response = await _apiClient.get(
      '/api/staff/products',
      queryParameters: {'pageNumber': pageNumber, 'pageSize': pageSize},
    );
    return ProductPagedResponse.fromJson(response.data as Map<String, dynamic>);
  }
}