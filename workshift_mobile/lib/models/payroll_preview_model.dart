import 'dart:convert';
import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Trạng thái Review của lượt Preview: PENDING, CONFIRMED, FEEDBACK_PENDING, SUPERSEDED, EXPIRED
class PayrollReviewStatus {
  static const String pending = 'PENDING';
  static const String confirmed = 'CONFIRMED';
  static const String feedbackPending = 'FEEDBACK_PENDING';
  static const String superseded = 'SUPERSEDED';
  static const String expired = 'EXPIRED';

  static Color getColor(String? status) {
    switch (status?.toUpperCase()) {
      case 'PENDING':
        return AppColors.warning;
      case 'CONFIRMED':
        return AppColors.success;
      case 'FEEDBACK_PENDING':
        return const Color(0xFF8B5CF6); // Tím
      case 'EXPIRED':
        return AppColors.error;
      case 'SUPERSEDED':
        return AppColors.textMuted;
      default:
        return AppColors.textSecondary;
    }
  }

  static String getLabel(String? status) {
    switch (status?.toUpperCase()) {
      case 'PENDING':
        return 'Chờ phản hồi';
      case 'CONFIRMED':
        return 'Đã xác nhận';
      case 'FEEDBACK_PENDING':
        return 'Đã gửi phản hồi';
      case 'EXPIRED':
        return 'Quá hạn';
      case 'SUPERSEDED':
        return 'Đã có lượt mới';
      default:
        return status ?? 'N/A';
    }
  }
}

/// Trạng thái Feedback: SUBMITTED, APPROVED, EXPLAINED
class StaffFeedbackStatus {
  static const String submitted = 'SUBMITTED';
  static const String approved = 'APPROVED';
  static const String explained = 'EXPLAINED';

  static Color getColor(String? status) {
    switch (status?.toUpperCase()) {
      case 'SUBMITTED':
        return AppColors.warning;
      case 'APPROVED':
        return AppColors.success;
      case 'EXPLAINED':
        return const Color(0xFF0284C7); // Info blue
      default:
        return AppColors.textSecondary;
    }
  }

  static String getLabel(String? status) {
    switch (status?.toUpperCase()) {
      case 'SUBMITTED':
        return 'Đang chờ xử lý';
      case 'APPROVED':
        return 'Đã duyệt sửa';
      case 'EXPLAINED':
        return 'Đã giải trình';
      default:
        return status ?? 'N/A';
    }
  }
}

/// DTO phản hồi một lượt Review Preview (PayrollFeedbackResponse)
class PayrollPreviewItem {
  final int feedbackId;
  final int payrollId;
  final int payrollVersionNo;
  final int payrollPeriodId;
  final String employeeId;
  final String employeeName;
  final int? previousFeedbackId;
  final int roundNo;
  final String reviewStatus;
  final String? status;
  final String? description;
  final String? sentBy;
  final DateTime createdAt;
  final DateTime? reviewDeadlineAt;
  final DateTime? staffRespondedAt;
  final String? processedBy;
  final DateTime? processedAt;
  final String? resolutionNote;
  final String? resultNote;
  final DateTime? closedAt;
  final String? closureReason;
  final bool canRespond;
  final bool canConfirm;
  final bool canSubmitFeedback;
  final String? previousResolutionNote;
  final String? previousFeedbackDescription;
  final List<PayrollIssueItem> corrections;

  PayrollPreviewItem({
    required this.feedbackId,
    required this.payrollId,
    required this.payrollVersionNo,
    required this.payrollPeriodId,
    required this.employeeId,
    required this.employeeName,
    this.previousFeedbackId,
    required this.roundNo,
    required this.reviewStatus,
    this.status,
    this.description,
    this.sentBy,
    required this.createdAt,
    this.reviewDeadlineAt,
    this.staffRespondedAt,
    this.processedBy,
    this.processedAt,
    this.resolutionNote,
    this.resultNote,
    this.closedAt,
    this.closureReason,
    required this.canRespond,
    required this.canConfirm,
    required this.canSubmitFeedback,
    this.previousResolutionNote,
    this.previousFeedbackDescription,
    this.corrections = const [],
  });

