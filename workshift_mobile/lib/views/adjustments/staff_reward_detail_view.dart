import 'package:flutter/material.dart';
import '../../models/adjustments/employee_adjustment_model.dart';
import '../../services/adjustments/staff_adjustment_service.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';

class StaffRewardDetailView extends StatefulWidget {
  final int rewardId;

  const StaffRewardDetailView({super.key, required this.rewardId});

  @override
  State<StaffRewardDetailView> createState() => _StaffRewardDetailViewState();
}

class _StaffRewardDetailViewState extends State<StaffRewardDetailView> {
  final StaffAdjustmentService _adjustmentService = StaffAdjustmentService();

  EmployeeAdjustmentModel? _reward;
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
      final reward = await _adjustmentService.getRewardDetail(widget.rewardId);
      if (!mounted) return;
      setState(() {
        _reward = reward;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage =
            'Unable to load reward details or this record does not belong to you.';
        _isLoading = false;
      });
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toUpperCase()) {
      case 'APPROVED':
        return AppColors.success;
      case 'PENDING':
        return AppColors.warning;
      case 'REJECTED':
        return AppColors.error;
      default:
        return AppColors.textSecondary;
    }
  }

  Color _getStatusBgColor(String status) {
    switch (status.toUpperCase()) {
      case 'APPROVED':
        return AppColors.successBg;
      case 'PENDING':
        return AppColors.warningBg;
      case 'REJECTED':
        return AppColors.errorBg;
      default:
        return AppColors.borderLight;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Reward Details',
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: AppColors.surface,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.textPrimary),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _errorMessage != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24.0),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 48,
                      color: AppColors.error,
                    ),
                    const SizedBox(height: 12),
                    Text(
                      _errorMessage!,
                      textAlign: TextAlign.center,
                      style: const TextStyle(color: AppColors.textSecondary),
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton.icon(
                      onPressed: _loadDetail,
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              ),
            )
          : _reward == null
          ? const Center(child: Text('Record not found.'))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Status banner
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: _getStatusBgColor(_reward!.approvalStatus),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: _getStatusColor(
                          _reward!.approvalStatus,
                        ).withValues(alpha: 0.3),
                      ),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          _reward!.isApproved
                              ? Icons.check_circle_rounded
                              : _reward!.isRejected
                              ? Icons.cancel_rounded
                              : Icons.pending_rounded,
                          color: _getStatusColor(_reward!.approvalStatus),
                          size: 28,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Status: ${_reward!.approvalStatus}',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: _getStatusColor(
                                    _reward!.approvalStatus,
                                  ),
                                ),
                              ),
                              Text(
                                _reward!.isApproved
                                    ? 'The reward has been approved and included in payroll.'
                                    : _reward!.isRejected
                                    ? 'The reward has been rejected.'
                                    : 'The reward is pending review by management.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: _getStatusColor(
                                    _reward!.approvalStatus,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Amount Card
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Reward Amount',
                          style: TextStyle(
                            fontSize: 14,
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          '+ ${formatCurrency(_reward!.amount)}',
                          style: const TextStyle(
                            fontSize: 26,
                            fontWeight: FontWeight.w800,
                            color: AppColors.success,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Details Card
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Reward Details',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 14),
                        _buildInfoRow(
                          'Reward Name',
                          _reward!.adjustmentName,
                        ),
                        _buildInfoRow(
                          'Applicable Payroll Period',
                          '${_reward!.periodCode} - ${_reward!.periodName}',
                        ),
                        if (_reward!.createdAt != null)
                          _buildInfoRow(
                            'Created Date',
                            formatVietnamDateTime(_reward!.createdAt),
                          ),
                        if (_reward!.createdByName != null)
                          _buildInfoRow(
                            'Created By',
                            _reward!.createdByName!,
                          ),
                        const Divider(height: 24),
                        const Text(
                          'Reason / Description',
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textSecondary,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          _reward!.reason.isNotEmpty
                              ? _reward!.reason
                              : 'No additional notes.',
                          style: const TextStyle(
                            fontSize: 14,
                            height: 1.5,
                            color: AppColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Review Info Card if reviewed
                  if (_reward!.approvedAt != null ||
                      _reward!.rejectedAt != null ||
                      _reward!.reviewNote != null)
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: AppColors.border),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Review Information',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 12),
                          if (_reward!.approvedByName != null)
                            _buildInfoRow(
                              'Approved By',
                              _reward!.approvedByName!,
                            ),
                          if (_reward!.approvedAt != null)
                            _buildInfoRow(
                              'Approved At',
                              formatVietnamDateTime(_reward!.approvedAt),
                            ),
                          if (_reward!.rejectedByName != null)
                            _buildInfoRow(
                              'Rejected By',
                              _reward!.rejectedByName!,
                            ),
                          if (_reward!.rejectedAt != null)
                            _buildInfoRow(
                              'Rejected At',
                              formatVietnamDateTime(_reward!.rejectedAt),
                            ),
                          if (_reward!.reviewNote != null &&
                              _reward!.reviewNote!.trim().isNotEmpty) ...[
                            const SizedBox(height: 8),
                            const Text(
                              'Review Notes:',
                              style: TextStyle(
                                fontSize: 13,
                                color: AppColors.textSecondary,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              _reward!.reviewNote!,
                              style: const TextStyle(
                                fontSize: 14,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                ],
              ),
            ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: AppColors.textPrimary,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
