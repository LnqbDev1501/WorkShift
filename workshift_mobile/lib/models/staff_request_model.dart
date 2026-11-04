import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Shift summary model in requests list
class StaffRequestShiftSummaryModel {
  final int totalShifts;
  final String? primaryShiftName;
  final DateTime? primaryScheduledStartAt;
  final DateTime? primaryScheduledEndAt;
  final DateTime? requestedTime;

  StaffRequestShiftSummaryModel({
    required this.totalShifts,
    this.primaryShiftName,
    this.primaryScheduledStartAt,
    this.primaryScheduledEndAt,
    this.requestedTime,
  });

  factory StaffRequestShiftSummaryModel.fromJson(Map<String, dynamic> json) {
    return StaffRequestShiftSummaryModel(
      totalShifts: json['totalShifts'] as int? ?? 0,
      primaryShiftName: json['primaryShiftName'] as String?,
      primaryScheduledStartAt: json['primaryScheduledStartAt'] != null
          ? DateTime.tryParse(json['primaryScheduledStartAt'].toString())
          : null,
      primaryScheduledEndAt: json['primaryScheduledEndAt'] != null
          ? DateTime.tryParse(json['primaryScheduledEndAt'].toString())
          : null,
      requestedTime: json['requestedTime'] != null
          ? DateTime.tryParse(json['requestedTime'].toString())
          : null,
    );
  }
}

/// Salary increase request detail model (SALARY_INCREASE)
class StaffSalaryIncreaseDetailModel {
  final int currentContractId;
  final String? contractNumber;
  final double currentHourlyRate;
  final double proposedHourlyRate;

  StaffSalaryIncreaseDetailModel({
    required this.currentContractId,
    this.contractNumber,
    required this.currentHourlyRate,
    required this.proposedHourlyRate,
  });

  factory StaffSalaryIncreaseDetailModel.fromJson(Map<String, dynamic> json) {
    return StaffSalaryIncreaseDetailModel(
      currentContractId: json['currentContractId'] as int? ?? 0,
      contractNumber: json['contractNumber'] as String?,
      currentHourlyRate: (json['currentHourlyRate'] as num?)?.toDouble() ?? 0.0,
      proposedHourlyRate:
          (json['proposedHourlyRate'] as num?)?.toDouble() ?? 0.0,
    );
  }
}

/// Staff request item model (GET /api/staff/requests)
class StaffEmployeeRequestListItemModel {
  final int requestId;
  final String userId;
  final int branchId;
  final String branchName;
  final String branchCode;
  final String requestType;
  final String? reason;
  final String status;
  final DateTime createdAt;
  final DateTime? updatedAt;
  final String? reviewedBy;
  final String? reviewerFullName;
  final DateTime? reviewedAt;
  final String? reviewNote;
  final StaffRequestShiftSummaryModel? shiftSummary;
  final StaffSalaryIncreaseDetailModel? salaryIncreaseDetail;

  StaffEmployeeRequestListItemModel({
    required this.requestId,
    required this.userId,
    required this.branchId,
    required this.branchName,
    required this.branchCode,
    required this.requestType,
    this.reason,
    required this.status,
    required this.createdAt,
    this.updatedAt,
    this.reviewedBy,
    this.reviewerFullName,
    this.reviewedAt,
    this.reviewNote,
    this.shiftSummary,
    this.salaryIncreaseDetail,
  });

  factory StaffEmployeeRequestListItemModel.fromJson(
    Map<String, dynamic> json,
  ) {
    return StaffEmployeeRequestListItemModel(
      requestId: json['requestId'] as int? ?? 0,
      userId: json['userId']?.toString() ?? '',
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      branchCode: json['branchCode']?.toString() ?? '',
      requestType: json['requestType']?.toString() ?? '',
      reason: json['reason'] as String?,
      status: json['status']?.toString() ?? 'PENDING',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt']?.toString() ?? ''),
      reviewedBy: json['reviewedBy']?.toString(),
      reviewerFullName: json['reviewerFullName'] as String?,
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      reviewNote: json['reviewNote'] as String?,
      shiftSummary: json['shiftSummary'] != null
          ? StaffRequestShiftSummaryModel.fromJson(
              json['shiftSummary'] as Map<String, dynamic>,
            )
          : null,
      salaryIncreaseDetail: json['salaryIncreaseDetail'] != null
          ? StaffSalaryIncreaseDetailModel.fromJson(
              json['salaryIncreaseDetail'] as Map<String, dynamic>,
            )
          : null,
    );
  }

