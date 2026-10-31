import 'package:flutter/material.dart';
import '../../models/employee_adjustment_model.dart';
import '../../services/staff_adjustment_service.dart';
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
        _errorMessage = 'Không thể tải chi tiết khen thưởng hoặc bản ghi không thuộc quyền sở hữu của bạn.';
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
                        const Icon(Icons.error_outline, size: 48, color: AppColors.error),
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
                          label: const Text('Thử lại'),
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
                                color: _getStatusColor(_reward!.approvalStatus).withOpacity(0.3),
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
                                        'Trạng thái: ${_reward!.approvalStatus}',
                                        style: TextStyle(
                                          fontSize: 16,
                                          fontWeight: FontWeight.bold,
                                          color: _getStatusColor(_reward!.approvalStatus),
                                        ),
                                      ),
                                      Text(
                                        _reward!.isApproved
                                            ? 'Khoản thưởng đã được phê duyệt và tính vào kỳ lương.'
                                            : _reward!.isRejected
                                                ? 'Khoản thưởng đã bị từ chối phê duyệt.'
                                                : 'Khoản thưởng đang chờ cấp quản lý / Admin xét duyệt.',
                                        style: TextStyle(
                                          fontSize: 12,
                                          color: _getStatusColor(_reward!.approvalStatus),
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
                                  'Số tiền khen thưởng',
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
                                  'Thông tin khen thưởng',
                                  style: TextStyle(
                                    fontSize: 16,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 14),
                                _buildInfoRow('Tên khoản thưởng', _reward!.adjustmentName),
                                _buildInfoRow('Kỳ lương áp dụng', '${_reward!.periodCode} - ${_reward!.periodName}'),
                                if (_reward!.createdAt != null)
                                  _buildInfoRow('Ngày tạo', formatVietnamDateTime(_reward!.createdAt)),
                                if (_reward!.createdByName != null)
                                  _buildInfoRow('Người tạo đề xuất', _reward!.createdByName!),
                                const Divider(height: 24),
                                const Text(
                                  'Lý do khen thưởng',
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 6),
                                Text(
                                  _reward!.reason.isNotEmpty ? _reward!.reason : 'Không có ghi chú thêm.',
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
                          if (_reward!.approvedAt != null || _reward!.rejectedAt != null || _reward!.reviewNote != null)
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
                                    'Thông tin xét duyệt',
                                    style: TextStyle(
                                      fontSize: 16,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  if (_reward!.approvedByName != null)
                                    _buildInfoRow('Người duyệt', _reward!.approvedByName!),
                                  if (_reward!.approvedAt != null)
                                    _buildInfoRow('Thời gian duyệt', formatVietnamDateTime(_reward!.approvedAt)),
                                  if (_reward!.rejectedByName != null)
                                    _buildInfoRow('Người từ chối', _reward!.rejectedByName!),
                                  if (_reward!.rejectedAt != null)
                                    _buildInfoRow('Thời gian từ chối', formatVietnamDateTime(_reward!.rejectedAt)),
                                  if (_reward!.reviewNote != null && _reward!.reviewNote!.trim().isNotEmpty) ...[
                                    const SizedBox(height: 8),
                                    const Text(
                                      'Ghi chú xét duyệt:',
                                      style: TextStyle(fontSize: 13, color: AppColors.textSecondary, fontWeight: FontWeight.w600),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      _reward!.reviewNote!,
                                      style: const TextStyle(fontSize: 14, color: AppColors.textPrimary),
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
