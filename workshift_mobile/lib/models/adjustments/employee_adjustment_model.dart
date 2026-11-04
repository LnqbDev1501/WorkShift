class EmployeeAdjustmentModel {
  final int rewardPenaltyId;
  final String adjustmentType; // REWARD | PENALTY
  final String userId;
  final String employeeName;
  final int branchId;
  final String branchName;
  final int payrollPeriodId;
  final String periodCode;
  final String periodName;
  final String? periodStart;
  final String? periodEnd;
  final String? periodStatus;
  final String adjustmentName;
  final double amount;
  final String reason;
  final String approvalStatus; // PENDING | APPROVED | REJECTED
  final String? createdByName;
  final DateTime? createdAt;
  final String? approvedByName;
  final DateTime? approvedAt;
  final String? rejectedByName;
  final DateTime? rejectedAt;
  final String? reviewNote;

  EmployeeAdjustmentModel({
    required this.rewardPenaltyId,
    required this.adjustmentType,
    required this.userId,
    required this.employeeName,
    required this.branchId,
    required this.branchName,
    required this.payrollPeriodId,
    required this.periodCode,
    required this.periodName,
    this.periodStart,
    this.periodEnd,
    this.periodStatus,
    required this.adjustmentName,
    required this.amount,
    required this.reason,
    required this.approvalStatus,
    this.createdByName,
    this.createdAt,
    this.approvedByName,
    this.approvedAt,
    this.rejectedByName,
    this.rejectedAt,
    this.reviewNote,
  });

  bool get isReward => adjustmentType.toUpperCase() == 'REWARD';
  bool get isPenalty => adjustmentType.toUpperCase() == 'PENALTY';
  bool get isPending => approvalStatus.toUpperCase() == 'PENDING';
  bool get isApproved => approvalStatus.toUpperCase() == 'APPROVED';
  bool get isRejected => approvalStatus.toUpperCase() == 'REJECTED';

  factory EmployeeAdjustmentModel.fromJson(Map<String, dynamic> json) {
    DateTime? parseDate(dynamic value) {
      if (value == null) return null;
      try {
        return DateTime.parse(value.toString());
      } catch (_) {
        return null;
      }
    }

    return EmployeeAdjustmentModel(
      rewardPenaltyId: (json['rewardPenaltyId'] as num?)?.toInt() ?? 0,
      adjustmentType: json['adjustmentType']?.toString() ?? '',
      userId: json['userId']?.toString() ?? '',
      employeeName: json['employeeName']?.toString() ?? '',
      branchId: (json['branchId'] as num?)?.toInt() ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      payrollPeriodId: (json['payrollPeriodId'] as num?)?.toInt() ?? 0,
      periodCode: json['periodCode']?.toString() ?? '',
      periodName: json['periodName']?.toString() ?? '',
      periodStart: json['periodStart']?.toString(),
      periodEnd: json['periodEnd']?.toString(),
      periodStatus: json['periodStatus']?.toString(),
      adjustmentName: json['adjustmentName']?.toString() ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0.0,
      reason: json['reason']?.toString() ?? '',
      approvalStatus: json['approvalStatus']?.toString() ?? 'PENDING',
      createdByName: json['createdByName']?.toString(),
      createdAt: parseDate(json['createdAt']),
      approvedByName: json['approvedByName']?.toString(),
      approvedAt: parseDate(json['approvedAt']),
      rejectedByName: json['rejectedByName']?.toString(),
      rejectedAt: parseDate(json['rejectedAt']),
      reviewNote: json['reviewNote']?.toString(),
    );
  }
}

class EmployeeAdjustmentPagedResponse {
  final List<EmployeeAdjustmentModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;

  EmployeeAdjustmentPagedResponse({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
  });

  factory EmployeeAdjustmentPagedResponse.fromJson(Map<String, dynamic> json) {
    final rawList = json['items'] as List<dynamic>? ?? [];
    return EmployeeAdjustmentPagedResponse(
      items: rawList
          .map(
            (e) => EmployeeAdjustmentModel.fromJson(e as Map<String, dynamic>),
          )
          .toList(),
      pageNumber: (json['pageNumber'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalItems: (json['totalItems'] as num?)?.toInt() ?? 0,
    );
  }
}
