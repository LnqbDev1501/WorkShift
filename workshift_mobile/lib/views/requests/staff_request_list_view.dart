import 'dart:async';
import 'package:flutter/material.dart';
import '../../controllers/staff_request_controller.dart';
import '../../models/staff_request_model.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';
import 'staff_request_detail_view.dart';
import 'submit_request_view.dart';

/// Màn hình Danh sách Yêu cầu của Staff (View, Search, Filter, Cancel Request)
class StaffRequestListView extends StatefulWidget {
  final bool showBackButton;

  const StaffRequestListView({
    super.key,
    this.showBackButton = true,
  });

  @override
  State<StaffRequestListView> createState() => _StaffRequestListViewState();
}

class _StaffRequestListViewState extends State<StaffRequestListView> {
  final StaffRequestController _controller = StaffRequestController();
  final TextEditingController _searchController = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  Timer? _searchDebounce;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onControllerUpdate);
    _scrollController.addListener(_onScroll);
    _controller.loadRequests();
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    _controller.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (mounted) setState(() {});
  }

  void _onScroll() {
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 200) {
      if (_controller.hasMore && !_controller.isLoadingMore) {
        _controller.loadMore();
      }
    }
  }

  void _onSearchChanged(String query) {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(const Duration(milliseconds: 500), () {
      _controller.setSearchQuery(query);
    });
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

  String _formatTime(DateTime? dt) {
    if (dt == null) return '--:--';
    final local = dt.toLocal();
    final h = local.hour.toString().padLeft(2, '0');
    final min = local.minute.toString().padLeft(2, '0');
    return '$h:$min';
  }

  String _formatDate(DateTime? dt) {
    if (dt == null) return '--';
    final local = dt.toLocal();
    final d = local.day.toString().padLeft(2, '0');
    final m = local.month.toString().padLeft(2, '0');
    final y = local.year.toString();
    return '$d/$m/$y';
  }

  Future<void> _handleCancelRequest(StaffEmployeeRequestListItemModel item) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.cancel_outlined, color: AppColors.error, size: 24),
            SizedBox(width: 8),
            Text('Hủy yêu cầu', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        content: Text(
          'Bạn có chắc chắn muốn hủy yêu cầu #${item.requestId} (${item.typeDisplayName}) không?',
          style: const TextStyle(fontSize: 14, color: AppColors.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Đóng', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: const Text('Xác nhận hủy'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final success = await _controller.cancelRequest(item.requestId);
      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Đã hủy yêu cầu thành công'),
              backgroundColor: AppColors.success,
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(_controller.actionErrorMessage ?? 'Hủy yêu cầu thất bại'),
              backgroundColor: AppColors.error,
            ),
          );
        }
      }
    }
  }

  void _showFilterBottomSheet() {
    String tempType = _controller.selectedRequestType;
    String tempStatus = _controller.selectedStatus;
    DateTime? tempFrom = _controller.fromDate;
    DateTime? tempTo = _controller.toDate;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return StatefulBuilder(
          builder: (modalCtx, setModalState) {
            return Padding(
              padding: EdgeInsets.only(
                top: 20,
                left: 20,
                right: 20,
                bottom: MediaQuery.of(modalCtx).viewInsets.bottom + 20,
              ),
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Header modal
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Bộ lọc nâng cao',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        TextButton(
                          onPressed: () {
                            setModalState(() {
                              tempType = 'ALL';
                              tempStatus = 'ALL';
                              tempFrom = null;
                              tempTo = null;
                            });
                          },
                          child: const Text('Đặt lại', style: TextStyle(color: AppColors.primary)),
                        ),
                      ],
                    ),
                    const Divider(height: 24, color: AppColors.borderLight),

                    // Lọc theo loại yêu cầu
                    const Text(
                      'Loại yêu cầu',
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 8,
                      children: [
                        _buildFilterChoiceChip('Tất cả', tempType == 'ALL', () {
                          setModalState(() => tempType = 'ALL');
                        }),
                        _buildFilterChoiceChip('Nghỉ phép', tempType == 'LEAVE', () {
                          setModalState(() => tempType = 'LEAVE');
                        }),
                        _buildFilterChoiceChip('Đi trễ', tempType == 'LATE_ARRIVAL', () {
                          setModalState(() => tempType = 'LATE_ARRIVAL');
                        }),
                        _buildFilterChoiceChip('Về sớm', tempType == 'EARLY_LEAVE', () {
                          setModalState(() => tempType = 'EARLY_LEAVE');
                        }),
                      ],
                    ),
                    const SizedBox(height: 16),

                    // Lọc theo trạng thái
                    const Text(
                      'Trạng thái đơn',
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 8,
                      children: [
                        _buildFilterChoiceChip('Tất cả', tempStatus == 'ALL', () {
                          setModalState(() => tempStatus = 'ALL');
                        }),
                        _buildFilterChoiceChip('Chờ duyệt', tempStatus == 'PENDING', () {
                          setModalState(() => tempStatus = 'PENDING');
                        }),
                        _buildFilterChoiceChip('Đã duyệt', tempStatus == 'APPROVED', () {
                          setModalState(() => tempStatus = 'APPROVED');
                        }),
                        _buildFilterChoiceChip('Từ chối', tempStatus == 'REJECTED', () {
                          setModalState(() => tempStatus = 'REJECTED');
                        }),
                        _buildFilterChoiceChip('Đã hủy', tempStatus == 'CANCELLED', () {
                          setModalState(() => tempStatus = 'CANCELLED');
                        }),
                      ],
                    ),
                    const SizedBox(height: 16),

                    // Lọc theo khoảng ngày (From Date - To Date)
                    const Text(
                      'Khoảng ngày tạo',
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            onPressed: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: tempFrom ?? DateTime.now(),
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2100),
                              );
                              if (picked != null) {
                                setModalState(() => tempFrom = picked);
                              }
                            },
                            icon: const Icon(Icons.calendar_today_rounded, size: 16, color: AppColors.textSecondary),
                            label: Text(
                              tempFrom != null ? _formatDate(tempFrom) : 'Từ ngày',
                              style: TextStyle(
                                fontSize: 13,
                                color: tempFrom != null ? AppColors.textPrimary : AppColors.textMuted,
                              ),
                            ),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: AppColors.borderLight),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                              padding: const EdgeInsets.symmetric(vertical: 12),
                            ),
                          ),
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: OutlinedButton.icon(
                            onPressed: () async {
                              final picked = await showDatePicker(
                                context: context,
                                initialDate: tempTo ?? DateTime.now(),
                                firstDate: DateTime(2020),
                                lastDate: DateTime(2100),
                              );
                              if (picked != null) {
                                setModalState(() => tempTo = picked);
                              }
                            },
                            icon: const Icon(Icons.calendar_today_rounded, size: 16, color: AppColors.textSecondary),
                            label: Text(
                              tempTo != null ? _formatDate(tempTo) : 'Đến ngày',
                              style: TextStyle(
                                fontSize: 13,
                                color: tempTo != null ? AppColors.textPrimary : AppColors.textMuted,
                              ),
                            ),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: AppColors.borderLight),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                              padding: const EdgeInsets.symmetric(vertical: 12),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 24),

                    // Nút Áp dụng
                    AppButton(
                      text: 'Áp dụng bộ lọc',
                      onPressed: () {
                        Navigator.pop(ctx);
                        _controller.applyAdvancedFilters(
                          requestType: tempType,
                          status: tempStatus,
                          fromDate: tempFrom,
                          toDate: tempTo,
                        );
                      },
                      width: double.infinity,
                      height: 48,
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildFilterChoiceChip(String label, bool isSelected, VoidCallback onTap) {
    return ChoiceChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (_) => onTap(),
      selectedColor: AppColors.primaryLight,
      backgroundColor: Colors.white,
      side: BorderSide(color: isSelected ? AppColors.primary : AppColors.borderLight),
      labelStyle: TextStyle(
        fontSize: 13,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        color: isSelected ? AppColors.primary : AppColors.textSecondary,
      ),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
    );
  }

  @override
  Widget build(BuildContext context) {
    final requests = _controller.requests;
    final isLoading = _controller.isLoading;
    final hasActiveFilter = _controller.hasActiveFilters;

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Yêu cầu của tôi',
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
        actions: [
          IconButton(
            tooltip: 'Tạo yêu cầu',
            icon: const Icon(Icons.add_circle_outline_rounded, color: AppColors.primary, size: 24),
            onPressed: () async {
              final result = await Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (_) => SubmitRequestView(controller: _controller),
                ),
              );
              if (result == true) {
                _controller.loadRequests(refresh: true);
              }
            },
          ),
        ],
      ),
      body: Column(
        children: [
          // 1. Search Bar & Nút mở Bộ lọc
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
                        hintText: 'Tìm kiếm theo lý do...',
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
                // Nút mở modal bộ lọc với badge
                Stack(
                  children: [
                    IconButton(
                      icon: const Icon(Icons.tune_rounded, color: AppColors.textPrimary, size: 22),
                      style: IconButton.styleFrom(
                        backgroundColor: const Color(0xFFF1F5F9),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                      ),
                      onPressed: _showFilterBottomSheet,
                    ),
                    if (hasActiveFilter)
                      Positioned(
                        top: 6,
                        right: 6,
                        child: Container(
                          width: 9,
                          height: 9,
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

          // 2. Filter chips cuộn ngang theo Trạng thái (Quick Filter)
          Container(
            color: Colors.white,
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 10),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  _buildQuickStatusChip('Tất cả', 'ALL'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Chờ duyệt', 'PENDING'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Đã duyệt', 'APPROVED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Từ chối', 'REJECTED'),
                  const SizedBox(width: 8),
                  _buildQuickStatusChip('Đã hủy', 'CANCELLED'),
                ],
              ),
            ),
          ),

          // 3. Danh sách các yêu cầu
          Expanded(
            child: RefreshIndicator(
              onRefresh: () => _controller.loadRequests(refresh: true),
              color: AppColors.primary,
              child: isLoading
                  ? const Center(
                      child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
                    )
                  : requests.isEmpty
                      ? _buildEmptyState()
                      : ListView.builder(
                          controller: _scrollController,
                          physics: const AlwaysScrollableScrollPhysics(),
                          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                          itemCount: requests.length + (_controller.isLoadingMore ? 1 : 0),
                          itemBuilder: (context, index) {
                            if (index == requests.length) {
                              return const Center(
                                child: Padding(
                                  padding: EdgeInsets.symmetric(vertical: 16),
                                  child: CircularProgressIndicator(
                                    color: AppColors.primary,
                                    strokeWidth: 2.5,
                                  ),
                                ),
                              );
                            }
                            final item = requests[index];
                            return _buildRequestCard(item);
                          },
                        ),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 3,
        icon: const Icon(Icons.add_rounded),
        label: const Text('Tạo yêu cầu', style: TextStyle(fontWeight: FontWeight.bold)),
        onPressed: () async {
          final result = await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => SubmitRequestView(controller: _controller),
            ),
          );
          if (result == true) {
            _controller.loadRequests(refresh: true);
          }
        },
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
              child: const Icon(Icons.assignment_late_outlined, size: 48, color: AppColors.primary),
            ),
            const SizedBox(height: 16),
            const Text(
              'Không tìm thấy yêu cầu nào',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
            ),
            const SizedBox(height: 6),
            Text(
              _controller.hasActiveFilters
                  ? 'Không có yêu cầu phù hợp với bộ lọc hiện tại.'
                  : 'Bạn chưa tạo yêu cầu nào. Nhấn nút bên dưới để tạo mới.',
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

  Widget _buildRequestCard(StaffEmployeeRequestListItemModel item) {
    return GestureDetector(
      onTap: () {
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => StaffRequestDetailView(
              requestId: item.requestId,
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
          border: Border.all(color: AppColors.borderLight),
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
            // Top row: Loại yêu cầu & StatusBadge
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: item.typeColor.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(item.typeIcon, color: item.typeColor, size: 20),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.typeDisplayName,
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '#${item.requestId} • Gửi ngày ${_formatDateTime(item.createdAt)}',
                        style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                      ),
                    ],
                  ),
                ),
                StatusBadge.forRequestStatus(item.status),
              ],
            ),
            const Divider(height: 20, color: AppColors.borderLight),

            // Tóm tắt ca làm việc (Shift summary)
            if (item.shiftSummary != null) ...[
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppColors.borderLight),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.event_note_rounded, size: 16, color: AppColors.textMuted),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _buildShiftSummaryText(item),
                        style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 10),
            ],

            // Lý do gửi
            if (item.reason != null && item.reason!.isNotEmpty) ...[
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Lý do: ',
                    style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: AppColors.textSecondary),
                  ),
                  Expanded(
                    child: Text(
                      item.reason!,
                      style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
            ],

            // Bottom row: Chi nhánh & Nút Hủy (nếu pending)
            Row(
              children: [
                const Icon(Icons.storefront_outlined, size: 15, color: AppColors.textMuted),
                const SizedBox(width: 4),
                Text(
                  item.branchName,
                  style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                ),
                const Spacer(),
                if (item.isPending) ...[
                  TextButton.icon(
                    onPressed: () => _handleCancelRequest(item),
                    icon: const Icon(Icons.close_rounded, size: 15, color: AppColors.error),
                    label: const Text(
                      'Hủy đơn',
                      style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.error),
                    ),
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      visualDensity: VisualDensity.compact,
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

  String _buildShiftSummaryText(StaffEmployeeRequestListItemModel item) {
    final s = item.shiftSummary!;
    if (item.requestType == 'LEAVE') {
      if (s.totalShifts > 1) {
        return '${s.totalShifts} ca làm việc (${s.primaryShiftName ?? 'Ca'}...)';
      }
      return '${s.primaryShiftName ?? 'Ca'} • ${_formatDate(s.primaryScheduledStartAt)} (${_formatTime(s.primaryScheduledStartAt)} - ${_formatTime(s.primaryScheduledEndAt)})';
    } else {
      // LATE_ARRIVAL hoặc EARLY_LEAVE
      final action = item.requestType == 'LATE_ARRIVAL' ? 'Đến lúc' : 'Về lúc';
      return '${s.primaryShiftName ?? 'Ca'} • $action ${_formatTime(s.requestedTime)} (${_formatDate(s.primaryScheduledStartAt)})';
    }
  }
}
