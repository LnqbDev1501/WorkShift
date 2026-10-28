import 'package:flutter/material.dart';
import '../theme/app_colors.dart';
import 'payroll_preview_model.dart';

/// Trạng thái phiếu lương chính thức
class PayslipStatus {
  static const String published = 'PUBLISHED';

  static Color getColor(String? status) {
    switch (status?.toUpperCase()) {
      case 'PUBLISHED':
        return AppColors.success;
      case 'PAID':
        return const Color(0xFF059669);
      case 'UNPAID':
        return AppColors.warning;
      default:
        return AppColors.textSecondary;
    }
  }

  static String getLabel(String? status) {
    switch (status?.toUpperCase()) {
      case 'PUBLISHED':
        return 'Chính thức';
      case 'PAID':
        return 'Đã thanh toán';
      case 'UNPAID':
        return 'Chờ thanh toán';
      default:
        return status ?? 'N/A';
    }
  }
}

/// Thông tin tóm tắt phiếu lương chính thức (PayslipSummaryResponse)
class PayslipSummaryItem {
  final int payslipId;
  final int payrollId;
  final int payrollPeriodId;
  final String userId;
  final int branchId;
  final String employeeName;
  final String? employeePosition;
  final String branchName;
  final String periodCode;
  final String periodName;
  final String periodStart;
  final String periodEnd;
  final int versionNo;
  final int? previousPayslipId;
  final String status;
  final String payrollStatus;
  final DateTime publishedAt;
  final DateTime? replacedAt;
  final DateTime? feedbackDeadlineAt;
  final bool canSubmitFeedback;
  final String? feedbackBlockedReason;
  final String? correctionReason;
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

  PayslipSummaryItem({
    required this.payslipId,
    required this.payrollId,
    required this.payrollPeriodId,
    required this.userId,
    required this.branchId,
    required this.employeeName,
    this.employeePosition,
    required this.branchName,
    required this.periodCode,
    required this.periodName,
    required this.periodStart,
    required this.periodEnd,
    required this.versionNo,
    this.previousPayslipId,
    required this.status,
    required this.payrollStatus,
    required this.publishedAt,
    this.replacedAt,
    this.feedbackDeadlineAt,
    required this.canSubmitFeedback,
    this.feedbackBlockedReason,
    this.correctionReason,
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
  });

