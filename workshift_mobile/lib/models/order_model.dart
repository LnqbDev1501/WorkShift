/// Order Item model (matches backend OrderItemResponse)
class OrderItemModel {
  final int orderItemId;
  final int productId;
  final String productName;
  final double unitPrice;
  final int quantity;
  final double discountAmount;
  final double totalAmount;
  final String? note;

  OrderItemModel({
    required this.orderItemId,
    required this.productId,
    required this.productName,
    required this.unitPrice,
    required this.quantity,
    required this.discountAmount,
    required this.totalAmount,
    this.note,
  });

  factory OrderItemModel.fromJson(Map<String, dynamic> json) {
    return OrderItemModel(
      orderItemId: (json['orderItemId'] as num?)?.toInt() ?? 0,
      productId: (json['productId'] as num?)?.toInt() ?? 0,
      productName: json['productName']?.toString() ?? '',
      unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0.0,
      quantity: (json['quantity'] as num?)?.toInt() ?? 0,
      discountAmount: (json['discountAmount'] as num?)?.toDouble() ?? 0.0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      note: json['note']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'orderItemId': orderItemId,
      'productId': productId,
      'productName': productName,
      'unitPrice': unitPrice,
      'quantity': quantity,
      'discountAmount': discountAmount,
      'totalAmount': totalAmount,
      'note': note,
    };
  }
}

/// Order model (matches backend OrderResponse)
class OrderModel {
  final int orderId;
  final String orderNumber;
  final int branchId;
  final String branchName;
  final String createdBy;
  final String createdByName;
  final DateTime orderDate;
  final double subtotal;
  final double totalAmount;
  final String paymentStatus;
  final DateTime? paidAt;
  final String status;
  final String? note;
  final DateTime createdAt;
  final List<OrderItemModel> items;
  final List<PaymentModel> payments;

