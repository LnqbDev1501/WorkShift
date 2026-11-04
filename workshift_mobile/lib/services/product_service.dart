import '../models/category_model.dart';
import '../models/product_model.dart';
import 'api_client.dart';

class ProductService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách sản phẩm theo chi nhánh của Staff
  Future<ProductPagedResponse> getProducts({
    int pageNumber = 1,
    int pageSize = 20,
    int? categoryId,
    String? search,
  }) async {
    final query = <String, dynamic>{
      'pageNumber': pageNumber,
      'pageSize': pageSize,
    };
    if (categoryId != null && categoryId > 0) {
      query['categoryId'] = categoryId;
    }
    if (search != null && search.trim().isNotEmpty) {
      query['search'] = search.trim();
    }

    final response = await _apiClient.get(
      '/api/staff/products',
      queryParameters: query,
    );
    return ProductPagedResponse.fromJson(response.data as Map<String, dynamic>);
  }

  /// Lấy chi tiết sản phẩm theo ID (thuộc chi nhánh của Staff)
  Future<ProductModel> getProductDetail(int id) async {
    final response = await _apiClient.get('/api/staff/products/$id');
    return ProductModel.fromJson(response.data as Map<String, dynamic>);
  }

  /// Lấy tất cả Category (dùng chung toàn hệ thống)
  Future<CategoryPagedResponse> getCategories({
    int pageNumber = 1,
    int pageSize = 100,
    String? search,
  }) async {
    final query = <String, dynamic>{
      'pageNumber': pageNumber,
      'pageSize': pageSize,
      'status': 'ACTIVE',
    };
    if (search != null && search.trim().isNotEmpty) {
      query['search'] = search.trim();
    }

    final response = await _apiClient.get(
      '/api/categories',
      queryParameters: query,
    );
    return CategoryPagedResponse.fromJson(
      response.data as Map<String, dynamic>,
    );
  }
}
