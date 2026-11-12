import 'package:flutter/material.dart';
import '../../models/payroll/payroll_preview_model.dart';
import '../../services/payroll/payroll_service.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';

/// Payroll Preview Details Screen (Screen 2: Payroll Preview Details)
class PayrollPreviewDetailView extends StatefulWidget {
  final int feedbackId;

  const PayrollPreviewDetailView({super.key, required this.feedbackId});

  @override
  State<PayrollPreviewDetailView> createState() =>
      _PayrollPreviewDetailViewState();
}

class _PayrollPreviewDetailViewState extends State<PayrollPreviewDetailView> {
  final PayrollService _service = PayrollService();
  PayrollPreviewDetail? _detail;
  bool _isLoading = true;
  String? _errorMessage;
  bool _isActionSubmitting = false;

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
      final detail = await _service.getPayrollPreviewDetail(widget.feedbackId);
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

  /// Handle Confirm payroll preview
  Future<void> _handleConfirm() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.check_circle_outline_rounded, color: AppColors.success),
            SizedBox(width: 8),
            Text(
              'Confirm Payroll Preview',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: const Text(
          'Do you agree with the payroll calculation in this preview?\n\n'
          'Once confirmed, this review round will be marked as complete and you will no longer be able to submit feedback for this round.',
          style: TextStyle(
            fontSize: 14,
            color: AppColors.textSecondary,
            height: 1.5,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Cancel',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.success,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    setState(() => _isActionSubmitting = true);
    try {
      await _service.confirmPayrollPreview(widget.feedbackId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Payroll preview confirmed successfully!'),
            backgroundColor: AppColors.success,
          ),
        );
        _loadDetail();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: $e'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isActionSubmitting = false);
    }
  }

  /// Open Bottom Sheet to submit feedback
  void _openFeedbackBottomSheet() {
    final textController = TextEditingController();
    final formKey = GlobalKey<FormState>();

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) {
        return Padding(
          padding: EdgeInsets.only(
            bottom: MediaQuery.of(ctx).viewInsets.bottom,
          ),
          child: Container(
            decoration: const BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
            ),
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
            child: Form(
              key: formKey,
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 4,
                      decoration: BoxDecoration(
                        color: AppColors.border,
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  const Row(
                    children: [
                      Icon(
                        Icons.rate_review_outlined,
                        color: AppColors.warning,
                      ),
                      SizedBox(width: 8),
                      Text(
                        'Submit Payroll Feedback',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Please describe in detail any discrepancies regarding shifts, work hours, salary rates, or adjustments for accountant verification.',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                      height: 1.4,
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: textController,
                    maxLines: 4,
                    maxLength: 2000,
                    autofocus: true,
                    decoration: InputDecoration(
                      hintText: 'Enter feedback details (required)...',
                      hintStyle: const TextStyle(
                        fontSize: 13,
                        color: AppColors.textMuted,
                      ),
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.border),
                      ),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(color: AppColors.border),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(12),
                        borderSide: const BorderSide(
                          color: AppColors.primary,
                          width: 1.5,
                        ),
                      ),
                      filled: true,
                      fillColor: AppColors.background,
                      contentPadding: const EdgeInsets.all(14),
                    ),
                    validator: (val) {
                      if (val == null || val.trim().isEmpty) {
                        return 'Please enter feedback details.';
                      }
                      if (val.trim().length < 5) {
                        return 'Feedback must be at least 5 characters.';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: () => Navigator.pop(ctx),
                          style: OutlinedButton.styleFrom(
                            padding: const EdgeInsets.symmetric(vertical: 14),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                            side: const BorderSide(color: AppColors.border),
                          ),
                          child: const Text(
                            'Cancel',
                            style: TextStyle(color: AppColors.textSecondary),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: ElevatedButton(
                          onPressed: () async {
                            if (!formKey.currentState!.validate()) return;
                            final description = textController.text.trim();
                            Navigator.pop(ctx);
                            await _submitFeedback(description);
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.primary,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(vertical: 14),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                          ),
                          child: const Text(
                            'Submit Feedback',
                            style: TextStyle(fontWeight: FontWeight.bold),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Future<void> _submitFeedback(String description) async {
    setState(() => _isActionSubmitting = true);
    try {
      await _service.submitPayrollFeedback(widget.feedbackId, description);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Feedback submitted successfully! The accountant will review it.',
            ),
            backgroundColor: AppColors.success,
          ),
        );
        _loadDetail();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: $e'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isActionSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Payroll Preview Details',
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
          ? const Center(child: Text('No data found.'))
          : _buildContent(),
      bottomNavigationBar: _detail != null ? _buildBottomActions() : null,
    );
  }

  Widget _buildContent() {
    final feedback = _detail!.feedback;
    final payroll = _detail!.originalPayroll;

    return RefreshIndicator(
      onRefresh: _loadDetail,
      color: AppColors.primary,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
        children: [
          // 1. Header Card: Period, Version, Round, Status & Deadline
          _buildHeaderCard(feedback),
          const SizedBox(height: 16),

          // 2. Explanation / Notes Card (if present)
          if ((feedback.resolutionNote != null &&
                  feedback.resolutionNote!.isNotEmpty) ||
              (feedback.resultNote != null &&
                  feedback.resultNote!.isNotEmpty) ||
              (feedback.previousResolutionNote != null &&
                  feedback.previousResolutionNote!.isNotEmpty)) ...[
            _buildExplanationCard(feedback),
            const SizedBox(height: 16),
          ],

          // 3. Submitted Feedback Card (if staff already submitted)
          if (feedback.description != null &&
              feedback.description!.isNotEmpty) ...[
            _buildStaffFeedbackCard(feedback),
            const SizedBox(height: 16),
          ],

          // 4. Corrections Status (if any correction request was linked)
          if (feedback.corrections.isNotEmpty) ...[
            _buildCorrectionsCard(feedback.corrections),
            const SizedBox(height: 16),
          ],

          // 5. Net Salary Highlight Card
          _buildSalaryOverviewCard(payroll),
          const SizedBox(height: 16),

          // 6. Financial Breakdown Card
          _buildFinancialBreakdownCard(payroll),
          const SizedBox(height: 16),

          // 7. Shifts Details Accordion
          _buildShiftDetailsCard(payroll),
          const SizedBox(height: 16),

          // 8. Pending Source Adjustments (if any)
          if (_detail!.pendingSourceAdjustments.isNotEmpty) ...[
            _buildPendingAdjustmentsCard(_detail!.pendingSourceAdjustments),
            const SizedBox(height: 16),
          ],

          // 9. Review History
          _buildReviewHistoryCard(feedback),
        ],
      ),
    );
  }

  /// 1. General Card: Period, Version, Round, Deadline
  Widget _buildHeaderCard(PayrollPreviewItem feedback) {
    final statusColor = PayrollReviewStatus.getColor(feedback.reviewStatus);
    final statusLabel = PayrollReviewStatus.getLabel(feedback.reviewStatus);

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
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  'Period #${feedback.payrollPeriodId}',
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: AppColors.primary,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 4,
                ),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.12),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                ),
                child: Text(
                  statusLabel,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                    color: statusColor,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            feedback.employeeName,
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              _buildBadgeItem(
                Icons.layers_outlined,
                'Version V${feedback.payrollVersionNo}',
              ),
              const SizedBox(width: 12),
              _buildBadgeItem(
                Icons.repeat_rounded,
                'Review Round #${feedback.roundNo}',
              ),
            ],
          ),
          const Divider(height: 24, color: AppColors.borderLight),
          Row(
            children: [
              const Icon(
                Icons.alarm_rounded,
                size: 16,
                color: AppColors.warning,
              ),
              const SizedBox(width: 6),
              const Text(
                'Feedback deadline: ',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                  color: AppColors.textSecondary,
                ),
              ),
              Expanded(
                child: Text(
                  formatVietnamDateTime(feedback.reviewDeadlineAt),
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w700,
                    color: AppColors.warning,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(
                Icons.send_rounded,
                size: 15,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 6),
              const Text(
                'Sent: ',
                style: TextStyle(fontSize: 12, color: AppColors.textMuted),
              ),
              Text(
                formatVietnamDateTime(feedback.createdAt),
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildBadgeItem(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: AppColors.textSecondary),
        const SizedBox(width: 4),
        Text(
          text,
          style: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            color: AppColors.textSecondary,
          ),
        ),
      ],
    );
  }

  /// 2. Accountant Explanation / Notes
  Widget _buildExplanationCard(PayrollPreviewItem feedback) {
    final note = feedback.resolutionNote ?? feedback.resultNote ?? '';
    final prevNote = feedback.previousResolutionNote;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFF0F9FF), // Sky blue light
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFBAE6FD)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.info_outline_rounded,
                size: 18,
                color: Color(0xFF0284C7),
              ),
              SizedBox(width: 8),
              Text(
                'Accountant Explanation',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF0284C7),
                ),
              ),
            ],
          ),
          if (note.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(
              note,
              style: const TextStyle(
                fontSize: 14,
                color: AppColors.textPrimary,
                height: 1.4,
              ),
            ),
          ],
          if (prevNote != null && prevNote.isNotEmpty) ...[
            const SizedBox(height: 10),
            Text(
              'Previous round note: $prevNote',
              style: const TextStyle(
                fontSize: 12,
                fontStyle: FontStyle.italic,
                color: AppColors.textSecondary,
              ),
            ),
          ],
        ],
      ),
    );
  }

  /// 3. Staff's Submitted Feedback
  Widget _buildStaffFeedbackCard(PayrollPreviewItem feedback) {
    final statusColor = StaffFeedbackStatus.getColor(feedback.status);
    final statusLabel = StaffFeedbackStatus.getLabel(feedback.status);

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
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Row(
                children: [
                  Icon(
                    Icons.chat_bubble_outline_rounded,
                    size: 16,
                    color: AppColors.primary,
                  ),
                  SizedBox(width: 6),
                  Text(
                    'Your Submitted Feedback',
                    style: TextStyle(
                      fontSize: 14,
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
              if (feedback.status != null)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    statusLabel,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: statusColor,
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            feedback.description!,
            style: const TextStyle(
              fontSize: 14,
              color: AppColors.textSecondary,
              height: 1.4,
            ),
          ),
          if (feedback.staffRespondedAt != null) ...[
            const SizedBox(height: 8),
            Text(
              'Submitted at: ${formatVietnamDateTime(feedback.staffRespondedAt)}',
              style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
          ],
        ],
      ),
    );
  }

  /// 4. Linked Correction Request
  Widget _buildCorrectionsCard(List<PayrollIssueItem> corrections) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF3C7), // Light amber
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFFDE68A)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.build_circle_outlined,
                size: 18,
                color: Color(0xFFD97706),
              ),
              SizedBox(width: 8),
              Text(
                'Correction Request Details',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF92400E),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...corrections.map((issue) {
            return Padding(
              padding: const EdgeInsets.only(top: 6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        '#${issue.correctionRequestId} - Status: ',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: Color(0xFF92400E),
                        ),
                      ),
                      Text(
                        issue.status,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.bold,
                          color: AppColors.primary,
                        ),
                      ),
                    ],
                  ),
                  Text(
                    'Reason: ${issue.reason}',
                    style: const TextStyle(
                      fontSize: 13,
                      color: Color(0xFF78350F),
                    ),
                  ),
                  if (issue.responseNote != null &&
                      issue.responseNote!.isNotEmpty)
                    Text(
                      'Manager Response: ${issue.responseNote}',
                      style: const TextStyle(
                        fontSize: 12,
                        fontStyle: FontStyle.italic,
                        color: Color(0xFF92400E),
                      ),
                    ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  /// 5. Net Salary Highlight
  Widget _buildSalaryOverviewCard(PayrollCalculationItem payroll) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [AppColors.homeHeroStart, AppColors.homeHeroEnd],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.25),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'ESTIMATED NET SALARY (PREVIEW)',
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: Color(0xFFBFDBFE),
              letterSpacing: 0.5,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            formatCurrency(payroll.netSalary),
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
                formatCurrency(payroll.grossSalary),
              ),
              _buildMiniOverviewStat(
                'Total Deductions',
                formatCurrency(payroll.totalDeduction),
              ),
              _buildMiniOverviewStat(
                'Total Work Hours',
                '${_formatWorkHours(payroll.totalWorkHours)}h',
              ),
            ],
          ),
        ],
      ),
    );
  }

  String _formatWorkHours(num hours) => formatWorkHours(hours);

  Widget _buildMiniOverviewStat(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: Color(0xFF93C5FD)),
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

  /// 6. Earnings & Deductions Breakdown
  Widget _buildFinancialBreakdownCard(PayrollCalculationItem payroll) {
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
                Icons.account_balance_wallet_outlined,
                size: 18,
                color: AppColors.primary,
              ),
              SizedBox(width: 8),
              Text(
                'Earnings & Deductions Breakdown',
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
            'Base Work Pay (${_formatWorkHours(payroll.totalWorkHours)} hrs @ ${formatCurrency(payroll.hourlyRate)})',
            formatCurrency(payroll.workPay),
          ),
          if (payroll.totalReward > 0)
            _buildDetailRow(
              'Bonus / Reward',
              '+${formatCurrency(payroll.totalReward)}',
              valueColor: AppColors.success,
            ),
          _buildDetailRow(
            'Gross Earnings',
            formatCurrency(payroll.grossSalary),
            isBold: true,
          ),
          const Divider(height: 20, color: AppColors.borderLight),
          if (payroll.totalPenalty > 0)
            _buildDetailRow(
              'Penalties',
              '-${formatCurrency(payroll.totalPenalty)}',
              valueColor: AppColors.error,
            ),
          if (payroll.attendanceDeduction > 0)
            _buildDetailRow(
              'Attendance / Late Deductions',
              '-${formatCurrency(payroll.attendanceDeduction)}',
              valueColor: AppColors.error,
            ),
          if (payroll.carryForwardDeduction > 0)
            _buildDetailRow(
              'Debt Carried Forward from Prior Period',
              '-${formatCurrency(payroll.carryForwardDeduction)}',
              valueColor: AppColors.error,
            ),
          if (payroll.deferredDeductionAmount > 0)
            _buildDetailRow(
              'Deduction Deferred to Next Period',
              formatCurrency(payroll.deferredDeductionAmount),
              valueColor: AppColors.warning,
            ),
          _buildDetailRow(
            'Total Deductions',
            '-${formatCurrency(payroll.totalDeduction)}',
            valueColor: AppColors.error,
            isBold: true,
          ),
          const Divider(height: 20, color: AppColors.border),
          _buildDetailRow(
            'Net Salary',
            formatCurrency(payroll.netSalary),
            valueColor: AppColors.primary,
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

  /// 7. Shift Work Details
  Widget _buildShiftDetailsCard(PayrollCalculationItem payroll) {
    final workDetails = payroll.workDetails;
    final workRowsFromDetails = payroll.details
        .where((d) => d.type == 'WORK')
        .toList();

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
            Icons.event_note_rounded,
            color: AppColors.primary,
          ),
          title: Text(
            'Shift Details (${workDetails.isNotEmpty ? workDetails.length : workRowsFromDetails.length} shifts)',
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
            else if (workRowsFromDetails.isNotEmpty)
              ...workRowsFromDetails.map(
                (detail) => _buildFallbackDetailItem(detail),
              )
            else
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text(
                  'No shift detail data available.',
                  style: TextStyle(color: AppColors.textMuted),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildShiftDetailItem(ShiftWorkDetailItem shift) {
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
              color: AppColors.primaryLight,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(
              Icons.access_time_filled_rounded,
              size: 18,
              color: AppColors.primary,
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
                        color: AppColors.primary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 3),
                Text(
                  'Date: ${shift.workDate != null ? formatVietnamDate(shift.workDate) : 'N/A'}',
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
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFallbackDetailItem(PayrollCalculationDetailItem detail) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: AppColors.borderLight)),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Text(
              detail.description,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textPrimary,
              ),
            ),
          ),
          Text(
            formatCurrency(detail.amount),
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.bold,
              color: AppColors.primary,
            ),
          ),
        ],
      ),
    );
  }

  /// 8. Pending Adjustments
  Widget _buildPendingAdjustmentsCard(
    List<PendingPayrollAdjustmentItem> adjustments,
  ) {
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
          const Text(
            'Pending Adjustments (not included in this period)',
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.bold,
              color: AppColors.warning,
            ),
          ),
          const SizedBox(height: 8),
          ...adjustments.map((a) {
            return Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    '${a.type == 'REWARD' ? '[Reward]' : '[Penalty]'} ${a.name}',
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  Text(
                    formatCurrency(a.amount),
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  /// 9. Review Rounds History
  Widget _buildReviewHistoryCard(PayrollPreviewItem feedback) {
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
                Icons.history_rounded,
                size: 18,
                color: AppColors.textSecondary,
              ),
              SizedBox(width: 8),
              Text(
                'Preview Rounds History',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            'Current Round: #${feedback.roundNo} (Version ${feedback.payrollVersionNo})',
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            'Status: ${PayrollReviewStatus.getLabel(feedback.reviewStatus)}',
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: PayrollReviewStatus.getColor(feedback.reviewStatus),
            ),
          ),
          if (feedback.closedAt != null) ...[
            const SizedBox(height: 4),
            Text(
              'Closed at: ${formatVietnamDateTime(feedback.closedAt)} (${feedback.closureReason ?? ''})',
              style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
          ],
        ],
      ),
    );
  }

  /// Bottom Actions: CONFIRM and SUBMIT FEEDBACK
  Widget _buildBottomActions() {
    final feedback = _detail!.feedback;
    final canConfirm = feedback.canConfirm;
    final canFeedback = feedback.canSubmitFeedback;

    if (!canConfirm && !canFeedback) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        color: AppColors.surface,
        child: SafeArea(
          child: Row(
            children: [
              const Icon(
                Icons.lock_outline_rounded,
                size: 18,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  feedback.reviewStatus == PayrollReviewStatus.confirmed
                      ? 'You have already confirmed this payroll preview.'
                      : feedback.reviewStatus == PayrollReviewStatus.expired
                      ? 'This payroll preview has expired for feedback.'
                      : 'This round is view-only.',
                  style: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textMuted,
                  ),
                ),
              ),
            ],
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      decoration: BoxDecoration(
        color: AppColors.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, -3),
          ),
        ],
      ),
      child: SafeArea(
        child: Row(
          children: [
            // SUBMIT FEEDBACK button
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _isActionSubmitting || !canFeedback
                    ? null
                    : _openFeedbackBottomSheet,
                icon: const Icon(Icons.rate_review_outlined, size: 18),
                label: const Text(
                  'Report Discrepancy',
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                style: OutlinedButton.styleFrom(
                  foregroundColor: AppColors.warning,
                  side: const BorderSide(color: AppColors.warning),
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),

            // CONFIRM button
            Expanded(
              child: ElevatedButton.icon(
                onPressed: _isActionSubmitting || !canConfirm
                    ? null
                    : _handleConfirm,
                icon: _isActionSubmitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Icon(Icons.check_circle_outline_rounded, size: 18),
                label: const Text(
                  'Confirm Payroll Preview',
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.success,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  elevation: 2,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
