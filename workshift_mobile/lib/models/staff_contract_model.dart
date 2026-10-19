import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Model tóm tắt hợp đồng lao động trong danh sách (GET /api/staff/contracts)
class StaffContractListItemModel {
  final int contractId;
  final String contractNumber;
  final String status;
  final DateTime startDate;
  final DateTime? endDate;
  final double hourlyRate;
  final String employeeFullName;
  final String employeePosition;
  final int branchId;
  final String branchName;
  final int regionId;
  final String regionName;
  final DateTime createdAt;
  final DateTime? sentToStaffAt;
  final DateTime? staffAcceptedAt;
  final bool hasPdf;

  StaffContractListItemModel({
    required this.contractId,
    required this.contractNumber,
    required this.status,
    required this.startDate,
    this.endDate,
    required this.hourlyRate,
    this.employeeFullName = '',
    required this.employeePosition,
    required this.branchId,
    required this.branchName,
    required this.regionId,
    required this.regionName,
    required this.createdAt,
    this.sentToStaffAt,
    this.staffAcceptedAt,
    required this.hasPdf,
  });

  factory StaffContractListItemModel.fromJson(Map<String, dynamic> json) {
    return StaffContractListItemModel(
      contractId: json['contractId'] as int? ?? 0,
      contractNumber: json['contractNumber']?.toString() ?? '',
      status: json['status']?.toString() ?? 'DRAFT',
      startDate: json['startDate'] != null
          ? DateTime.tryParse(json['startDate'].toString()) ?? DateTime.now()
          : DateTime.now(),
      endDate: json['endDate'] != null
          ? DateTime.tryParse(json['endDate'].toString())
          : null,
      hourlyRate: (json['hourlyRate'] as num?)?.toDouble() ?? 0.0,
      employeeFullName: json['employeeFullName']?.toString() ??
          json['employeeFullNameSnapshot']?.toString() ??
          json['employeeName']?.toString() ??
          '',
      employeePosition: json['employeePosition']?.toString() ?? '',
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      regionId: json['regionId'] as int? ?? 0,
      regionName: json['regionName']?.toString() ?? '',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      sentToStaffAt: json['sentToStaffAt'] != null
          ? DateTime.tryParse(json['sentToStaffAt'].toString())
          : null,
      staffAcceptedAt: json['staffAcceptedAt'] != null
          ? DateTime.tryParse(json['staffAcceptedAt'].toString())
          : null,
      hasPdf: json['hasPdf'] as bool? ?? false,
    );
  }

  bool get isPendingStaff => status.toUpperCase() == 'PENDING_STAFF';
  bool get isActive => status.toUpperCase() == 'ACTIVE';
  bool get isScheduled => status.toUpperCase() == 'SCHEDULED';
  bool get isExpired => status.toUpperCase() == 'EXPIRED';
  bool get isCancelled => status.toUpperCase() == 'CANCELLED';
  bool get isTerminated => status.toUpperCase() == 'TERMINATED';

  String get statusDisplayName {
    switch (status.toUpperCase()) {
      case 'PENDING_STAFF':
        return 'Pending Acceptance';
      case 'ACTIVE':
        return 'Active';
      case 'SCHEDULED':
        return 'Scheduled (Pending Start)';
      case 'EXPIRED':
        return 'Expired';
      case 'CANCELLED':
        return 'Cancelled';
      case 'TERMINATED':
        return 'Terminated';
      default:
        return status;
    }
  }

  Color get statusColor {
    switch (status.toUpperCase()) {
      case 'PENDING_STAFF':
        return AppColors.warning;
      case 'ACTIVE':
        return const Color(0xFF059669);
      case 'SCHEDULED':
        return AppColors.info;
      case 'EXPIRED':
        return AppColors.textMuted;
      case 'CANCELLED':
      case 'TERMINATED':
        return AppColors.error;
      default:
        return AppColors.textSecondary;
    }
  }

  String get formattedHourlyRate {
    final formatted = hourlyRate.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        );
    return '$formatted VND/hr';
  }
}

/// Model chi tiết hợp đồng lao động (GET /api/staff/contracts/{id})
class StaffContractDetailModel {
  final int contractId;
  final String contractNumber;
  final String status;
  final DateTime startDate;
  final DateTime? endDate;
  final double hourlyRate;
  final String employeeFullName;
  final String employeePosition;
  final String? contractTerms;
  final int branchId;
  final String branchName;
  final String branchAddress;
  final int regionId;
  final String regionName;
  final String? createdByManagerName;
  final DateTime createdAt;
  final DateTime? sentToStaffAt;
  final DateTime? staffAcceptedAt;
  final bool hasPdf;
  final bool canAccept;
  final String? cannotAcceptReason;
  final bool canSubmitChangeRequest;
  final String? cannotSubmitChangeRequestReason;
  final int? openChangeRequestId;
  final String? openChangeRequestStatus;
  final String? terminationReason;
  final DateTime? terminationEffectiveAt;
  final String? cancellationReason;
  final DateTime? cancelledAt;