  factory PayslipSummaryItem.fromJson(Map<String, dynamic> json) {
    return PayslipSummaryItem(
      payslipId: json['payslipId'] as int? ?? 0,
      payrollId: json['payrollId'] as int? ?? 0,
      payrollPeriodId: json['payrollPeriodId'] as int? ?? 0,
      userId: json['userId']?.toString() ?? '',
      branchId: json['branchId'] as int? ?? 0,
      employeeName: json['employeeName']?.toString() ?? '',
      employeePosition: json['employeePosition']?.toString(),
      branchName: json['branchName']?.toString() ?? '',
      periodCode: json['periodCode']?.toString() ?? '',
      periodName: json['periodName']?.toString() ?? '',
      periodStart: json['periodStart']?.toString() ?? '',
      periodEnd: json['periodEnd']?.toString() ?? '',
      versionNo: json['versionNo'] as int? ?? 1,
      previousPayslipId: json['previousPayslipId'] as int?,
      status: json['status']?.toString() ?? 'PUBLISHED',
      payrollStatus: json['payrollStatus']?.toString() ?? '',
      publishedAt: json['publishedAt'] != null
          ? DateTime.tryParse(json['publishedAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
      replacedAt: json['replacedAt'] != null
          ? DateTime.tryParse(json['replacedAt'].toString())
          : null,
      feedbackDeadlineAt: json['feedbackDeadlineAt'] != null
          ? DateTime.tryParse(json['feedbackDeadlineAt'].toString())
          : null,
      canSubmitFeedback: json['canSubmitFeedback'] == true,
      feedbackBlockedReason: json['feedbackBlockedReason']?.toString(),
      correctionReason: json['correctionReason']?.toString(),
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
    );
  }
}

/// Chi tiết ca làm việc chính thức trong phiếu lương (PayslipWorkDetailsResponse)
class PayslipWorkDetailItem {
  final int payslipWorkDetailId;
  final int sourceAssignmentId;
  final int? sourceAttendanceId;
  final String workDate;
  final String shiftName;
  final DateTime scheduledStartAt;
  final DateTime scheduledEndAt;
  final int breakMinutes;
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
  final int displayOrder;

  PayslipWorkDetailItem({
    required this.payslipWorkDetailId,
    required this.sourceAssignmentId,
    this.sourceAttendanceId,
    required this.workDate,
    required this.shiftName,
    required this.scheduledStartAt,
    required this.scheduledEndAt,
    required this.breakMinutes,
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
    required this.displayOrder,
  });

  factory PayslipWorkDetailItem.fromJson(Map<String, dynamic> json) {
    return PayslipWorkDetailItem(
      payslipWorkDetailId: json['payslipWorkDetailId'] as int? ?? 0,
      sourceAssignmentId: json['sourceAssignmentId'] as int? ?? 0,
      sourceAttendanceId: json['sourceAttendanceId'] as int?,
      workDate: json['workDate']?.toString() ?? '',
      shiftName: json['shiftName']?.toString() ?? 'Ca làm việc',
      scheduledStartAt: json['scheduledStartAt'] != null
          ? DateTime.tryParse(json['scheduledStartAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      scheduledEndAt: json['scheduledEndAt'] != null
          ? DateTime.tryParse(json['scheduledEndAt'].toString()) ??
                DateTime.now()
          : DateTime.now(),
      breakMinutes: json['breakMinutes'] as int? ?? 0,
      checkInTime: json['checkInTime'] != null
          ? DateTime.tryParse(json['checkInTime'].toString())
          : null,
      checkOutTime: json['checkOutTime'] != null
          ? DateTime.tryParse(json['checkOutTime'].toString())
          : null,
      attendanceStatus: json['attendanceStatus']?.toString() ?? '',
      actualWorkingMinutes: json['actualWorkingMinutes'] as int? ?? 0,
      payableMinutes: json['payableMinutes'] as int? ?? 0,
      totalLateMinutes: json['totalLateMinutes'] as int? ?? 0,
      chargeableLateMinutes: json['chargeableLateMinutes'] as int? ?? 0,
      totalEarlyLeaveMinutes: json['totalEarlyLeaveMinutes'] as int? ?? 0,
      chargeableEarlyLeaveMinutes:
          json['chargeableEarlyLeaveMinutes'] as int? ?? 0,
      note: json['note']?.toString(),
      displayOrder: json['displayOrder'] as int? ?? 0,
    );
  }
}

/// Chi tiết đầy đủ phiếu lương chính thức (PayslipDetailsResponse)
class PayslipDetailModel {
  final PayslipSummaryItem payslip;
  final List<PayslipWorkDetailItem> workDetails;
  final List<PayrollCalculationDetailItem> calculationDetails;

  PayslipDetailModel({
    required this.payslip,
    required this.workDetails,
    required this.calculationDetails,
  });

  factory PayslipDetailModel.fromJson(Map<String, dynamic> json) {
    return PayslipDetailModel(
      payslip: PayslipSummaryItem.fromJson(
        json['payslip'] as Map<String, dynamic>,
      ),
      workDetails:
          (json['workDetails'] as List<dynamic>?)
              ?.map(
                (w) =>
                    PayslipWorkDetailItem.fromJson(w as Map<String, dynamic>),
              )
              .toList() ??
          const [],
      calculationDetails:
          (json['calculationDetails'] as List<dynamic>?)
              ?.map(
                (c) => PayrollCalculationDetailItem.fromJson(
                  c as Map<String, dynamic>,
                ),
              )
              .toList() ??
          const [],
    );
  }
}