  factory PayrollPreviewItem.fromJson(Map<String, dynamic> json) {
    return PayrollPreviewItem(
      feedbackId: json['feedbackId'] as int? ?? 0,
      payrollId: json['payrollId'] as int? ?? 0,
      payrollVersionNo: json['payrollVersionNo'] as int? ?? 1,
      payrollPeriodId: json['payrollPeriodId'] as int? ?? 0,
      employeeId: json['employeeId']?.toString() ?? '',
      employeeName: json['employeeName']?.toString() ?? '',
      previousFeedbackId: json['previousFeedbackId'] as int?,
      roundNo: json['roundNo'] as int? ?? 1,
      reviewStatus: json['reviewStatus']?.toString() ?? 'PENDING',
      status: json['status']?.toString(),
      description: json['description']?.toString(),
      sentBy: json['sentBy']?.toString(),
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      reviewDeadlineAt: json['reviewDeadlineAt'] != null
          ? DateTime.tryParse(json['reviewDeadlineAt'].toString())
          : null,
      staffRespondedAt: json['staffRespondedAt'] != null
          ? DateTime.tryParse(json['staffRespondedAt'].toString())
          : null,
      processedBy: json['processedBy']?.toString(),
      processedAt: json['processedAt'] != null
          ? DateTime.tryParse(json['processedAt'].toString())
          : null,
      resolutionNote: json['resolutionNote']?.toString(),
      resultNote: json['resultNote']?.toString(),
      closedAt: json['closedAt'] != null
          ? DateTime.tryParse(json['closedAt'].toString())
          : null,
      closureReason: json['closureReason']?.toString(),
      canRespond: json['canRespond'] == true,
      canConfirm: json['canConfirm'] == true || json['canRespond'] == true,
      canSubmitFeedback:
          json['canSubmitFeedback'] == true || json['canRespond'] == true,
      previousResolutionNote: json['previousResolutionNote']?.toString(),
      previousFeedbackDescription: json['previousFeedbackDescription']
          ?.toString(),
      corrections:
          (json['corrections'] as List<dynamic>?)
              ?.map((c) => PayrollIssueItem.fromJson(c as Map<String, dynamic>))
              .toList() ??
          const [],
    );
  }
}

/// Yêu cầu điều chỉnh nguồn lương liên kết với Feedback
class PayrollIssueItem {
  final int correctionRequestId;
  final int payrollId;
  final int? feedbackId;
  final int? previousRequestId;
  final String reason;
  final String status;
  final DateTime createdAt;
  final String? responseAction;
  final String? responseNote;
  final DateTime? respondedAt;
  final String? reviewNote;
  final DateTime? reviewedAt;
  final int? resolvedPayrollId;
  final DateTime? completedAt;
  final DateTime? responseDeadlineAt;
  final DateTime? expiredAt;
  final String periodCode;
  final String employeeName;

  PayrollIssueItem({
    required this.correctionRequestId,
    required this.payrollId,
    this.feedbackId,
    this.previousRequestId,
    required this.reason,
    required this.status,
    required this.createdAt,
    this.responseAction,
    this.responseNote,
    this.respondedAt,
    this.reviewNote,
    this.reviewedAt,
    this.resolvedPayrollId,
    this.completedAt,
    this.responseDeadlineAt,
    this.expiredAt,
    required this.periodCode,
    required this.employeeName,
  });

