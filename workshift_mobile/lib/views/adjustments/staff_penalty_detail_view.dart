import 'package:flutter/material.dart';
import '../../models/adjustments/employee_adjustment_model.dart';
import '../../services/adjustments/staff_adjustment_service.dart';
import '../../theme/app_colors.dart';
import '../../utils/format_utils.dart';

class StaffPenaltyDetailView extends StatefulWidget {
  final int penaltyId;

  const StaffPenaltyDetailView({super.key, required this.penaltyId});

  @override
  State<StaffPenaltyDetailView> createState() => _StaffPenaltyDetailViewState();
}

class _StaffPenaltyDetailViewState extends State<StaffPenaltyDetailView> {
  final StaffAdjustmentService _adjustmentService = StaffAdjustmentService();

  EmployeeAdjustmentModel? _penalty;
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
      final penalty = await _adjustmentService.getPenaltyDetail(
        widget.penaltyId,
      );
      if (!mounted) return;
      setState(() {
        _penalty = penalty;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage =
            'Unable to load penalty details or this record does not belong to you.';
        _isLoading = false;
      });
    }
  }

  Color _getStatusColor(String status) {
    switch (status.toUpperCase()) {
      case 'APPROVED':
        return AppColors.error;
      case 'PENDING':
        return AppColors.warning;
      case 'REJECTED':
        return AppColors.success;
      default:
        return AppColors.textSecondary;
    }
  }

  Color _getStatusBgColor(String status) {
    switch (status.toUpperCase()) {
      case 'APPROVED':
        return AppColors.errorBg;
      case 'PENDING':
        return AppColors.warningBg;
      case 'REJECTED':
        return AppColors.successBg;
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
          'Penalty Details',
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
          : _penalty == null
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
                      color: _getStatusBgColor(_penalty!.approvalStatus),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: _getStatusColor(
                          _penalty!.approvalStatus,
                        ).withValues(alpha: 0.3),
                      ),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          _penalty!.isApproved
                              ? Icons.gavel_rounded
                              : _penalty!.isRejected
                              ? Icons.cancel_rounded
                              : Icons.pending_rounded,
                          color: _getStatusColor(_penalty!.approvalStatus),
                          size: 28,
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Status: ${_penalty!.approvalStatus}',
                                style: TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  color: _getStatusColor(
                                    _penalty!.approvalStatus,
                                  ),
                                ),
                              ),
                              Text(
                                _penalty!.isApproved
                                    ? 'The penalty has been approved and will be deducted in payroll.'
                                    : _penalty!.isRejected
                                    ? 'The penalty has been rejected.'
                                    : 'The penalty is pending management review.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: _getStatusColor(
                                    _penalty!.approvalStatus,
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
                          'Penalty Deduction Amount',
                          style: TextStyle(
                            fontSize: 14,
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          '- ${formatCurrency(_penalty!.amount)}',
                          style: const TextStyle(
                            fontSize: 26,
                            fontWeight: FontWeight.w800,
                            color: AppColors.error,
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
                          'Penalty Violation Details',
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 14),
                        _buildInfoRow(
                          'Penalty Name',
                          _penalty!.adjustmentName,
                        ),
                        _buildInfoRow(
                          'Applicable Payroll Period',
                          '${_penalty!.periodCode} - ${_penalty!.periodName}',
                        ),
                        if (_penalty!.createdAt != null)
                          _buildInfoRow(
                            'Created Date',
                            formatVietnamDateTime(_penalty!.createdAt),
                          ),
                        if (_penalty!.createdByName != null)
                          _buildInfoRow(
                            'Created By',
                            _penalty!.createdByName!,
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
                          _penalty!.reason.isNotEmpty
                              ? _penalty!.reason
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
                  if (_penalty!.approvedAt != null ||
                      _penalty!.rejectedAt != null ||
                      _penalty!.reviewNote != null)
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
                          if (_penalty!.approvedByName != null)
                            _buildInfoRow(
                              'Approved By',
                              _penalty!.approvedByName!,
                            ),
                          if (_penalty!.approvedAt != null)
                            _buildInfoRow(
                              'Approved At',
                              formatVietnamDateTime(_penalty!.approvedAt),
                            ),
                          if (_penalty!.rejectedByName != null)
                            _buildInfoRow(
                              'Rejected By',
                              _penalty!.rejectedByName!,
                            ),
                          if (_penalty!.rejectedAt != null)
                            _buildInfoRow(
                              'Rejected At',
                              formatVietnamDateTime(_penalty!.rejectedAt),
                            ),
                          if (_penalty!.reviewNote != null &&
                              _penalty!.reviewNote!.trim().isNotEmpty) ...[
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
                              _penalty!.reviewNote!,
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
