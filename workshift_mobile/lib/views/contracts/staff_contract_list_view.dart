import 'dart:async';
import 'package:flutter/material.dart';
import '../../controllers/staff_contract_controller.dart';
import '../../models/staff_contract_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/status_badge.dart';
import 'staff_contract_detail_view.dart';

/// Màn hình Danh sách Hợp đồng lao động của Staff
/// (View & Search View Employment Contract List for Staff)
class StaffContractListView extends StatefulWidget {
  final bool showBackButton;

  const StaffContractListView({
    super.key,
    this.showBackButton = true,
  });

  @override
  State<StaffContractListView> createState() => _StaffContractListViewState();
}

class _StaffContractListViewState extends State<StaffContractListView> {
  final StaffContractController _controller = StaffContractController();
  final TextEditingController _searchController = TextEditingController();
  Timer? _searchDebounce;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onControllerUpdate);
    _controller.loadContracts();
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    _controller.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  void _onSearchChanged(String query) {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 400), () {
      _controller.setSearchQuery(query);
    });
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '--';
    final d = dt.day.toString().padLeft(2, '0');
    final m = dt.month.toString().padLeft(2, '0');
    final y = dt.year.toString();
    return '$d/$m/$y';
  }

  void _showDateFilterDialog() async {
    final pickedRange = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2020),
      lastDate: DateTime(2100),
      initialDateRange: _controller.startDateFrom != null && _controller.startDateTo != null
          ? DateTimeRange(start: _controller.startDateFrom!, end: _controller.startDateTo!)
          : null,
      helpText: 'CHỌN KHOẢNG NGÀY BẮT ĐẦU HỢP ĐỒNG',
    );

    if (pickedRange != null) {
      _controller.setDateRange(pickedRange.start, pickedRange.end);
    }
  }

  @override
  Widget build(BuildContext context) {
    final contracts = _controller.contracts;
    final isLoading = _controller.isLoading;
    final hasActiveFilters = _controller.hasActiveFilters;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Hợp đồng lao động',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: widget.showBackButton
            ? IconButton(
                icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
                onPressed: () => Navigator.pop(context),
              )
            : null,
      ),
      body: Column(
        children: [
          // 1. Search Bar & Date Filter button
          Container(
            color: Colors.white,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            child: Row(
              children: [
                Expanded(
                  child: Container(
                    height: 42,
                    decoration: BoxDecoration(
                      color: const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: TextField(
                      controller: _searchController,
                      onChanged: _onSearchChanged,
                      decoration: InputDecoration(
                        hintText: 'Tìm theo số HĐ, chức danh, chi nhánh...',
                        hintStyle: const TextStyle(fontSize: 13, color: AppColors.textMuted),
                        prefixIcon: const Icon(Icons.search_rounded, size: 20, color: AppColors.textMuted),
                        suffixIcon: _searchController.text.isNotEmpty
                            ? IconButton(
                                icon: const Icon(Icons.clear_rounded, size: 18, color: AppColors.textMuted),
                                onPressed: () {
                                  _searchController.clear();
                                  _controller.setSearchQuery('');
                                },
                              )
                            : null,
                        border: InputBorder.none,
                        contentPadding: const EdgeInsets.symmetric(vertical: 10),
                      ),
                      style: const TextStyle(fontSize: 14),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                // Nút chọn ngày
                Stack(
                  children: [
                    IconButton(
                      tooltip: 'Lọc ngày bắt đầu',
                      icon: Icon(
                        Icons.calendar_month_outlined,
                        color: _controller.startDateFrom != null ? AppColors.primary : AppColors.textPrimary,
                        size: 22,
                      ),
                      style: IconButton.styleFrom(
                        backgroundColor: _controller.startDateFrom != null
                            ? AppColors.primaryLight
                            : const Color(0xFFF1F5F9),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      onPressed: _showDateFilterDialog,
                    ),
                    if (hasActiveFilters)
                      Positioned(
                        top: 6,
                        right: 6,
                        child: Container(
                          width: 8,
                          height: 8,
                          decoration: const BoxDecoration(
                            color: AppColors.primary,
                            shape: BoxShape.circle,
                          ),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),

          // 2. Filter chips cuộn ngang theo Trạng thái
          Container(
            color: Colors.white,
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 10),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _buildQuickStatusChip('Tất cả', 'ALL'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Chờ ký', 'PENDING_STAFF'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Có hiệu lực', 'ACTIVE'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Sắp bắt đầu', 'SCHEDULED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Đã hết hạn', 'EXPIRED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Đã hủy / Chấm dứt', 'CANCELLED'),
                ],
              ),
            ),
          ),

          // 3. Danh sách các hợp đồng
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => _controller.loadContracts(refresh: true),
              color: AppColors.primary,
              child: isLoading
                  ? const Center(
                      child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
                    )
                  : contracts.isEmpty
                      ? _buildEmptyState()
                      : ListView.builder(
                          physics: const AlwaysScrollableScrollPhysics(),
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          itemCount: contracts.length,
                          itemBuilder: (context, index) {
                            final item = contracts[index];
                            return _buildContractCard(item);
                          },
                        ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildQuickStatusChip(String label, String statusCode) {
    final isSelected = _controller.selectedStatus == statusCode;
    return GestureDetector(
      onTap: () => _controller.setStatusFilter(statusCode),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary : const Color(0xFFF1F5F9),
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 13,
            fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
            color: isSelected ? Colors.white : AppColors.textSecondary,
          ),
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: const BoxDecoration(
                color: Color(0xFFEFF6FF),
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.verified_user_outlined, size: 48, color: AppColors.primary),
            ),
            const SizedBox(height: 16),
            const Text(
              'Không tìm thấy hợp đồng nào',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
            ),
            const SizedBox(height: 6),
            Text(
              _controller.hasActiveFilters
                  ? 'Không có hợp đồng nào phù hợp với bộ lọc hiện tại.'
                  : 'Bạn hiện chưa có hợp đồng lao động nào được gửi từ Quản lý.',
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 20),
            if (_controller.hasActiveFilters)
              OutlinedButton(
                onPressed: () {
                  _searchController.clear();
                  _controller.resetFilters();
                },
                style: OutlinedButton.styleFrom(
                  side: const BorderSide(color: AppColors.primary),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
                child: const Text('Đặt lại bộ lọc', style: TextStyle(color: AppColors.primary)),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildContractCard(StaffContractListItemModel item) {
    return GestureDetector(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => StaffContractDetailView(
              contractId: item.contractId,
              controller: _controller,
            ),
          ),
        );
      },
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: item.isPendingStaff ? AppColors.warning.withValues(alpha: 0.6) : AppColors.borderLight,
            width: item.isPendingStaff ? 1.5 : 1,
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.02),
              blurRadius: 6,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Top row: Mã hợp đồng & StatusBadge
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: item.isPendingStaff
                        ? AppColors.warningBg
                        : item.isActive
                            ? AppColors.successBg
                            : const Color(0xFFEFF6FF),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(
                    Icons.verified_user_rounded,
                    color: item.isPendingStaff
                        ? AppColors.warning
                        : item.isActive
                            ? const Color(0xFF059669)
                            : AppColors.primary,
                    size: 20,
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.contractNumber,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        item.employeePosition,
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary, fontWeight: FontWeight.w500),
                      ),
                    ],
                  ),
                ),
                StatusBadge.forContractStatus(item.status),
              ],
            ),
            const Divider(height: 20, color: AppColors.borderLight),

            // Chi tiết: Mức lương & Thời hạn
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Mức lương / giờ', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
                      const SizedBox(height: 2),
                      Text(
                        item.formattedHourlyRate,
                        style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.primaryDark),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Thời hạn hợp đồng', style: TextStyle(fontSize: 12, color: AppColors.textMuted)),
                      const SizedBox(height: 2),
                      Text(
                        '${_formatDate(item.startDate)} - ${item.endDate != null ? _formatDate(item.endDate) : "KTH"}',
                        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.textPrimary),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Footer: Chi nhánh & Nút Hành động
            Row(
              children: [
                const Icon(Icons.storefront_outlined, size: 15, color: AppColors.textMuted),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    item.branchName,
                    style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                if (item.isPendingStaff) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: AppColors.warning,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.draw_rounded, size: 14, color: Colors.white),
                        SizedBox(width: 4),
                        Text(
                          'Ký ngay',
                          style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Colors.white),
                        ),
                      ],
                    ),
                  ),
                ] else ...[
                  const Text(
                    'Xem chi tiết',
                    style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.primary),
                  ),
                  const Icon(Icons.chevron_right_rounded, size: 16, color: AppColors.primary),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}