  factory PayrollIssueItem.fromJson(Map<String, dynamic> json) {
    return PayrollIssueItem(
      correctionRequestId: json['correctionRequestId'] as int? ?? 0,
      payrollId: json['payrollId'] as int? ?? 0,
      feedbackId: json['feedbackId'] as int?,
      previousRequestId: json['previousRequestId'] as int?,
      reason: json['reason']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      responseAction: json['responseAction']?.toString(),
      responseNote: json['responseNote']?.toString(),
      respondedAt: json['respondedAt'] != null
          ? DateTime.tryParse(json['respondedAt'].toString())
          : null,
      reviewNote: json['reviewNote']?.toString(),
      reviewedAt: json['reviewedAt'] != null
          ? DateTime.tryParse(json['reviewedAt'].toString())
          : null,
      resolvedPayrollId: json['resolvedPayrollId'] as int?,
      completedAt: json['completedAt'] != null
          ? DateTime.tryParse(json['completedAt'].toString())
          : null,
      responseDeadlineAt: json['responseDeadlineAt'] != null
          ? DateTime.tryParse(json['responseDeadlineAt'].toString())
          : null,
      expiredAt: json['expiredAt'] != null
          ? DateTime.tryParse(json['expiredAt'].toString())
          : null,
      periodCode: json['periodCode']?.toString() ?? '',
      employeeName: json['employeeName']?.toString() ?? '',
    );
  }
}

/// Chi tiết từng dòng tính lương (PayrollCalculationDetailResponse)
class PayrollCalculationDetailItem {
  final int payrollDetailId;
  final String type;
  final String? itemCode;
  final String description;
  final DateTime? workFrom;
  final DateTime? workTo;
  final double? quantity;
  final double? rate;
  final double multiplier;
  final double amount;
  final double? appliedAmount;
  final String? note;

  PayrollCalculationDetailItem({
    required this.payrollDetailId,
    required this.type,
    this.itemCode,
    required this.description,
    this.workFrom,
    this.workTo,
    this.quantity,
    this.rate,
    required this.multiplier,
    required this.amount,
    this.appliedAmount,
    this.note,
  });

  factory PayrollCalculationDetailItem.fromJson(Map<String, dynamic> json) {
    return PayrollCalculationDetailItem(
      payrollDetailId: json['payrollDetailId'] as int? ?? 0,
      type: json['type']?.toString() ?? '',
      itemCode: json['itemCode']?.toString(),
      description: json['description']?.toString() ?? '',
      workFrom: json['workFrom'] != null
          ? DateTime.tryParse(json['workFrom'].toString())
          : null,
      workTo: json['workTo'] != null
          ? DateTime.tryParse(json['workTo'].toString())
          : null,
      quantity: (json['quantity'] as num?)?.toDouble(),
      rate: (json['rate'] as num?)?.toDouble(),
      multiplier: (json['multiplier'] as num?)?.toDouble() ?? 1.0,
      amount: (json['amount'] as num?)?.toDouble() ?? 0.0,
      appliedAmount: (json['appliedAmount'] as num?)?.toDouble(),
      note: json['note']?.toString(),
    );
  }
}

/// Snapshot ca làm việc trích xuất từ WorkInputSnapshot
class ShiftWorkDetailItem {
  final int sourceAssignmentId;
  final int? sourceAttendanceId;
  final DateTime? workDate;
  final String shiftName;
  final DateTime? scheduledStartAt;
  final DateTime? scheduledEndAt;
  final DateTime? checkInTime;
  final DateTime? checkOutTime;
  final String attendanceStatus;
  final int actualWorkingMinutes;
  final int payableMinutes;
  final int totalLateMinutes;
  final int chargeableLateMinutes;
  final int totalEarlyLeaveMinutes;
  final int chargeableEarlyLeaveMinutes;
  final String? note;

  ShiftWorkDetailItem({
    required this.sourceAssignmentId,
    this.sourceAttendanceId,
    this.workDate,
    required this.shiftName,
    this.scheduledStartAt,
    this.scheduledEndAt,
    this.checkInTime,
    this.checkOutTime,
    required this.attendanceStatus,
    required this.actualWorkingMinutes,
    required this.payableMinutes,
    required this.totalLateMinutes,
    required this.chargeableLateMinutes,
    required this.totalEarlyLeaveMinutes,
    required this.chargeableEarlyLeaveMinutes,
    this.note,
  });