  StaffContractDetailModel({
    required this.contractId,
    required this.contractNumber,
    required this.status,
    required this.startDate,
    this.endDate,
    required this.hourlyRate,
    this.employeeFullName = '',
    required this.employeePosition,
    this.contractTerms,
    required this.branchId,
    required this.branchName,
    required this.branchAddress,
    required this.regionId,
    required this.regionName,
    this.createdByManagerName,
    required this.createdAt,
    this.sentToStaffAt,
    this.staffAcceptedAt,
    required this.hasPdf,
    required this.canAccept,
    this.cannotAcceptReason,
    required this.canSubmitChangeRequest,
    this.cannotSubmitChangeRequestReason,
    this.openChangeRequestId,
    this.openChangeRequestStatus,
    this.terminationReason,
    this.terminationEffectiveAt,
    this.cancellationReason,
    this.cancelledAt,
  });

  factory StaffContractDetailModel.fromJson(Map<String, dynamic> json) {
    return StaffContractDetailModel(
      contractId: json['contractId'] as int? ?? 0,
      contractNumber: json['contractNumber']?.toString() ?? '',
      status: json['status']?.toString() ?? 'DRAFT',
      startDate: json['startDate'] != null
          ? DateTime.tryParse(json['startDate'].toString()) ?? DateTime.now()
          : DateTime.now(),
      endDate: json['endDate'] != null
          ? DateTime.tryParse(json['endDate'].toString())
          : null,
      hourlyRate: (json['hourlyRate'] as num?)?.toDouble() ?? 0.0,
      employeeFullName: json['employeeFullName']?.toString() ??
          json['employeeFullNameSnapshot']?.toString() ??
          json['employeeName']?.toString() ??
          '',
      employeePosition: json['employeePosition']?.toString() ?? '',
      contractTerms: json['contractTerms'] as String?,
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      branchAddress: json['branchAddress']?.toString() ?? '',
      regionId: json['regionId'] as int? ?? 0,
      regionName: json['regionName']?.toString() ?? '',
      createdByManagerName: json['createdByManagerName'] as String?,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      sentToStaffAt: json['sentToStaffAt'] != null
          ? DateTime.tryParse(json['sentToStaffAt'].toString())
          : null,
      staffAcceptedAt: json['staffAcceptedAt'] != null
          ? DateTime.tryParse(json['staffAcceptedAt'].toString())
          : null,
      hasPdf: json['hasPdf'] as bool? ?? false,
      canAccept: json['canAccept'] as bool? ?? false,
      cannotAcceptReason: json['cannotAcceptReason'] as String?,
      canSubmitChangeRequest: json['canSubmitChangeRequest'] as bool? ?? false,
      cannotSubmitChangeRequestReason: json['cannotSubmitChangeRequestReason'] as String?,
      openChangeRequestId: json['openChangeRequestId'] as int?,
      openChangeRequestStatus: json['openChangeRequestStatus'] as String?,
      terminationReason: json['terminationReason'] as String?,
      terminationEffectiveAt: json['terminationEffectiveAt'] != null
          ? DateTime.tryParse(json['terminationEffectiveAt'].toString())
          : null,
      cancellationReason: json['cancellationReason'] as String?,
      cancelledAt: json['cancelledAt'] != null
          ? DateTime.tryParse(json['cancelledAt'].toString())
          : null,
    );
  }

  bool get isPendingStaff => status.toUpperCase() == 'PENDING_STAFF';
  bool get isActive => status.toUpperCase() == 'ACTIVE';
  bool get isScheduled => status.toUpperCase() == 'SCHEDULED';
  bool get isExpired => status.toUpperCase() == 'EXPIRED';
  bool get isCancelled => status.toUpperCase() == 'CANCELLED';
  bool get isTerminated => status.toUpperCase() == 'TERMINATED';

  String get statusDisplayName {
    switch (status.toUpperCase()) {
      case 'PENDING_STAFF':
        return 'Pending Acceptance';
      case 'ACTIVE':
        return 'Active';
      case 'SCHEDULED':
        return 'Scheduled (Pending Start)';
      case 'EXPIRED':
        return 'Expired';
      case 'CANCELLED':
        return 'Cancelled';
      case 'TERMINATED':
        return 'Terminated';
      default:
        return status;
    }
  }

  String get formattedHourlyRate {
    final formatted = hourlyRate.toStringAsFixed(0).replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        );
    return '$formatted VND/hr';
  }
}

/// Model chi tiết từng trường yêu cầu thay đổi
class ContractChangeRequestItemDetailModel {
  final int changeRequestItemId;
  final String fieldName;
  final String? originalValue;
  final String proposedValue;
  final String? reason;

  ContractChangeRequestItemDetailModel({
    required this.changeRequestItemId,
    required this.fieldName,
    this.originalValue,
    required this.proposedValue,
    this.reason,
  });

