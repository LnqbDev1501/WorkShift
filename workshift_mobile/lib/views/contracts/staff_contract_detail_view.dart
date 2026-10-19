import 'package:flutter/material.dart';
import '../../controllers/staff_contract_controller.dart';
import '../../models/staff_contract_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';
import 'submit_contract_change_request_view.dart';

/// Màn hình Chi tiết hợp đồng lao động của Staff
/// (View Details, View & Download PDF, Accept Contract, Submit Change Request, Resend Updated Contract)
class StaffContractDetailView extends StatefulWidget {
  final int contractId;
  final StaffContractController controller;

  const StaffContractDetailView({
    super.key,
    required this.contractId,
    required this.controller,
  });

  @override
  State<StaffContractDetailView> createState() => _StaffContractDetailViewState();
}

class _StaffContractDetailViewState extends State<StaffContractDetailView> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(_onControllerUpdate);
    widget.controller.loadContractDetail(widget.contractId);
  }

  @override
  void dispose() {
    widget.controller.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '--';
    final d = dt.day.toString().padLeft(2, '0');
    final m = dt.month.toString().padLeft(2, '0');
    final y = dt.year.toString();
    return '$d/$m/$y';
  }

  String _formatDateTime(DateTime? dt) {
    if (dt == null) return '--';
    final local = dt.toLocal();
    final d = local.day.toString().padLeft(2, '0');
    final m = local.month.toString().padLeft(2, '0');
    final y = local.year.toString();
    final h = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$d/$m/$y $h:$min';
  }

  Future<void> _handleAcceptContract(StaffContractDetailModel contract) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        titlePadding: const EdgeInsets.fromLTRB(20, 20, 20, 10),
        contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
        actionsPadding: const EdgeInsets.fromLTRB(16, 10, 16, 16),
        title: const Row(
          children: [
            Icon(Icons.verified_rounded, color: AppColors.success, size: 24),
            SizedBox(width: 8),
            Expanded(
              child: Text(
                'Xác nhận & Ký hợp đồng',
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Xác nhận ký kết hợp đồng điện tử ${contract.contractNumber}',
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            // Khung 2 cột mô phỏng chữ ký 2 bên
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: AppColors.borderLight),
              ),
              child: Row(
                children: [
                  // Cột trái: Người lao động
                  Expanded(
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: const Color(0xFFBFDBFE)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          const Text(
                            'NGƯỜI LAO ĐỘNG',
                            style: TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF1E40AF)),
                          ),
                          const SizedBox(height: 4),
                          const Icon(Icons.edit_document, color: Color(0xFF2563EB), size: 22),
                          const SizedBox(height: 4),
                          const Text(
                            'Sẵn sàng ký số',
                            style: TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: Color(0xFF2563EB)),
                          ),
                          Text(
                            contract.employeePosition,
                            style: const TextStyle(fontSize: 9, color: AppColors.textMuted),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Cột phải: WorkShift Stamp
                  Expanded(
                    child: Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(color: const Color(0xFFFECACA)),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          const Text(
                            'CÔNG TY WORKSHIFT',
                            style: TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF991B1B)),
                          ),
                          const SizedBox(height: 4),
                          Image.asset(
                            'assets/images/workshift_stamp.png',
                            height: 22,
                            fit: BoxFit.contain,
                            errorBuilder: (context, error, stackTrace) => const Text(
                              'WorkShift',
                              style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold, fontSize: 13),
                            ),
                          ),
                          const SizedBox(height: 4),
                          const Text(
                            '✓ Đã chứng thực',
                            style: TextStyle(fontSize: 9, fontWeight: FontWeight.w600, color: Color(0xFFDC2626)),
                          ),
                          Text(
                            contract.branchName,
                            style: const TextStyle(fontSize: 9, color: AppColors.textMuted),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFFF0FDF4),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: const Color(0xFFBBF7D0)),
              ),
              child: const Row(
                children: [
                  Icon(Icons.info_outline_rounded, color: AppColors.success, size: 16),
                  SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Hợp đồng sau khi ký sẽ có giá trị pháp lý chính thức và chuyển sang trạng thái Sắp bắt đầu / Có hiệu lực.',
                      style: TextStyle(fontSize: 11, color: Color(0xFF15803D), height: 1.3),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Xem lại', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton.icon(
            onPressed: () => Navigator.pop(ctx, true),
            icon: const Icon(Icons.draw_rounded, size: 16),
            label: const Text('Xác nhận & Ký'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.success,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final success = await widget.controller.acceptContract(contract.contractId);
      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Ký hợp đồng thành công!'),
              backgroundColor: AppColors.success,
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(widget.controller.actionErrorMessage ?? 'Ký hợp đồng thất bại'),
              backgroundColor: AppColors.error,
            ),
          );
        }
      }
    }
  }

  Future<void> _handleDownloadPdf(StaffContractDetailModel contract) async {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Đang tải tệp PDF hợp đồng...')),
    );
    final bytes = await widget.controller.fetchPdfBytes(contract.contractId, isDownload: true);
    if (mounted) {
      if (bytes != null && bytes.isNotEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Đã tải thành công tệp hợp đồng ${contract.contractNumber}.pdf (${bytes.lengthInBytes} bytes)'),
            backgroundColor: AppColors.success,
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(widget.controller.actionErrorMessage ?? 'Không thể tải tệp PDF'),
            backgroundColor: AppColors.error,
          ),
        );
      }
    }
  }

  void _showPdfPreviewDialog(StaffContractDetailModel contract) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            const Icon(Icons.picture_as_pdf_rounded, color: AppColors.error, size: 24),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                'Bản hợp đồng PDF: ${contract.contractNumber}',
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Tệp PDF chính thức được tạo và ký điện tử bởi hệ thống WorkShift Management.',
              style: TextStyle(fontSize: 13, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: AppColors.borderLight),
              ),
              child: Column(
                children: [
                  _buildPdfMetaRow('Số hợp đồng', contract.contractNumber),
                  const SizedBox(height: 6),
                  _buildPdfMetaRow('Trạng thái', contract.statusDisplayName),
                  const SizedBox(height: 6),
                  _buildPdfMetaRow('Vị trí', contract.employeePosition),
                  const SizedBox(height: 6),
                  _buildPdfMetaRow('Mức lương', contract.formattedHourlyRate),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Đóng', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton.icon(
            onPressed: () {
              Navigator.pop(ctx);
              _handleDownloadPdf(contract);
            },
            icon: const Icon(Icons.download_rounded, size: 18),
            label: const Text('Tải file PDF về máy'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPdfMetaRow(String label, String value) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: const TextStyle(fontSize: 12, color: AppColors.textMuted)),
        Text(value, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textPrimary)),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    final contract = widget.controller.selectedContract;
    final isLoading = widget.controller.isLoadingDetail;
    final isAccepting = widget.controller.isAccepting;
    final changeRequests = widget.controller.changeRequests;

    // Kiểm tra xem hợp đồng này có Change Request nào đã được Quản lý duyệt không (Resend Updated Contract)
    final hasApprovedUpdate = changeRequests.any((cr) => cr.isApproved);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          contract != null ? contract.contractNumber : 'Chi tiết hợp đồng',
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
        actions: [
          if (contract != null && contract.hasPdf)
            IconButton(
              tooltip: 'Tải PDF',
              icon: const Icon(Icons.download_rounded, color: AppColors.primary),
              onPressed: () => _handleDownloadPdf(contract),
            ),
        ],
      ),
      body: isLoading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
            )
          : contract == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.error_outline, size: 48, color: AppColors.error),
                        const SizedBox(height: 12),
                        Text(
                          widget.controller.actionErrorMessage ?? 'Không tìm thấy dữ liệu hợp đồng.',
                          style: const TextStyle(color: AppColors.textSecondary),
                        ),
                        const SizedBox(height: 16),
                        AppButton(
                          text: 'Thử lại',
                          onPressed: () => widget.controller.loadContractDetail(widget.contractId),
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
                      // Banner cảnh báo nếu hợp đồng được cập nhật lại (Resend Updated Employment Contract)
                      if (hasApprovedUpdate && contract.isPendingStaff) ...[
                        Container(
                          margin: const EdgeInsets.only(bottom: 16),
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFFFBEB),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: const Color(0xFFFDE68A)),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.mark_email_read_rounded, color: Color(0xFFD97706), size: 24),
                              const SizedBox(width: 12),
                              const Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      'Hợp đồng đã được Quản lý cập nhật lại!',
                                      style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.bold,
                                        color: Color(0xFF92400E),
                                      ),
                                    ),
                                    SizedBox(height: 2),
                                    Text(
                                      'Yêu cầu điều chỉnh của bạn đã được Quản lý phê duyệt và cập nhật vào bản hợp đồng này. Vui lòng kiểm tra lại thông tin và tiến hành ký xác nhận.',
                                      style: TextStyle(fontSize: 12, color: Color(0xFFB45309)),
                                    ),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],

                      // 1. Header Card: Số HĐ & Trạng thái
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
                                color: const Color(0xFFEFF6FF),
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: const Icon(
                                Icons.verified_user_rounded,
                                color: AppColors.primary,
                                size: 24,
                              ),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    contract.contractNumber,
                                    style: const TextStyle(
                                      fontSize: 17,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    'Gửi lúc: ${_formatDateTime(contract.sentToStaffAt ?? contract.createdAt)}',
                                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                                  ),
                                ],
                              ),
                            ),
                            StatusBadge.forContractStatus(contract.status),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // 2. Thông tin chính của hợp đồng
                      _buildSectionTitle('Thông tin công việc & quyền lợi'),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.borderLight),
                        ),
                        child: Column(
                          children: [
                            _buildInfoRow('Vị trí công việc', contract.employeePosition, Icons.badge_outlined),
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow('Mức lương mỗi giờ', contract.formattedHourlyRate, Icons.monetization_on_outlined, isHighlight: true),
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow(
                              'Thời hạn hợp đồng',
                              '${_formatDate(contract.startDate)} - ${contract.endDate != null ? _formatDate(contract.endDate) : "Không thời hạn"}',
                              Icons.calendar_today_rounded,
                            ),
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow('Chi nhánh', contract.branchName, Icons.storefront_rounded),
                            if (contract.branchAddress.isNotEmpty) ...[
                              const Divider(height: 20, color: AppColors.borderLight),
                              _buildInfoRow('Địa chỉ làm việc', contract.branchAddress, Icons.location_on_outlined),
                            ],
                            const Divider(height: 20, color: AppColors.borderLight),
                            _buildInfoRow('Khu vực lương', contract.regionName, Icons.map_outlined),
                            if (contract.createdByManagerName != null) ...[
                              const Divider(height: 20, color: AppColors.borderLight),
                              _buildInfoRow('Quản lý phụ trách', contract.createdByManagerName!, Icons.person_outline_rounded),
                            ],
                            if (contract.staffAcceptedAt != null) ...[
                              const Divider(height: 20, color: AppColors.borderLight),
                              _buildInfoRow('Thời gian bạn ký', _formatDateTime(contract.staffAcceptedAt), Icons.done_all_rounded),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // 3. Điều khoản hợp đồng
                      if (contract.contractTerms != null && contract.contractTerms!.isNotEmpty) ...[
                        _buildSectionTitle('Điều khoản hợp đồng'),
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: AppColors.borderLight),
                          ),
                          child: Text(
                            contract.contractTerms!,
                            style: const TextStyle(
                              fontSize: 13,
                              color: AppColors.textSecondary,
                              height: 1.5,
                            ),
                          ),
                        ),
                        const SizedBox(height: 16),
                      ],

                      // 4. File PDF đính kèm (View and Download PDF)
                      _buildSectionTitle('Tệp hợp đồng đính kèm'),
                      Container(
                        padding: const EdgeInsets.all(14),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.borderLight),
                        ),
                        child: Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: const Color(0xFFFEE2E2),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: const Icon(Icons.picture_as_pdf_rounded, color: AppColors.error, size: 24),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${contract.contractNumber}.pdf',
                                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    contract.hasPdf ? 'Tệp PDF chính thức có sẵn' : 'Chưa có file PDF',
                                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                                  ),
                                ],
                              ),
                            ),
                            if (contract.hasPdf) ...[
                              TextButton.icon(
                                onPressed: () => _showPdfPreviewDialog(contract),
                                icon: const Icon(Icons.visibility_outlined, size: 16),
                                label: const Text('Xem'),
                                style: TextButton.styleFrom(foregroundColor: AppColors.primary),
                              ),
                              IconButton(
                                tooltip: 'Tải về',
                                icon: const Icon(Icons.download_rounded, color: AppColors.primary),
                                onPressed: () => _handleDownloadPdf(contract),
                              ),
                            ],
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),

                      // 5. Lịch sử yêu cầu thay đổi (Contract Change Requests / Resend Updates)
                      if (changeRequests.isNotEmpty) ...[
                        _buildSectionTitle('Lịch sử yêu cầu điều chỉnh hợp đồng'),
                        ...changeRequests.map((cr) => _buildChangeRequestCard(cr)),
                        const SizedBox(height: 16),
                      ],

                      // Thông báo lý do không thể ký nếu có
                      if (contract.isPendingStaff && !contract.canAccept && contract.cannotAcceptReason != null) ...[
                        Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEF2F2),
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: const Color(0xFFFECACA)),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.warning_amber_rounded, color: AppColors.error, size: 20),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  contract.cannotAcceptReason!,
                                  style: const TextStyle(fontSize: 13, color: AppColors.error),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 16),
                      ],

                      // 6. Action buttons: Ký hợp đồng & Gửi yêu cầu sửa
                      if (contract.isPendingStaff) ...[
                        if (contract.canAccept) ...[
                          AppButton(
                            text: 'Xác nhận & Ký hợp đồng',
                            icon: Icons.draw_rounded,
                            isLoading: isAccepting,
                            onPressed: isAccepting ? null : () => _handleAcceptContract(contract),
                            width: double.infinity,
                            height: 52,
                          ),
                          const SizedBox(height: 12),
                        ],
                        if (contract.canSubmitChangeRequest) ...[
                          SizedBox(
                            width: double.infinity,
                            child: OutlinedButton.icon(
                              onPressed: () async {
                                final res = await Navigator.push(
                                  context,
                                  MaterialPageRoute(
                                    builder: (_) => SubmitContractChangeRequestView(
                                      contract: contract,
                                      controller: widget.controller,
                                    ),
                                  ),
                                );
                                if (res == true) {
                                  widget.controller.loadContractDetail(widget.contractId);
                                }
                              },
                              icon: const Icon(Icons.edit_note_rounded, color: AppColors.primary),
                              label: const Text(
                                'Yêu cầu điều chỉnh thông tin sai sót',
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.primary,
                                ),
                              ),
                              style: OutlinedButton.styleFrom(
                                side: const BorderSide(color: AppColors.primary, width: 1.5),
                                padding: const EdgeInsets.symmetric(vertical: 14),
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                              ),
                            ),
                          ),
                        ],
                      ] else if (contract.isActive && contract.canSubmitChangeRequest) ...[
                        SizedBox(
                          width: double.infinity,
                          child: OutlinedButton.icon(
                            onPressed: () async {
                              final res = await Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => SubmitContractChangeRequestView(
                                    contract: contract,
                                    controller: widget.controller,
                                  ),
                                ),
                              );
                              if (res == true) {
                                widget.controller.loadContractDetail(widget.contractId);
                              }
                            },
                            icon: const Icon(Icons.edit_note_rounded, color: AppColors.primary),
                            label: const Text(
                              'Gửi yêu cầu điều chỉnh hợp đồng',
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.w600,
                                color: AppColors.primary,
                              ),
                            ),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: AppColors.primary, width: 1.5),
                              padding: const EdgeInsets.symmetric(vertical: 14),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                            ),
                          ),
                        ),
                      ],
                      const SizedBox(height: 24),
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

  Widget _buildInfoRow(String label, String value, IconData icon, {bool isHighlight = false}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: isHighlight ? AppColors.primary : AppColors.textMuted),
        const SizedBox(width: 10),
        SizedBox(
          width: 130,
          child: Text(
            label,
            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.w600,
              color: isHighlight ? AppColors.primaryDark : AppColors.textPrimary,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildChangeRequestCard(StaffContractChangeRequestModel cr) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Yêu cầu #${cr.changeRequestId}',
                style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
              ),
              StatusBadge.forChangeRequestStatus(cr.status),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            'Gửi ngày: ${_formatDateTime(cr.submittedAt ?? cr.createdAt)}',
            style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
          ),
          if (cr.staffComment != null && cr.staffComment!.isNotEmpty) ...[
            const SizedBox(height: 6),
            Text(
              'Ghi chú của bạn: ${cr.staffComment}',
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary, fontStyle: FontStyle.italic),
            ),
          ],
          const Divider(height: 16, color: AppColors.borderLight),
          const Text('Các nội dung đề xuất:', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textPrimary)),
          const SizedBox(height: 6),
          ...cr.items.map((item) => Padding(
                padding: const EdgeInsets.only(bottom: 4),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('• ', style: TextStyle(fontWeight: FontWeight.bold, color: AppColors.primary)),
                    Expanded(
                      child: Text(
                        '${item.fieldDisplayName}: ${item.proposedValue}${item.reason != null ? " (${item.reason})" : ""}',
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                      ),
                    ),
                  ],
                ),
              )),
          if (cr.reviewComment != null && cr.reviewComment!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.chat_bubble_outline_rounded, size: 14, color: AppColors.textMuted),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      'Phản hồi từ Quản lý: ${cr.reviewComment}',
                      style: const TextStyle(fontSize: 12, color: AppColors.textSecondary),
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