  bool get isPending => status.toUpperCase() == 'PENDING';
  bool get isApproved => status.toUpperCase() == 'APPROVED';
  bool get isRejected => status.toUpperCase() == 'REJECTED';
  bool get isCancelled => status.toUpperCase() == 'CANCELLED';

  String get typeDisplayName {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return 'Leave';
      case 'LATE_ARRIVAL':
        return 'Late Arrival';
      case 'EARLY_LEAVE':
        return 'Early Departure';
      case 'SALARY_INCREASE':
        return 'Salary Increase';
      default:
        return requestType;
    }
  }

  String get statusDisplayName {
    switch (status.toUpperCase()) {
      case 'PENDING':
        return 'Pending';
      case 'APPROVED':
        return 'Approved';
      case 'REJECTED':
        return 'Rejected';
      case 'CANCELLED':
        return 'Cancelled';
      default:
        return status;
    }
  }

  IconData get typeIcon {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return Icons.event_busy_rounded;
      case 'LATE_ARRIVAL':
        return Icons.more_time_rounded;
      case 'EARLY_LEAVE':
        return Icons.timer_off_rounded;
      case 'SALARY_INCREASE':
        return Icons.payments_rounded;
      default:
        return Icons.description_rounded;
    }
  }

  Color get typeColor {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return const Color(0xFF059669); // Deep mint green
      case 'LATE_ARRIVAL':
        return const Color(0xFFEA580C); // Cam
      case 'EARLY_LEAVE':
        return const Color(0xFF7C3AED); // Purple
      case 'SALARY_INCREASE':
        return const Color(0xFF2563EB); // Blue
      default:
        return AppColors.primary;
    }
  }
}

/// Monthly quota rule model
class StaffAppliedRequestLimitRuleModel {
  final int ruleId;
  final String requestType;
  final int maxRequestsPerMonth;

  StaffAppliedRequestLimitRuleModel({
    required this.ruleId,
    required this.requestType,
    required this.maxRequestsPerMonth,
  });

  factory StaffAppliedRequestLimitRuleModel.fromJson(
    Map<String, dynamic> json,
  ) {
    return StaffAppliedRequestLimitRuleModel(
      ruleId: json['ruleId'] as int? ?? 0,
      requestType: json['requestType']?.toString() ?? '',
      maxRequestsPerMonth: json['maxRequestsPerMonth'] as int? ?? 0,
    );
  }
}

/// Leave shift model
class StaffLeaveRequestAssignmentModel {
  final int leaveRequestAssignmentId;
  final int assignmentId;
  final String assignmentStatus;
  final int shiftId;
  final String shiftName;
  final String? shiftTemplateName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final String? branchName;

  StaffLeaveRequestAssignmentModel({
    required this.leaveRequestAssignmentId,
    required this.assignmentId,
    required this.assignmentStatus,
    required this.shiftId,
    required this.shiftName,
    this.shiftTemplateName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    this.branchName,
  });

