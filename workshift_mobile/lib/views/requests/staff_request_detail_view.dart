import 'package:flutter/material.dart';
import '../../controllers/staff_request_controller.dart';
import '../../models/staff_request_model.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';

/// View Employee Request Details for Staff
class StaffRequestDetailView extends StatefulWidget {
  final int requestId;
  final StaffRequestController controller;

  const StaffRequestDetailView({
    super.key,
    required this.requestId,
    required this.controller,
  });

  @override
  State<StaffRequestDetailView> createState() => _StaffRequestDetailViewState();
}

class _StaffRequestDetailViewState extends State<StaffRequestDetailView> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerUpdate);
    widget.controller.loadRequestDetail(widget.requestId);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  String _formatDateTime(DateTime? dt) {
    if (dt == null) return '--';
    final local = dt.toLocal();
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year.toString();
    final hour = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$day/$month/$year $hour:$min';
  }

  String _formatTime(DateTime? dt) {
    if (dt == null) return '--:--';
    final local = dt.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$hour:$min';
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '--';
    final local = dt.toLocal();
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year.toString();
    return '$day/$month/$year';
  }

  Future<void> _handleCancelRequest(StaffEmployeeRequestDetailModel detail) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.cancel_outlined, color: AppColors.error, size: 24),
            SizedBox(width: 8),
            Text(
              'Cancel Request',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          'Are you sure you want to cancel request #${detail.requestId} (${detail.typeDisplayName})? This action cannot be undone.',
          style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Close', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: const Text('Confirm Cancel'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final success = await widget.controller.cancelRequest(detail.requestId);
      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Request cancelled successfully'),
              backgroundColor: AppColors.success,
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(widget.controller.actionErrorMessage ?? 'Failed to cancel request'),
              backgroundColor: AppColors.error,
            ),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final detail = widget.controller.selectedDetail;
    final isLoading = widget.controller.isLoadingDetail;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          'Request Details #${widget.requestId}',
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: isLoading
          ? const Center(
              child: CircularProgressIndicator(
                color: AppColors.primary,
                strokeWidth: 2.5,
              ),
            )
          : detail == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.error_outline, size: 48, color: AppColors.error),
                        const SizedBox(height: 12),
                        Text(
                          widget.controller.actionErrorMessage ?? 'Request details not found.',
                          textAlign: TextAlign.center,
                          style: const TextStyle(color: AppColors.textSecondary),
                        ),
                        const SizedBox(height: 16),
                        AppButton(
                          text: 'Retry',
                          onPressed: () => widget.controller.loadRequestDetail(widget.requestId),
                          width: 140,
                          height: 40,
                        ),
                      ],
                    ),
                  ),
                )
              : SingleChildScrollView(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // 1. Header Card: Request Type & Status
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.borderLight),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.02),
                              blurRadius: 8,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 48,
                              height: 48,
                              decoration: BoxDecoration(
                                color: detail.typeColor.withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Icon(
                                detail.typeIcon,
                                color: detail.typeColor,
                                size: 24,
                              ),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    detail.typeDisplayName,
                                    style: const TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    'Request ID: #${detail.requestId}',
                                    style: const TextStyle(
                                      fontSize: 13,
                                      color: AppColors.textMuted,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            StatusBadge.forRequestStatus(detail.status),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // 2. General Information
                      _buildSectionTitle('Request Information'),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.borderLight),
                        ),
                        child: Column(
                          children: [
                            _buildInfoRow('Branch', detail.branchName, Icons.storefront_rounded),
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow('Submitted on', _formatDateTime(detail.createdAt), Icons.access_time_rounded),
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow(
                              'Reason',
                              detail.reason?.isNotEmpty == true ? detail.reason! : 'No reason provided',
                              Icons.description_outlined,
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // 3. Shift Details or Salary Increase Information
                      _buildSectionTitle(
                        detail.requestType == 'SALARY_INCREASE'
                            ? 'Salary Adjustment Details'
                            : 'Applicable Shifts',
                      ),
                      if (detail.requestType == 'SALARY_INCREASE' && detail.salaryIncreaseDetail != null)
                        _buildSalaryIncreaseCard(detail.salaryIncreaseDetail!)
                      else if (detail.requestType == 'LEAVE' && detail.leaveAssignments != null)
                        ...detail.leaveAssignments!.map((shift) => _buildLeaveShiftCard(shift))
                      else if ((detail.requestType == 'LATE_ARRIVAL' || detail.requestType == 'EARLY_LEAVE') &&
                          detail.timeAdjustmentDetail != null)
                        _buildTimeAdjustmentCard(detail.timeAdjustmentDetail!, detail.requestType)
                      else
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: AppColors.borderLight),
                          ),
                          child: const Text(
                            'No shift or salary information available.',
                            style: TextStyle(color: AppColors.textSecondary),
                          ),
                        ),
                      const SizedBox(height: 16),

                      // 4. Manager Review Result (if present)
                      if (!detail.isPending) ...[
                        _buildSectionTitle('Manager Feedback'),
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: detail.isApproved
                                ? const Color(0xFFF0FDF4)
                                : detail.isRejected
                                    ? const Color(0xFFFEF2F2)
                                    : Colors.white,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(
                              color: detail.isApproved
                                  ? const Color(0xFFBBF7D0)
                                  : detail.isRejected
                                      ? const Color(0xFFFECACA)
                                      : AppColors.borderLight,
                            ),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              _buildInfoRow(
                                'Reviewed by',
                                detail.reviewerFullName ?? 'Branch Manager',
                                Icons.person_outline_rounded,
                              ),
                              const Divider(height: 20, color: AppColors.borderLight),
                              _buildInfoRow(
                                'Reviewed at',
                                _formatDateTime(detail.reviewedAt),
                                Icons.event_available_rounded,
                              ),
                              if (detail.reviewNote != null && detail.reviewNote!.isNotEmpty) ...[
                                const Divider(height: 20, color: AppColors.borderLight),
                                _buildInfoRow(
                                  'Manager Notes',
                                  detail.reviewNote!,
                                  Icons.chat_bubble_outline_rounded,
                                ),
                              ],
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                      ],

                      // 5. Applied Policy Rules (if present)
                      if (detail.appliedRule != null) ...[
                        _buildSectionTitle('Applied Policy'),
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: AppColors.borderLight),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.info_outline_rounded, color: AppColors.info, size: 20),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  'Policy: Maximum ${detail.appliedRule!.maxRequestsPerMonth} requests of this type per month.',
                                  style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 24),
                      ],

                      // 6. Cancel Request Button (if PENDING)
                      if (detail.isPending) ...[
                        const SizedBox(height: 8),
                        SizedBox(
                          width: double.infinity,
                          child: OutlinedButton.icon(
                            onPressed: widget.controller.isCancelling
                                ? null
                                : () => _handleCancelRequest(detail),
                            icon: widget.controller.isCancelling
                                ? const SizedBox(
                                    width: 18,
                                    height: 18,
                                    child: CircularProgressIndicator(
                                      strokeWidth: 2,
                                      color: AppColors.error,
                                    ),
                                  )
                                : const Icon(Icons.cancel_outlined, color: AppColors.error, size: 20),
                            label: Text(
                              widget.controller.isCancelling ? 'Cancelling...' : 'Cancel this request',
                              style: const TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w600,
                                color: AppColors.error,
                              ),
                            ),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: AppColors.error, width: 1.5),
                              padding: const EdgeInsets.symmetric(vertical: 14),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            ),
                          ),
                        ),
                        const SizedBox(height: 24),
                      ],
                    ],
                  ),
                ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8, left: 4),
      child: Text(
        title,
        style: const TextStyle(
          fontSize: 15,
          fontWeight: FontWeight.bold,
          color: AppColors.textPrimary,
        ),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value, IconData icon) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: AppColors.textMuted),
        const SizedBox(width: 10),
        SizedBox(
          width: 110,
          child: Text(
            label,
            style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildLeaveShiftCard(StaffLeaveRequestAssignmentModel shift) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: const Color(0xFFEFF6FF),
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(Icons.work_outline_rounded, color: AppColors.primary, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  shift.shiftName,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${_formatDate(shift.scheduledStartAt)} • ${_formatTime(shift.scheduledStartAt)} - ${_formatTime(shift.scheduledEndAt)}',
                  style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                ),
                if (shift.branchName != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    shift.branchName!,
                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTimeAdjustmentCard(StaffTimeAdjustmentDetailModel detail, String requestType) {
    final isLate = requestType == 'LATE_ARRIVAL';
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: isLate ? const Color(0xFFFFEDD5) : const Color(0xFFF3E8FF),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(
                  isLate ? Icons.more_time_rounded : Icons.timer_off_rounded,
                  color: isLate ? const Color(0xFFEA580C) : const Color(0xFF7C3AED),
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      detail.shiftName,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${_formatDate(detail.scheduledStartAt)} • ${_formatTime(detail.scheduledStartAt)} - ${_formatTime(detail.scheduledEndAt)}',
                      style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const Divider(height: 24, color: AppColors.borderLight),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: isLate ? const Color(0xFFFFF7ED) : const Color(0xFFFAF5FF),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: isLate ? const Color(0xFFFED7AA) : const Color(0xFFE9D5FF),
              ),
            ),
            child: Row(
              children: [
                Icon(
                  isLate ? Icons.schedule_rounded : Icons.logout_rounded,
                  color: isLate ? const Color(0xFFEA580C) : const Color(0xFF7C3AED),
                  size: 18,
                ),
                const SizedBox(width: 10),
                Text(
                  isLate ? 'Expected arrival:' : 'Expected departure:',
                  style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                ),
                const Spacer(),
                Text(
                  _formatTime(detail.requestedTime),
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: isLate ? const Color(0xFFEA580C) : const Color(0xFF7C3AED),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSalaryIncreaseCard(StaffSalaryIncreaseDetailModel detail) {
    final curRate = detail.currentHourlyRate;
    final propRate = detail.proposedHourlyRate;
    final diff = propRate - curRate;
    final hasCurrentRate = curRate > 0;
    final percent = hasCurrentRate ? ((diff / curRate) * 100).toStringAsFixed(1) : null;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: const Color(0xFFEFF6FF),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: const Icon(
                  Icons.payments_rounded,
                  color: Color(0xFF2563EB),
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Hourly Rate Increase Request',
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      detail.contractNumber != null
                          ? 'Contract: ${detail.contractNumber}'
                          : 'Active Employment Contract',
                      style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const Divider(height: 24, color: AppColors.borderLight),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Current Rate',
                      style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      hasCurrentRate ? formatCurrency(curRate) : 'Not specified',
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.arrow_forward_rounded, color: AppColors.textMuted, size: 20),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    const Text(
                      'Proposed Rate',
                      style: TextStyle(fontSize: 12, color: Color(0xFF2563EB), fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      formatCurrency(propRate),
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF2563EB),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (hasCurrentRate && diff > 0) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: const Color(0xFFEFF6FF),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFBFDBFE)),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Icon(Icons.trending_up_rounded, size: 16, color: Color(0xFF2563EB)),
                  const SizedBox(width: 6),
                  Text(
                    'Increase of +${formatCurrency(diff)} ($percent%)',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: Color(0xFF2563EB),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

