class CategoryModel {
  final int categoryId;
  final String categoryName;
  final String? description;
  final String? status;
  final int? productCount;

  CategoryModel({
    required this.categoryId,
    required this.categoryName,
    this.description,
    this.status,
    this.productCount,
  });

  factory CategoryModel.fromJson(Map<String, dynamic> json) {
    return CategoryModel(
      categoryId: (json['categoryId'] as num?)?.toInt() ?? 0,
      categoryName: json['categoryName']?.toString() ?? '',
      description: json['description']?.toString(),
      status: json['status']?.toString() ?? 'ACTIVE',
      productCount: (json['productCount'] as num?)?.toInt(),
    );
  }
}

class CategoryPagedResponse {
  final List<CategoryModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;

  CategoryPagedResponse({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
  });

  factory CategoryPagedResponse.fromJson(Map<String, dynamic> json) {
    final rawList = json['items'] as List<dynamic>? ?? [];
    return CategoryPagedResponse(
      items: rawList
          .map((e) => CategoryModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      pageNumber: (json['pageNumber'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalItems: (json['totalItems'] as num?)?.toInt() ?? 0,
    );
  }
}