  factory ContractChangeRequestItemDetailModel.fromJson(Map<String, dynamic> json) {
    return ContractChangeRequestItemDetailModel(
      changeRequestItemId: json['changeRequestItemId'] as int? ?? 0,
      fieldName: json['fieldName']?.toString() ?? '',
      originalValue: json['originalValue']?.toString(),
      proposedValue: json['proposedValue']?.toString() ?? '',
      reason: json['reason'] as String?,
    );
  }

  String get fieldDisplayName {
    switch (fieldName.trim().toLowerCase()) {
      case 'startdate':
        return 'Contract Start Date';
      case 'enddate':
        return 'Contract End Date';
      case 'employeeposition':
      case 'position':
        return 'Position / Title';
      case 'hourlyrate':
        return 'Base Hourly Rate';
      case 'contractterms':
      case 'terms':
        return 'Contract Terms';
      case 'other':
        return 'Other Details';
      default:
        return fieldName;
    }
  }
}

/// Model yêu cầu thay đổi hợp đồng (GET /api/staff/contracts/{id}/change-requests)
class StaffContractChangeRequestModel {
  final int changeRequestId;
  final int contractId;
  final String requestType;
  final String? staffComment;
  final String status;
  final DateTime? submittedAt;
  final String? reviewComment;
  final DateTime? reviewedAt;
  final DateTime createdAt;
  final List<ContractChangeRequestItemDetailModel> items;
  final String? contractNumber;
  final String? employeePosition;
  final String? branchName;

  StaffContractChangeRequestModel({
    required this.changeRequestId,
    required this.contractId,
    required this.requestType,
    this.staffComment,
    required this.status,
    this.submittedAt,
    this.reviewComment,
    this.reviewedAt,
    required this.createdAt,
    required this.items,
    this.contractNumber,
    this.employeePosition,
    this.branchName,
  });

  factory StaffContractChangeRequestModel.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List? ?? [];
    return StaffContractChangeRequestModel(
      changeRequestId: json['changeRequestId'] as int? ?? 0,
      contractId: json['contractId'] as int? ?? 0,
      requestType: json['requestType']?.toString() ?? 'CHANGE_REQUEST',
      staffComment: json['staffComment'] as String?,
      status: json['status']?.toString() ?? 'SUBMITTED',
      submittedAt: json['submittedAt'] != null
          ? DateTime.tryParse(json['submittedAt'].toString())
          : null,
      reviewComment: json['reviewComment'] as String?,
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      items: list
          .map((e) => ContractChangeRequestItemDetailModel.fromJson(e as Map<String, dynamic>))
          .toList(),
      contractNumber: json['contractNumber'] as String?,
      employeePosition: json['employeePosition'] as String?,
      branchName: json['branchName'] as String?,
    );
  }

  StaffContractChangeRequestModel copyWith({
    int? changeRequestId,
    int? contractId,
    String? requestType,
    String? staffComment,
    String? status,
    DateTime? submittedAt,
    String? reviewComment,
    DateTime? reviewedAt,
    DateTime? createdAt,
    List<ContractChangeRequestItemDetailModel>? items,
    String? contractNumber,
    String? employeePosition,
    String? branchName,
  }) {
    return StaffContractChangeRequestModel(
      changeRequestId: changeRequestId ?? this.changeRequestId,
      contractId: contractId ?? this.contractId,
      requestType: requestType ?? this.requestType,
      staffComment: staffComment ?? this.staffComment,
      status: status ?? this.status,
      submittedAt: submittedAt ?? this.submittedAt,
      reviewComment: reviewComment ?? this.reviewComment,
      reviewedAt: reviewedAt ?? this.reviewedAt,
      createdAt: createdAt ?? this.createdAt,
      items: items ?? this.items,
      contractNumber: contractNumber ?? this.contractNumber,
      employeePosition: employeePosition ?? this.employeePosition,
      branchName: branchName ?? this.branchName,
    );
  }

  bool get isSubmitted => status.toUpperCase() == 'SUBMITTED' || status.toUpperCase() == 'PENDING' || status.toUpperCase() == 'PENDING_APPROVAL';
  bool get isApproved => status.toUpperCase() == 'APPROVED';
  bool get isRejected => status.toUpperCase() == 'REJECTED';

  String get statusDisplayName {
    switch (status.toUpperCase()) {
      case 'SUBMITTED':
      case 'PENDING':
      case 'PENDING_APPROVAL':
        return 'Pending Review';
      case 'APPROVED':
        return 'Approved';
      case 'REJECTED':
        return 'Rejected';
      default:
        return status;
    }
  }
}

/// Input model khi gửi yêu cầu thay đổi hợp đồng
class ContractChangeRequestItemInputModel {
  final String fieldName;
  final String proposedValue;
  final String? reason;

  ContractChangeRequestItemInputModel({
    required this.fieldName,
    required this.proposedValue,
    this.reason,
  });

  Map<String, dynamic> toJson() {
    return {
      'fieldName': fieldName,
      'proposedValue': proposedValue,
      if (reason != null && reason!.isNotEmpty) 'reason': reason,
    };
  }
}
