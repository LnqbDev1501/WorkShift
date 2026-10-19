import 'package:flutter_test/flutter_test.dart';
import 'package:workshift_mobile/models/payroll_preview_model.dart';
import 'package:workshift_mobile/models/payslip_model.dart';
import 'package:workshift_mobile/utils/format_utils.dart';

void main() {
  group('Payroll Models & Helpers Test', () {
    test('Format Vietnam Time and Currency', () {
      final utcTime = DateTime.utc(2026, 10, 19, 2, 0); // 02:00 UTC -> 09:00 UTC+7
      final formatted = formatVietnamDateTime(utcTime);
      expect(formatted, '19/10/2026 09:00');

      final dateFormatted = formatVietnamDate(utcTime);
      expect(dateFormatted, '19/10/2026');

      final currency = formatCurrency(12500000);
      expect(currency, '12,500,000 VNĐ');
    });

    test('PayrollPreviewItem.fromJson parses correctly', () {
      final json = {
        'feedbackId': 101,
        'payrollId': 55,
        'payrollVersionNo': 2,
        'payrollPeriodId': 12,
        'employeeId': '68b9193d-d421-4f11-8868-b39b360b3ee4',
        'employeeName': 'Nguyễn Văn A',
        'roundNo': 2,
        'reviewStatus': 'PENDING',
        'status': 'SUBMITTED',
        'description': 'Sai số giờ công ca sáng',
        'createdAt': '2026-10-18T10:00:00Z',
        'reviewDeadlineAt': '2026-10-19T17:00:00Z',
        'canRespond': true,
        'canConfirm': true,
        'canSubmitFeedback': true,
        'resolutionNote': 'Kế toán đã kiểm tra và điều chỉnh ca làm',
        'corrections': [
          {
            'correctionRequestId': 5,
            'payrollId': 55,
            'reason': 'Sai giờ chấm công',
            'status': 'COMPLETED',
            'createdAt': '2026-10-18T11:00:00Z',
            'periodCode': 'PR-2026-12',
            'employeeName': 'Nguyễn Văn A',
          }
        ],
      };

      final item = PayrollPreviewItem.fromJson(json);
      expect(item.feedbackId, 101);
      expect(item.payrollId, 55);
      expect(item.payrollVersionNo, 2);
      expect(item.payrollPeriodId, 12);
      expect(item.employeeName, 'Nguyễn Văn A');
      expect(item.roundNo, 2);
      expect(item.reviewStatus, 'PENDING');
      expect(item.canConfirm, true);
      expect(item.canSubmitFeedback, true);
      expect(item.corrections.length, 1);
      expect(item.corrections.first.correctionRequestId, 5);
      expect(item.corrections.first.status, 'COMPLETED');
    });

    test('PayrollPreviewDetail.fromJson parses nested snapshot and shifts', () {
      final json = {
        'feedback': {
          'feedbackId': 101,
          'payrollId': 55,
          'payrollVersionNo': 1,
          'payrollPeriodId': 12,
          'employeeId': 'user-1',
          'employeeName': 'Nguyễn Văn A',
          'roundNo': 1,
          'reviewStatus': 'PENDING',
          'createdAt': '2026-10-18T10:00:00Z',
          'canRespond': true,
        },
        'originalPayroll': {
          'payrollId': 55,
          'userId': 'user-1',
          'payrollPeriodId': 12,
          'branchId': 1,
          'branchName': 'Chi nhánh Quận 1',
          'employeeName': 'Nguyễn Văn A',
          'employeePosition': 'Barista',
          'status': 'CALCULATED',
          'versionNo': 1,
          'hourlyRate': 30000.0,
          'totalWorkHours': 40.0,
          'workPay': 1200000.0,
          'totalReward': 100000.0,
          'totalPenalty': 0.0,
          'attendanceDeduction': 0.0,
          'carryForwardDeduction': 0.0,
          'deferredDeductionAmount': 0.0,
          'grossSalary': 1300000.0,
          'totalDeduction': 0.0,
          'netSalary': 1300000.0,
          'workInputSnapshot': {
            'schemaVersion': 1,
            'workDetails': [
              {
                'sourceAssignmentId': 12,
                'workDate': '2026-10-12',
                'shiftNameSnapshot': 'Ca sáng',
                'actualWorkingMinutes': 240,
                'payableMinutes': 240,
                'totalLateMinutes': 0,
                'totalEarlyLeaveMinutes': 0,
                'attendanceStatusSnapshot': 'COMPLETED',
              }
            ]
          },
          'details': [
            {
              'payrollDetailId': 1,
              'type': 'WORK',
              'description': '12/10/2026 - Ca sáng',
              'multiplier': 1.0,
              'amount': 120000.0,
            }
          ],
        },
        'pendingSourceAdjustments': [
          {
            'rewardPenaltyId': 10,
            'type': 'REWARD',
            'name': 'Chuyên cần tuần',
            'amount': 50000.0,
            'status': 'PENDING',
          }
        ],
      };

      final detail = PayrollPreviewDetail.fromJson(json);
      expect(detail.feedback.feedbackId, 101);
      expect(detail.originalPayroll.netSalary, 1300000.0);
      expect(detail.originalPayroll.workDetails.length, 1);
      expect(detail.originalPayroll.workDetails.first.shiftName, 'Ca sáng');
      expect(detail.originalPayroll.workDetails.first.payableMinutes, 240);
      expect(detail.originalPayroll.details.length, 1);
      expect(detail.pendingSourceAdjustments.length, 1);
      expect(detail.pendingSourceAdjustments.first.name, 'Chuyên cần tuần');
    });

    test('PayslipDetailModel.fromJson parses official payslip properly', () {
      final json = {
        'payslip': {
          'payslipId': 201,
          'payrollId': 55,
          'payrollPeriodId': 12,
          'userId': 'user-1',
          'branchId': 1,
          'employeeName': 'Nguyễn Văn A',
          'employeePosition': 'Barista',
          'branchName': 'Chi nhánh Quận 1',
          'periodCode': 'PR-2026-12',
          'periodName': 'Kỳ lương tuần 42',
          'periodStart': '2026-10-12',
          'periodEnd': '2026-10-18',
          'versionNo': 1,
          'status': 'PUBLISHED',
          'payrollStatus': 'APPROVED',
          'publishedAt': '2026-10-19T01:00:00Z',
          'canSubmitFeedback': false,
          'hourlyRate': 30000.0,
          'totalWorkHours': 40.0,
          'workPay': 1200000.0,
          'totalReward': 100000.0,
          'totalPenalty': 0.0,
          'attendanceDeduction': 0.0,
          'carryForwardDeduction': 0.0,
          'deferredDeductionAmount': 0.0,
          'grossSalary': 1300000.0,
          'totalDeduction': 0.0,
          'netSalary': 1300000.0,
        },
        'workDetails': [
          {
            'payslipWorkDetailId': 1,
            'sourceAssignmentId': 12,
            'workDate': '2026-10-12',
            'shiftName': 'Ca sáng',
            'scheduledStartAt': '2026-10-12T07:00:00Z',
            'scheduledEndAt': '2026-10-12T11:00:00Z',
            'breakMinutes': 0,
            'attendanceStatus': 'COMPLETED',
            'actualWorkingMinutes': 240,
            'payableMinutes': 240,
            'totalLateMinutes': 0,
            'chargeableLateMinutes': 0,
            'totalEarlyLeaveMinutes': 0,
            'chargeableEarlyLeaveMinutes': 0,
            'displayOrder': 1,
          }
        ],
        'calculationDetails': [
          {
            'payrollDetailId': 1,
            'type': 'WORK',
            'description': '12/10/2026 - Ca sáng',
            'multiplier': 1.0,
            'amount': 120000.0,
          }
        ]
      };

      final detail = PayslipDetailModel.fromJson(json);
      expect(detail.payslip.payslipId, 201);
      expect(detail.payslip.periodCode, 'PR-2026-12');
      expect(detail.payslip.status, 'PUBLISHED');
      expect(detail.payslip.canSubmitFeedback, false);
      expect(detail.payslip.netSalary, 1300000.0);
      expect(detail.workDetails.length, 1);
      expect(detail.workDetails.first.shiftName, 'Ca sáng');
      expect(detail.calculationDetails.length, 1);
    });
  });
}
