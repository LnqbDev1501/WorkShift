class ProductModel {
  final int productId;
  final int branchId;
  final String branchName;
  final int categoryId;
  final String categoryName;
  final String productCode;
  final String productName;
  final double price;
  final String? description;
  final String? imageUrl;
  final String status;

  ProductModel({
    required this.productId,
    required this.branchId,
    required this.branchName,
    required this.categoryId,
    required this.categoryName,
    required this.productCode,
    required this.productName,
    required this.price,
    this.description,
    this.imageUrl,
    required this.status,
  });

  factory ProductModel.fromJson(Map<String, dynamic> json) {
    return ProductModel(
      productId: (json['productId'] as num?)?.toInt() ?? 0,
      branchId: (json['branchId'] as num?)?.toInt() ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      categoryId: (json['categoryId'] as num?)?.toInt() ?? 0,
      categoryName: json['categoryName']?.toString() ?? '',
      productCode: json['productCode']?.toString() ?? '',
      productName: json['productName']?.toString() ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0.0,
      description: json['description']?.toString(),
      imageUrl: json['imageUrl']?.toString(),
      status: json['status']?.toString() ?? '',
    );
  }
}

class ProductPagedResponse {
  final List<ProductModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;

  ProductPagedResponse({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
  });

  factory ProductPagedResponse.fromJson(Map<String, dynamic> json) {
    return ProductPagedResponse(
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => ProductModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      pageNumber: (json['pageNumber'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 10,
      totalItems: (json['totalItems'] as num?)?.toInt() ?? 0,
    );
  }
}