  factory ShiftWorkDetailItem.fromJson(Map<String, dynamic> json) {
    return ShiftWorkDetailItem(
      sourceAssignmentId: json['sourceAssignmentId'] as int? ?? 0,
      sourceAttendanceId: json['sourceAttendanceId'] as int?,
      workDate: json['workDate'] != null
          ? DateTime.tryParse(json['workDate'].toString())
          : null,
      shiftName:
          json['shiftNameSnapshot']?.toString() ??
          json['shiftName']?.toString() ??
          'Ca làm việc',
      scheduledStartAt: json['scheduledStartAt'] != null
          ? DateTime.tryParse(json['scheduledStartAt'].toString())
          : null,
      scheduledEndAt: json['scheduledEndAt'] != null
          ? DateTime.tryParse(json['scheduledEndAt'].toString())
          : null,
      checkInTime: json['checkInTime'] != null
          ? DateTime.tryParse(json['checkInTime'].toString())
          : null,
      checkOutTime: json['checkOutTime'] != null
          ? DateTime.tryParse(json['checkOutTime'].toString())
          : null,
      attendanceStatus:
          json['attendanceStatusSnapshot']?.toString() ??
          json['attendanceStatus']?.toString() ??
          '',
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      payableMinutes: json['payableMinutes'] as int? ?? 0,
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      chargeableLateMinutes: json['chargeableLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      chargeableEarlyLeaveMinutes:
          json['chargeableEarlyLeaveMinutes'] as int? ?? 0,
      note: json['note']?.toString(),
    );
  }
}

/// Mô hình bảng lương gốc được tính (PayrollCalculationResponse)
class PayrollCalculationItem {
  final int payrollId;
  final String userId;
  final int payrollPeriodId;
  final int branchId;
  final String branchName;
  final String employeeName;
  final String? employeePosition;
  final String status;
  final int versionNo;
  final double hourlyRate;
  final double totalWorkHours;
  final double workPay;
  final double totalReward;
  final double totalPenalty;
  final double attendanceDeduction;
  final double carryForwardDeduction;
  final double deferredDeductionAmount;
  final double grossSalary;
  final double totalDeduction;
  final double netSalary;
  final List<ShiftWorkDetailItem> workDetails;
  final List<PayrollCalculationDetailItem> details;

  PayrollCalculationItem({
    required this.payrollId,
    required this.userId,
    required this.payrollPeriodId,
    required this.branchId,
    required this.branchName,
    required this.employeeName,
    this.employeePosition,
    required this.status,
    required this.versionNo,
    required this.hourlyRate,
    required this.totalWorkHours,
    required this.workPay,
    required this.totalReward,
    required this.totalPenalty,
    required this.attendanceDeduction,
    required this.carryForwardDeduction,
    required this.deferredDeductionAmount,
    required this.grossSalary,
    required this.totalDeduction,
    required this.netSalary,
    this.workDetails = const [],
    this.details = const [],
  });