  factory StaffLeaveRequestAssignmentModel.fromJson(Map<String, dynamic> json) {
    return StaffLeaveRequestAssignmentModel(
      leaveRequestAssignmentId: json['leaveRequestAssignmentId'] as int? ?? 0,
      assignmentId: json['assignmentId'] as int? ?? 0,
      assignmentStatus: json['assignmentStatus']?.toString() ?? '',
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? '',
      shiftTemplateName: json['shiftTemplateName'] as String?,
      scheduledStartAt: json['scheduledStartAt'] != null
          ? DateTime.tryParse(json['scheduledStartAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      scheduledEndAt: json['scheduledEndAt'] != null
          ? DateTime.tryParse(json['scheduledEndAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      branchName: json['branchName'] as String?,
    );
  }
}

/// Time adjustment detail model (Late arrival / Early leave)
class StaffTimeAdjustmentDetailModel {
  final int assignmentId;
  final DateTime requestedTime;
  final String assignmentStatus;
  final int shiftId;
  final String shiftName;
  final String? shiftTemplateName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final String? branchName;

  StaffTimeAdjustmentDetailModel({
    required this.assignmentId,
    required this.requestedTime,
    required this.assignmentStatus,
    required this.shiftId,
    required this.shiftName,
    this.shiftTemplateName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    this.branchName,
  });

  factory StaffTimeAdjustmentDetailModel.fromJson(Map<String, dynamic> json) {
    return StaffTimeAdjustmentDetailModel(
      assignmentId: json['assignmentId'] as int? ?? 0,
      requestedTime: json['requestedTime'] != null
          ? DateTime.tryParse(json['requestedTime'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      assignmentStatus: json['assignmentStatus']?.toString() ?? '',
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? '',
      shiftTemplateName: json['shiftTemplateName'] as String?,
      scheduledStartAt: json['scheduledStartAt'] != null
          ? DateTime.tryParse(json['scheduledStartAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      scheduledEndAt: json['scheduledEndAt'] != null
          ? DateTime.tryParse(json['scheduledEndAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      branchName: json['branchName'] as String?,
    );
  }
}

/// Staff request detail model (GET /api/staff/requests/{requestId})
class StaffEmployeeRequestDetailModel {
  final int requestId;
  final String userId;
  final int branchId;
  final String branchName;
  final String branchCode;
  final String requestType;
  final String? reason;
  final String status;
  final DateTime createdAt;
  final DateTime updatedAt;
  final String? reviewedBy;
  final String? reviewerFullName;
  final DateTime? reviewedAt;
  final String? reviewNote;
  final StaffAppliedRequestLimitRuleModel? appliedRule;
  final List<StaffLeaveRequestAssignmentModel>? leaveAssignments;
  final StaffTimeAdjustmentDetailModel? timeAdjustmentDetail;
  final StaffSalaryIncreaseDetailModel? salaryIncreaseDetail;

  StaffEmployeeRequestDetailModel({
    required this.requestId,
    required this.userId,
    required this.branchId,
    required this.branchName,
    required this.branchCode,
    required this.requestType,
    this.reason,
    required this.status,
    required this.createdAt,
    required this.updatedAt,
    this.reviewedBy,
    this.reviewerFullName,
    this.reviewedAt,
    this.reviewNote,
    this.appliedRule,
    this.leaveAssignments,
    this.timeAdjustmentDetail,
    this.salaryIncreaseDetail,
  });

  factory StaffEmployeeRequestDetailModel.fromJson(Map<String, dynamic> json) {
    return StaffEmployeeRequestDetailModel(
      requestId: json['requestId'] as int? ?? 0,
      userId: json['userId']?.toString() ?? '',
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      branchCode: json['branchCode']?.toString() ?? '',
      requestType: json['requestType']?.toString() ?? '',
      reason: json['reason'] as String?,
      status: json['status']?.toString() ?? 'PENDING',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      reviewedBy: json['reviewedBy']?.toString(),
      reviewerFullName: json['reviewerFullName'] as String?,
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      reviewNote: json['reviewNote'] as String?,
      appliedRule: json['appliedRule'] != null
          ? StaffAppliedRequestLimitRuleModel.fromJson(
              json['appliedRule'] as Map<String, dynamic>,
            )
          : null,
      leaveAssignments: json['leaveAssignments'] != null
          ? (json['leaveAssignments'] as List)
                .map(
                  (e) => StaffLeaveRequestAssignmentModel.fromJson(
                    e as Map<String, dynamic>,
                  ),
                )
                .toList()
          : null,
      timeAdjustmentDetail: json['timeAdjustmentDetail'] != null
          ? StaffTimeAdjustmentDetailModel.fromJson(
              json['timeAdjustmentDetail'] as Map<String, dynamic>,
            )
          : null,
      salaryIncreaseDetail: json['salaryIncreaseDetail'] != null
          ? StaffSalaryIncreaseDetailModel.fromJson(
              json['salaryIncreaseDetail'] as Map<String, dynamic>,
            )
          : null,
    );
  }

  bool get isPending => status.toUpperCase() == 'PENDING';
  bool get isApproved => status.toUpperCase() == 'APPROVED';
  bool get isRejected => status.toUpperCase() == 'REJECTED';
  bool get isCancelled => status.toUpperCase() == 'CANCELLED';

  String get typeDisplayName {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return 'Leave';
      case 'LATE_ARRIVAL':
        return 'Late Arrival';
      case 'EARLY_LEAVE':
        return 'Early Departure';
      case 'SALARY_INCREASE':
        return 'Salary Increase';
      default:
        return requestType;
    }
  }

  IconData get typeIcon {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return Icons.event_busy_rounded;
      case 'LATE_ARRIVAL':
        return Icons.more_time_rounded;
      case 'EARLY_LEAVE':
        return Icons.timer_off_rounded;
      case 'SALARY_INCREASE':
        return Icons.payments_rounded;
      default:
        return Icons.description_rounded;
    }
  }

  Color get typeColor {
    switch (requestType.toUpperCase()) {
      case 'LEAVE':
        return const Color(0xFF059669);
      case 'LATE_ARRIVAL':
        return const Color(0xFFEA580C);
      case 'EARLY_LEAVE':
        return const Color(0xFF7C3AED);
      case 'SALARY_INCREASE':
        return const Color(0xFF2563EB);
      default:
        return AppColors.primary;
    }
  }

  String get statusDisplayName {
    switch (status.toUpperCase()) {
      case 'PENDING':
        return 'Pending';
      case 'APPROVED':
        return 'Approved';
      case 'REJECTED':
        return 'Rejected';
      case 'CANCELLED':
        return 'Cancelled';
      default:
        return status;
    }
  }
}

/// Eligible shift model for requests (GET /api/staff/requests/eligible-shifts)
class StaffEligibleShiftModel {
  final int assignmentId;
  final int shiftId;
  final String shiftName;
  final String? shiftTemplateName;
  final int branchId;
  final String branchName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;

  StaffEligibleShiftModel({
    required this.assignmentId,
    required this.shiftId,
    required this.shiftName,
    this.shiftTemplateName,
    required this.branchId,
    required this.branchName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
  });

  factory StaffEligibleShiftModel.fromJson(Map<String, dynamic> json) {
    return StaffEligibleShiftModel(
      assignmentId: json['assignmentId'] as int? ?? 0,
      shiftId: json['shiftId'] as int? ?? 0,
      shiftName: json['shiftName']?.toString() ?? '',
      shiftTemplateName: json['shiftTemplateName'] as String?,
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      scheduledStartAt: json['scheduledStartAt'] != null
          ? DateTime.tryParse(json['scheduledStartAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      scheduledEndAt: json['scheduledEndAt'] != null
          ? DateTime.tryParse(json['scheduledEndAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
    );
  }
}

/// Paged result model for staff requests
class PagedStaffRequestModel {
  final List<StaffEmployeeRequestListItemModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalItems;
  final int totalPages;

  PagedStaffRequestModel({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalItems,
    required this.totalPages,
  });

  factory PagedStaffRequestModel.fromJson(Map<String, dynamic> json) {
    final list = json['items'] as List? ?? [];
    return PagedStaffRequestModel(
      items: list
          .map(
            (e) => StaffEmployeeRequestListItemModel.fromJson(
              e as Map<String, dynamic>,
            ),
          )
          .toList(),
      pageNumber: json['pageNumber'] as int? ?? (json['page'] as int? ?? 1),
      pageSize: json['pageSize'] as int? ?? 10,
      totalItems:
          json['totalItems'] as int? ?? (json['totalCount'] as int? ?? 0),
      totalPages: json['totalPages'] as int? ?? 0,
    );
  }
}
