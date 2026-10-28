import 'dart:async';
import 'package:flutter/material.dart';
import '../../controllers/staff_contract_controller.dart';
import '../../models/staff_contract_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/status_badge.dart';
import 'staff_contract_detail_view.dart';

/// Screen: My Employment Contracts (Staff Portal)
/// Features: List, Search, Quick Status Filter, Date Filter, Shift Registration Notice, Details Navigation
class StaffContractListView extends StatefulWidget {
  final bool showBackButton;

  const StaffContractListView({super.key, this.showBackButton = true});

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
      initialDateRange:
          _controller.startDateFrom != null && _controller.startDateTo != null
          ? DateTimeRange(
              start: _controller.startDateFrom!,
              end: _controller.startDateTo!,
            )
          : null,
      helpText: 'SELECT CONTRACT START DATE RANGE',
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
          'My Employment Contracts',
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
                icon: const Icon(
                  Icons.arrow_back_ios_new_rounded,
                  color: AppColors.textPrimary,
                  size: 20,
                ),
                onPressed: () => Navigator.pop(context),
              )
            : null,
        actions: [
          IconButton(
            tooltip: 'Refresh',
            icon: const Icon(
              Icons.refresh_rounded,
              color: AppColors.textPrimary,
            ),
            onPressed: () => _controller.loadContracts(refresh: true),
          ),
        ],
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
                        hintText: 'Search contract no, position, branch...',
                        hintStyle: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textMuted,
                        ),
                        prefixIcon: const Icon(
                          Icons.search_rounded,
                          size: 20,
                          color: AppColors.textMuted,
                        ),
                        suffixIcon: _searchController.text.isNotEmpty
                            ? IconButton(
                                icon: const Icon(
                                  Icons.clear_rounded,
                                  size: 18,
                                  color: AppColors.textMuted,
                                ),
                                onPressed: () {
                                  _searchController.clear();
                                  _controller.setSearchQuery('');
                                },
                              )
                            : null,
                        border: InputBorder.none,
                        contentPadding: const EdgeInsets.symmetric(
                          vertical: 10,
                        ),
                      ),
                      style: const TextStyle(fontSize: 14),
                    ),
                  ),
                ),
                const SizedBox(width: 10),
                // Date range button
                Stack(
                  children: [
                    IconButton(
                      tooltip: 'Filter by start date',
                      icon: Icon(
                        Icons.calendar_month_outlined,
                        color: _controller.startDateFrom != null
                            ? AppColors.primary
                            : AppColors.textPrimary,
                        size: 22,
                      ),
                      style: IconButton.styleFrom(
                        backgroundColor: _controller.startDateFrom != null
                            ? AppColors.primaryLight
                            : const Color(0xFFF1F5F9),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
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

          // 2. Filter chips for Contract Statuses
          Container(
            color: Colors.white,
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 10),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _buildQuickStatusChip('All', 'ALL'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Pending Acceptance', 'PENDING_STAFF'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Active', 'ACTIVE'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Scheduled', 'SCHEDULED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Expired', 'EXPIRED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Cancelled / Terminated', 'CANCELLED'),
                ],
              ),
            ),
          ),

          // 3. Contracts List Body
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => _controller.loadContracts(refresh: true),
              color: AppColors.primary,
              child: isLoading
                  ? const Center(
                      child: CircularProgressIndicator(
                        color: AppColors.primary,
                        strokeWidth: 2.5,
                      ),
                    )
                  : ListView(
                      physics: const AlwaysScrollableScrollPhysics(),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 12,
                      ),
                      children: [
                        // Shift Registration Policy Card (matching web Index.cshtml)
                        _buildShiftPolicyNotice(),
                        const SizedBox(height: 12),

                        if (contracts.isEmpty)
                          _buildEmptyState()
                        else
                          ...contracts.map((item) => _buildContractCard(item)),
                      ],
                    ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildShiftPolicyNotice() {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFFEFF6FF), Color(0xFFEEF2FF)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: const Color(0xFFBFDBFE)),
      ),
      child: const Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.info_rounded, color: Color(0xFF2563EB), size: 20),
          SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Shift Registration Policy',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF1E3A8A),
                  ),
                ),
                SizedBox(height: 3),
                Text(
                  'Staff can only register for and receive shifts when their contract is accepted and active (or scheduled with a valid start date). Contracts in pending acceptance, cancelled, or terminated status are not eligible for shift assignments.',
                  style: TextStyle(
                    fontSize: 12,
                    color: Color(0xFF1E40AF),
                    height: 1.35,
                  ),
                ),
              ],
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
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 20),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(20),
            decoration: const BoxDecoration(
              color: Color(0xFFEFF6FF),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.verified_user_outlined,
              size: 48,
              color: AppColors.primary,
            ),
          ),
          const SizedBox(height: 16),
          const Text(
            'No employment contracts found',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            _controller.hasActiveFilters
                ? 'No contracts match your current filter criteria.'
                : 'You currently have no employment contracts issued by your manager.',
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 20),
          if (_controller.hasActiveFilters)
            OutlinedButton.icon(
              onPressed: () {
                _searchController.clear();
                _controller.resetFilters();
              },
              icon: const Icon(Icons.restart_alt_rounded, size: 18),
              label: const Text('Reset Filters'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: const BorderSide(color: AppColors.primary),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
              ),
            ),
        ],
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
            color: item.isPendingStaff
                ? AppColors.warning.withValues(alpha: 0.6)
                : AppColors.borderLight,
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
            // Top row: Contract number & StatusBadge
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
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ],
                  ),
                ),
                StatusBadge.forContractStatus(item.status),
              ],
            ),
            const Divider(height: 20, color: AppColors.borderLight),

            // Metrics: Hourly Rate & Contract Period
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Hourly Rate',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textMuted,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        item.formattedHourlyRate,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: Color(0xFF047857),
                        ),
                      ),
                    ],
                  ),
                ),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Contract Period',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.textMuted,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${_formatDate(item.startDate)} - ${item.endDate != null ? _formatDate(item.endDate) : "Indefinite"}',
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Footer: Branch & Actions
            Row(
              children: [
                const Icon(
                  Icons.storefront_outlined,
                  size: 15,
                  color: AppColors.textMuted,
                ),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    item.branchName,
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textMuted,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                if (item.isPendingStaff) ...[
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 6,
                    ),
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
                          'Sign Contract',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
                  ),
                ] else ...[
                  const Text(
                    'View Details',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.primary,
                    ),
                  ),
                  const Icon(
                    Icons.chevron_right_rounded,
                    size: 16,
                    color: AppColors.primary,
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }
}