  factory PayrollCalculationItem.fromJson(Map<String, dynamic> json) {
    // Parse work details from workInputSnapshot if present
    List<ShiftWorkDetailItem> parsedWorkDetails = [];
    if (json['workInputSnapshot'] != null) {
      try {
        dynamic snapshot = json['workInputSnapshot'];
        if (snapshot is String) {
          snapshot = jsonDecode(snapshot);
        }
        if (snapshot is Map<String, dynamic> &&
            snapshot['workDetails'] is List) {
          parsedWorkDetails = (snapshot['workDetails'] as List)
              .map(
                (item) =>
                    ShiftWorkDetailItem.fromJson(item as Map<String, dynamic>),
              )
              .toList();
        }
      } catch (_) {}
    }

    final parsedDetails =
        (json['details'] as List<dynamic>?)
            ?.map(
              (d) => PayrollCalculationDetailItem.fromJson(
                d as Map<String, dynamic>,
              ),
            )
            .toList() ??
        const [];

    return PayrollCalculationItem(
      payrollId: json['payrollId'] as int? ?? 0,
      userId: json['userId']?.toString() ?? '',
      payrollPeriodId: json['payrollPeriodId'] as int? ?? 0,
      branchId: json['branchId'] as int? ?? 0,
      branchName: json['branchName']?.toString() ?? '',
      employeeName: json['employeeName']?.toString() ?? '',
      employeePosition: json['employeePosition']?.toString(),
      status: json['status']?.toString() ?? '',
      versionNo: json['versionNo'] as int? ?? 1,
      hourlyRate: (json['hourlyRate'] as num?)?.toDouble() ?? 0.0,
      totalWorkHours: (json['totalWorkHours'] as num?)?.toDouble() ?? 0.0,
      workPay: (json['workPay'] as num?)?.toDouble() ?? 0.0,
      totalReward: (json['totalReward'] as num?)?.toDouble() ?? 0.0,
      totalPenalty: (json['totalPenalty'] as num?)?.toDouble() ?? 0.0,
      attendanceDeduction:
          (json['attendanceDeduction'] as num?)?.toDouble() ?? 0.0,
      carryForwardDeduction:
          (json['carryForwardDeduction'] as num?)?.toDouble() ?? 0.0,
      deferredDeductionAmount:
          (json['deferredDeductionAmount'] as num?)?.toDouble() ?? 0.0,
      grossSalary: (json['grossSalary'] as num?)?.toDouble() ?? 0.0,
      totalDeduction: (json['totalDeduction'] as num?)?.toDouble() ?? 0.0,
      netSalary: (json['netSalary'] as num?)?.toDouble() ?? 0.0,
      workDetails: parsedWorkDetails,
      details: parsedDetails,
    );
  }
}

/// Điều chỉnh thưởng phạt đang chờ duyệt
class PendingPayrollAdjustmentItem {
  final int rewardPenaltyId;
  final String type;
  final String name;
  final double amount;
  final String status;

  PendingPayrollAdjustmentItem({
    required this.rewardPenaltyId,
    required this.type,
    required this.name,
    required this.amount,
    required this.status,
  });

  factory PendingPayrollAdjustmentItem.fromJson(Map<String, dynamic> json) {
    return PendingPayrollAdjustmentItem(
      rewardPenaltyId: json['rewardPenaltyId'] as int? ?? 0,
      type: json['type']?.toString() ?? '',
      name: json['name']?.toString() ?? '',
      amount: (json['amount'] as num?)?.toDouble() ?? 0.0,
      status: json['status']?.toString() ?? 'PENDING',
    );
  }
}

/// Chi tiết đầy đủ một lượt Preview (PayrollFeedbackDetailsResponse)
class PayrollPreviewDetail {
  final PayrollPreviewItem feedback;
  final PayrollCalculationItem originalPayroll;
  final List<PendingPayrollAdjustmentItem> pendingSourceAdjustments;

  PayrollPreviewDetail({
    required this.feedback,
    required this.originalPayroll,
    this.pendingSourceAdjustments = const [],
  });

  factory PayrollPreviewDetail.fromJson(Map<String, dynamic> json) {
    return PayrollPreviewDetail(
      feedback: PayrollPreviewItem.fromJson(
        json['feedback'] as Map<String, dynamic>,
      ),
      originalPayroll: PayrollCalculationItem.fromJson(
        json['originalPayroll'] as Map<String, dynamic>,
      ),
      pendingSourceAdjustments:
          (json['pendingSourceAdjustments'] as List<dynamic>?)
              ?.map(
                (a) => PendingPayrollAdjustmentItem.fromJson(
                  a as Map<String, dynamic>,
                ),
              )
              .toList() ??
          const [],
    );
  }
}