  OrderModel({
    required this.orderId,
    required this.orderNumber,
    required this.branchId,
    required this.branchName,
    required this.createdBy,
    required this.createdByName,
    required this.orderDate,
    required this.subtotal,
    required this.totalAmount,
    required this.paymentStatus,
    this.paidAt,
    required this.status,
    this.note,
    required this.createdAt,
    required this.items,
    this.payments = const [],
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) {
    return OrderModel(
      orderId: (json['orderId'] as num?)?.toInt() ?? 0,
      orderNumber: json['orderNumber']?.toString() ?? '',
      branchId: (json['branchId'] as num?)?.toInt() ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      createdBy: json['createdBy']?.toString() ?? '',
      createdByName: json['createdByName']?.toString() ?? '',
      orderDate:
          DateTime.tryParse(json['orderDate']?.toString() ?? '') ??
          DateTime.now(),
      subtotal: (json['subtotal'] as num?)?.toDouble() ?? 0.0,
      totalAmount: (json['totalAmount'] as num?)?.toDouble() ?? 0.0,
      paymentStatus: json['paymentStatus']?.toString() ?? '',
      paidAt: json['paidAt'] != null
          ? DateTime.tryParse(json['paidAt'].toString())
          : null,
      status: json['status']?.toString() ?? '',
      note: json['note']?.toString(),
      createdAt:
          DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
          DateTime.now(),
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => OrderItemModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      payments:
          (json['payments'] as List<dynamic>?)
              ?.map((e) => PaymentModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }
}

/// Payment model (matches backend PaymentResponse)
class PaymentModel {
  final int paymentId;
  final int orderId;
  final double amount;
  final String paymentMethod;
  final String? referenceNumber;
  final String? note;
  final String recordedBy;
  final String recordedByName;
  final DateTime recordedAt;

  PaymentModel({
    required this.paymentId,
    required this.orderId,
    required this.amount,
    required this.paymentMethod,
    this.referenceNumber,
    this.note,
    required this.recordedBy,
    required this.recordedByName,
    required this.recordedAt,
  });

  factory PaymentModel.fromJson(Map<String, dynamic> json) {
    return PaymentModel(
      paymentId: (json['paymentId'] as num?)?.toInt() ?? 0,
      orderId: (json['orderId'] as num?)?.toInt() ?? 0,
      amount: (json['amount'] as num?)?.toDouble() ?? 0.0,
      paymentMethod: json['paymentMethod']?.toString() ?? '',
      referenceNumber: json['referenceNumber']?.toString(),
      note: json['note']?.toString(),
      recordedBy: json['recordedBy']?.toString() ?? '',
      recordedByName: json['recordedByName']?.toString() ?? '',
      recordedAt:
          DateTime.tryParse(json['recordedAt']?.toString() ?? '') ??
          DateTime.now(),
    );
  }
}

/// Request to create an order
class CreateOrderRequest {
  final List<CreateOrderItemRequest> items;
  final String? note;

  CreateOrderRequest({required this.items, this.note});

  Map<String, dynamic> toJson() {
    return {
      'items': items.map((e) => e.toJson()).toList(),
      'note': note?.trim(),
    };
  }
}

class CreateOrderItemRequest {
  final int productId;
  final int quantity;
  final double discountAmount;
  final String? note;

  CreateOrderItemRequest({
    required this.productId,
    required this.quantity,
    this.discountAmount = 0,
    this.note,
  });

  Map<String, dynamic> toJson() {
    return {
      'productId': productId,
      'quantity': quantity,
      'discountAmount': discountAmount,
      'note': note?.trim(),
    };
  }
}

/// Request to update an order
class UpdateOrderRequest {
  final List<UpdateOrderItemRequest> items;
  final String? note;

  UpdateOrderRequest({required this.items, this.note});

  Map<String, dynamic> toJson() {
    return {
      'items': items.map((e) => e.toJson()).toList(),
      'note': note?.trim(),
    };
  }
}

class UpdateOrderItemRequest {
  final int? orderItemId;
  final int productId;
  final int quantity;
  final double discountAmount;
  final String? note;

  UpdateOrderItemRequest({
    this.orderItemId,
    required this.productId,
    required this.quantity,
    this.discountAmount = 0,
    this.note,
  });

  Map<String, dynamic> toJson() {
    return {
      'orderItemId': orderItemId,
      'productId': productId,
      'quantity': quantity,
      'discountAmount': discountAmount,
      'note': note?.trim(),
    };
  }
}

/// Request to record payment
class RecordPaymentRequest {
  final double amount;
  final String paymentMethod;
  final String? referenceNumber;
  final String? note;

  RecordPaymentRequest({
    required this.amount,
    required this.paymentMethod,
    this.referenceNumber,
    this.note,
  });

  Map<String, dynamic> toJson() {
    return {
      'amount': amount,
      'paymentMethod': paymentMethod,
      'referenceNumber': referenceNumber,
      'note': note?.trim(),
    };
  }
}

/// Query for listing orders
class OrderListQuery {
  final String? search;
  final int? branchId;
  final String? status;
  final String? paymentStatus;
  final DateTime? dateFrom;
  final DateTime? dateTo;
  final int pageNumber;
  final int pageSize;

  OrderListQuery({
    this.search,
    this.branchId,
    this.status,
    this.paymentStatus,
    this.dateFrom,
    this.dateTo,
    this.pageNumber = 1,
    this.pageSize = 20,
  });

  Map<String, dynamic> toQueryParameters() {
    final params = <String, dynamic>{};
    if (search != null && search!.isNotEmpty) params['search'] = search;
    if (branchId != null) params['branchId'] = branchId;
    if (status != null && status!.isNotEmpty) params['status'] = status;
    if (paymentStatus != null && paymentStatus!.isNotEmpty) {
      params['paymentStatus'] = paymentStatus;
    }
    if (dateFrom != null) params['dateFrom'] = dateFrom!.toIso8601String();
    if (dateTo != null) params['dateTo'] = dateTo!.toIso8601String();
    params['pageNumber'] = pageNumber;
    params['pageSize'] = pageSize;
    return params;
  }
}

/// Paged response for orders
class OrderPagedResponse {
  final List<OrderModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;

  OrderPagedResponse({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
  });

  factory OrderPagedResponse.fromJson(Map<String, dynamic> json) {
    return OrderPagedResponse(
      items:
          (json['items'] as List<dynamic>?)
              ?.map((e) => OrderModel.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      pageNumber: (json['pageNumber'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalItems: (json['totalItems'] as num?)?.toInt() ?? 0,
    );
  }
}
