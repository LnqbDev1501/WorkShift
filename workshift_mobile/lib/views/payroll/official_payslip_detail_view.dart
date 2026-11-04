import 'package:flutter/material.dart';
import '../../models/payroll/payroll_preview_model.dart';
import '../../models/payroll/payslip_model.dart';
import '../../services/payroll/payroll_service.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';

/// Official Payslip Details Screen (Screen 5: Official Payslip Details)
/// Read-only view: no confirmation or feedback, no data recalculation
class OfficialPayslipDetailView extends StatefulWidget {
  final int payslipId;

  const OfficialPayslipDetailView({super.key, required this.payslipId});

  @override
  State<OfficialPayslipDetailView> createState() =>
      _OfficialPayslipDetailViewState();
}

class _OfficialPayslipDetailViewState extends State<OfficialPayslipDetailView> {
  final PayrollService _service = PayrollService();
  PayslipDetailModel? _detail;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  Future<void> _loadDetail() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final detail = await _service.getPayslipDetail(widget.payslipId);
      if (mounted) {
        setState(() {
          _detail = detail;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Official Payslip',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
        ),
        centerTitle: false,
        elevation: 0,
        backgroundColor: AppColors.surface,
        foregroundColor: AppColors.textPrimary,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Reload',
            onPressed: _isLoading ? null : _loadDetail,
          ),
        ],
      ),
      body: _isLoading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            )
          : _errorMessage != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline_rounded,
                      size: 52,
                      color: AppColors.error,
                    ),
                    const SizedBox(height: 16),
                    Text(
                      _errorMessage!,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 15,
                        color: AppColors.error,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 20),
                    ElevatedButton.icon(
                      onPressed: _loadDetail,
                      icon: const Icon(Icons.refresh_rounded, size: 18),
                      label: const Text('Retry'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            )
          : _detail == null
          ? const Center(child: Text('Official payslip data not found.'))
          : _buildContent(),
    );
  }

  Widget _buildContent() {
    final payslip = _detail!.payslip;
    final workDetails = _detail!.workDetails;
    final calcDetails = _detail!.calculationDetails;

    return RefreshIndicator(
      onRefresh: _loadDetail,
      color: AppColors.primary,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
        children: [
          // 1. Snapshot Period & Employee Info Header
          _buildHeaderCard(payslip),
          const SizedBox(height: 16),

          // 2. Net Salary Overview (Official Published)
          _buildNetSalaryCard(payslip),
          const SizedBox(height: 16),

          // 3. Financial Breakdown
          _buildFinancialBreakdownCard(payslip),
          const SizedBox(height: 16),

          // 4. Official Shifts Details (WorkDetails)
          _buildShiftDetailsCard(workDetails),
          const SizedBox(height: 16),

          // 5. Calculation Line Items (if any additional items)
          if (calcDetails.isNotEmpty) ...[
            _buildCalculationItemsCard(calcDetails),
            const SizedBox(height: 16),
          ],

          // 6. Notice Banner: Read-only Snapshot
          _buildReadOnlyNoticeCard(),
        ],
      ),
    );
  }

  /// 1. Header Card: Period & Employee Information
  Widget _buildHeaderCard(PayslipSummaryItem payslip) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFFDCFCE7), // Green 100
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  payslip.periodCode.isNotEmpty
                      ? payslip.periodCode
                      : 'Period #${payslip.payrollPeriodId}',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF15803D),
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: AppColors.success.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: AppColors.success.withValues(alpha: 0.3),
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(
                      Icons.verified_rounded,
                      size: 13,
                      color: AppColors.success,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      PayslipStatus.getLabel(payslip.status),
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: AppColors.success,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            payslip.periodName.isNotEmpty
                ? payslip.periodName
                : 'Payslip for Period #${payslip.payrollPeriodId}',
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(
                Icons.person_outline_rounded,
                size: 15,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: 6),
              Text(
                '${payslip.employeeName}${payslip.employeePosition != null && payslip.employeePosition!.isNotEmpty ? ' • ${payslip.employeePosition}' : ''}',
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          if (payslip.branchName.isNotEmpty) ...[
            const SizedBox(height: 4),
            Row(
              children: [
                const Icon(
                  Icons.storefront_outlined,
                  size: 15,
                  color: AppColors.textMuted,
                ),
                const SizedBox(width: 6),
                Text(
                  payslip.branchName,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textMuted,
                  ),
                ),
              ],
            ),
          ],
          const Divider(height: 24, color: AppColors.borderLight),
          Row(
            children: [
              const Icon(
                Icons.date_range_rounded,
                size: 15,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: 6),
              const Text(
                'Date range: ',
                style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
              ),
              Text(
                '${payslip.periodStart} - ${payslip.periodEnd}',
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(
                Icons.event_available_rounded,
                size: 15,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 6),
              const Text(
                'Official publication date: ',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
              Text(
                formatVietnamDateTime(payslip.publishedAt),
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  /// 2. Official Net Salary Overview
  Widget _buildNetSalaryCard(PayslipSummaryItem payslip) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [
            Color(0xFF0F766E),
            Color(0xFF0D9488),
          ], // Teal theme for official
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF0D9488).withValues(alpha: 0.25),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'OFFICIAL NET SALARY',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w700,
                  color: Color(0xFF99F6E4),
                  letterSpacing: 0.5,
                ),
              ),
              Icon(Icons.lock_rounded, size: 16, color: Color(0xFF99F6E4)),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            formatCurrency(payslip.netSalary),
            style: const TextStyle(
              fontSize: 28,
              fontWeight: FontWeight.w900,
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 12),
          const Divider(color: Colors.white24, height: 1),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _buildMiniOverviewStat(
                'Total Earnings',
                formatCurrency(payslip.grossSalary),
              ),
              _buildMiniOverviewStat(
                'Total Deductions',
                formatCurrency(payslip.totalDeduction),
              ),
              _buildMiniOverviewStat(
                'Total Work Hours',
                '${payslip.totalWorkHours}h',
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildMiniOverviewStat(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: Color(0xFFCCFBF1)),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.bold,
            color: Colors.white,
          ),
        ),
      ],
    );
  }

  /// 3. Official Financial Breakdown
  Widget _buildFinancialBreakdownCard(PayslipSummaryItem payslip) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.receipt_long_rounded,
                size: 18,
                color: Color(0xFF0F766E),
              ),
              SizedBox(width: 8),
              Text(
                'Official Earnings & Deductions Breakdown',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          _buildDetailRow(
            'Base Work Pay (${payslip.totalWorkHours} hrs @ ${formatCurrency(payslip.hourlyRate)})',
            formatCurrency(payslip.workPay),
          ),
          if (payslip.totalReward > 0)
            _buildDetailRow(
              'Bonus / Reward',
              '+${formatCurrency(payslip.totalReward)}',
              valueColor: AppColors.success,
            ),
          _buildDetailRow(
            'Gross Earnings',
            formatCurrency(payslip.grossSalary),
            isBold: true,
          ),
          const Divider(height: 20, color: AppColors.borderLight),
          if (payslip.totalPenalty > 0)
            _buildDetailRow(
              'Penalties',
              '-${formatCurrency(payslip.totalPenalty)}',
              valueColor: AppColors.error,
            ),
          if (payslip.attendanceDeduction > 0)
            _buildDetailRow(
              'Attendance / Late Deductions',
              '-${formatCurrency(payslip.attendanceDeduction)}',
              valueColor: AppColors.error,
            ),
          if (payslip.carryForwardDeduction > 0)
            _buildDetailRow(
              'Debt Carried Forward from Prior Period',
              '-${formatCurrency(payslip.carryForwardDeduction)}',
              valueColor: AppColors.error,
            ),
          if (payslip.deferredDeductionAmount > 0)
            _buildDetailRow(
              'Deduction Deferred to Next Period',
              formatCurrency(payslip.deferredDeductionAmount),
              valueColor: AppColors.warning,
            ),
          _buildDetailRow(
            'Total Deductions',
            '-${formatCurrency(payslip.totalDeduction)}',
            valueColor: AppColors.error,
            isBold: true,
          ),
          const Divider(height: 20, color: AppColors.border),
          _buildDetailRow(
            'Net Salary',
            formatCurrency(payslip.netSalary),
            valueColor: const Color(0xFF0F766E),
            isBold: true,
            isLarge: true,
          ),
        ],
      ),
    );
  }

  Widget _buildDetailRow(
    String label,
    String value, {
    Color? valueColor,
    bool isBold = false,
    bool isLarge = false,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Text(
              label,
              style: TextStyle(
                fontSize: isLarge ? 15 : 13,
                fontWeight: isBold ? FontWeight.bold : FontWeight.w500,
                color: isBold ? AppColors.textPrimary : AppColors.textSecondary,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            value,
            style: TextStyle(
              fontSize: isLarge ? 17 : 13,
              fontWeight: isBold ? FontWeight.bold : FontWeight.w600,
              color: valueColor ?? AppColors.textPrimary,
            ),
          ),
        ],
      ),
    );
  }

  /// 4. Shift Work Details (WorkDetails)
  Widget _buildShiftDetailsCard(List<PayslipWorkDetailItem> workDetails) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Theme(
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          initiallyExpanded: false,
          leading: const Icon(
            Icons.calendar_month_rounded,
            color: Color(0xFF0F766E),
          ),
          title: Text(
            'Shift Details (${workDetails.length} shifts)',
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          children: [
            const Divider(height: 1, color: AppColors.borderLight),
            if (workDetails.isNotEmpty)
              ...workDetails.map((shift) => _buildShiftDetailItem(shift))
            else
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text(
                  'No shift work data available.',
                  style: TextStyle(color: AppColors.textMuted),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildShiftDetailItem(PayslipWorkDetailItem shift) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: AppColors.borderLight)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: const Color(0xFFCCFBF1), // Teal 100
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(
              Icons.timer_outlined,
              size: 18,
              color: Color(0xFF0F766E),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      shift.shiftName,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    Text(
                      '${(shift.payableMinutes / 60.0).toStringAsFixed(1)} hrs',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF0F766E),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 3),
                Text(
                  'Date: ${shift.workDate}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  'Attendance: ${shift.checkInTime != null ? formatVietnamDateTime(shift.checkInTime) : '--:--'} - ${shift.checkOutTime != null ? formatVietnamDateTime(shift.checkOutTime) : '--:--'}',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textMuted,
                  ),
                ),
                if (shift.totalLateMinutes > 0 ||
                    shift.totalEarlyLeaveMinutes > 0) ...[
                  const SizedBox(height: 3),
                  Text(
                    'Late: ${shift.totalLateMinutes}m • Early Leave: ${shift.totalEarlyLeaveMinutes}m',
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppColors.error,
                    ),
                  ),
                ],
                if (shift.note != null && shift.note!.isNotEmpty) ...[
                  const SizedBox(height: 3),
                  Text(
                    'Note: ${shift.note}',
                    style: const TextStyle(
                      fontSize: 11,
                      fontStyle: FontStyle.italic,
                      color: AppColors.textMuted,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// 5. Specific Calculation Line Items (CalculationDetails)
  Widget _buildCalculationItemsCard(
    List<PayrollCalculationDetailItem> calcDetails,
  ) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Theme(
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          initiallyExpanded: false,
          leading: const Icon(
            Icons.calculate_outlined,
            color: Color(0xFF0F766E),
          ),
          title: Text(
            'Specific Line Items (${calcDetails.length} items)',
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          children: [
            const Divider(height: 1, color: AppColors.borderLight),
            ...calcDetails.map((item) {
              return Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 10,
                ),
                decoration: const BoxDecoration(
                  border: Border(
                    bottom: BorderSide(color: AppColors.borderLight),
                  ),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            item.description,
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          if (item.note != null && item.note!.isNotEmpty)
                            Text(
                              item.note!,
                              style: const TextStyle(
                                fontSize: 11,
                                color: AppColors.textMuted,
                              ),
                            ),
                        ],
                      ),
                    ),
                    Text(
                      formatCurrency(item.amount),
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                  ],
                ),
              );
            }),
          ],
        ),
      ),
    );
  }

  /// 6. Read-only frozen snapshot notice
  Widget _buildReadOnlyNoticeCard() {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFF1F5F9), // Slate 100
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.border),
      ),
      child: const Row(
        children: [
          Icon(Icons.lock_rounded, size: 20, color: AppColors.textSecondary),
          SizedBox(width: 10),
          Expanded(
            child: Text(
              'This official payslip has been approved and published. All figures are permanently frozen snapshots and cannot be modified.',
              style: TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                height: 1.4,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
