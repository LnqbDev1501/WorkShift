import 'package:flutter/material.dart';
import '../../../models/staff_timesheet_model.dart';
import '../../../theme/app_colors.dart';

class WeekSelectionBottomSheet extends StatefulWidget {
  final List<StaffTimesheetWeekOptionModel> weeks;
  final int? selectedWeekId;

  const WeekSelectionBottomSheet({
    super.key,
    required this.weeks,
    required this.selectedWeekId,
  });

  static Future<int?> show({
    required BuildContext context,
    required List<StaffTimesheetWeekOptionModel> weeks,
    required int? selectedWeekId,
  }) {
    if (weeks.isEmpty) return Future.value(null);

    return showModalBottomSheet<int>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      elevation: 0,
      builder: (_) => WeekSelectionBottomSheet(
        weeks: weeks,
        selectedWeekId: selectedWeekId,
      ),
    );
  }

  @override
  State<WeekSelectionBottomSheet> createState() => _WeekSelectionBottomSheetState();
}

class _WeekSelectionBottomSheetState extends State<WeekSelectionBottomSheet> {
  final TextEditingController _searchController = TextEditingController();
  final FocusNode _searchFocusNode = FocusNode();
  String _searchQuery = '';
  String _selectedStatusFilter = 'ALL'; // ALL, LOCKED, GENERATED, NOT GENERATED

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text.trim().toLowerCase();
      });
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    _searchFocusNode.dispose();
    super.dispose();
  }

  bool _isCurrentWeek(StaffTimesheetWeekOptionModel item) {
    try {
      final now = DateTime.now();
      final today = DateTime(now.year, now.month, now.day);
      final start = DateTime.parse(item.weekStartDate);
      final end = DateTime.parse(item.weekEndDate);
      return !today.isBefore(start) && !today.isAfter(end);
    } catch (_) {
      return false;
    }
  }

  List<StaffTimesheetWeekOptionModel> get _filteredWeeks {
    return widget.weeks.where((item) {
      // 1. Status Filter
      if (_selectedStatusFilter != 'ALL') {
        final itemStatus = item.status.toUpperCase();
        if (_selectedStatusFilter == 'LOCKED' && itemStatus != 'LOCKED') {
          return false;
        }
        if (_selectedStatusFilter == 'GENERATED' && itemStatus != 'GENERATED') {
          return false;
        }
        if (_selectedStatusFilter == 'NOT GENERATED' &&
            itemStatus != 'NOT GENERATED' &&
            itemStatus != 'NOT_GENERATED') {
          return false;
        }
      }

      // 2. Search Query Filter
      if (_searchQuery.isEmpty) return true;

      final periodText = 'week ${item.period}'.toLowerCase();
      final dateText = '${item.weekStartDate} ${item.weekEndDate}'.toLowerCase();
      final statusText = item.status.toLowerCase();

      return periodText.contains(_searchQuery) ||
          dateText.contains(_searchQuery) ||
          statusText.contains(_searchQuery);
    }).toList();
  }

  int _countByStatus(String filter) {
    if (filter == 'ALL') return widget.weeks.length;
    return widget.weeks.where((w) {
      final s = w.status.toUpperCase();
      if (filter == 'LOCKED') return s == 'LOCKED';
      if (filter == 'GENERATED') return s == 'GENERATED';
      if (filter == 'NOT GENERATED') return s == 'NOT GENERATED' || s == 'NOT_GENERATED';
      return false;
    }).length;
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;
    final filtered = _filteredWeeks;

    return Container(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.88,
      ),
      padding: EdgeInsets.only(bottom: bottomInset),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        boxShadow: [
          BoxShadow(
            color: Color(0x22000000),
            blurRadius: 20,
            offset: Offset(0, -4),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Drag Handle
            Center(
              child: Container(
                margin: const EdgeInsets.only(top: 10, bottom: 6),
                width: 44,
                height: 4,
                decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),

            // Header Row
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 16, 12),
              child: Row(
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: AppColors.primaryLight,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFFBFDBFE)),
                    ),
                    child: const Icon(
                      Icons.calendar_month_rounded,
                      color: AppColors.primary,
                      size: 22,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Text(
                              'Select Schedule Week',
                              style: TextStyle(
                                fontSize: 17,
                                fontWeight: FontWeight.bold,
                                color: AppColors.textPrimary,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
                              decoration: BoxDecoration(
                                color: const Color(0xFFF1F5F9),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: Text(
                                '${widget.weeks.length}',
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.textSecondary,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 2),
                        const Text(
                          'Find and switch to any schedule week',
                          style: TextStyle(
                            fontSize: 12,
                            color: AppColors.textMuted,
                          ),
                        ),
                      ],
                    ),
                  ),
                  InkWell(
                    onTap: () => Navigator.pop(context),
                    borderRadius: BorderRadius.circular(20),
                    child: Container(
                      padding: const EdgeInsets.all(7),
                      decoration: const BoxDecoration(
                        color: Color(0xFFF1F5F9),
                        shape: BoxShape.circle,
                      ),
                      child: const Icon(
                        Icons.close_rounded,
                        size: 18,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Search Bar
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 18),
              child: Container(
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(
                    color: _searchFocusNode.hasFocus ? AppColors.primary : AppColors.border,
                    width: _searchFocusNode.hasFocus ? 1.5 : 1.0,
                  ),
                ),
                child: TextField(
                  controller: _searchController,
                  focusNode: _searchFocusNode,
                  style: const TextStyle(fontSize: 14, color: AppColors.textPrimary),
                  decoration: InputDecoration(
                    hintText: 'Search week, date (e.g. 12/10, Oct, 2026)...',
                    hintStyle: const TextStyle(fontSize: 13, color: AppColors.textMuted),
                    prefixIcon: const Icon(
                      Icons.search_rounded,
                      color: AppColors.textSecondary,
                      size: 20,
                    ),
                    suffixIcon: _searchQuery.isNotEmpty
                        ? IconButton(
                            icon: const Icon(Icons.cancel_rounded, size: 18, color: AppColors.textMuted),
                            onPressed: () {
                              _searchController.clear();
                              setState(() => _searchQuery = '');
                            },
                          )
                        : null,
                    border: InputBorder.none,
                    isDense: true,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                  ),
                ),
              ),
            ),

            const SizedBox(height: 10),

            // Filter Chips
            SizedBox(
              height: 34,
              child: ListView(
                scrollDirection: Axis.horizontal,
                padding: const EdgeInsets.symmetric(horizontal: 18),
                children: [
                  _buildFilterChip('ALL', 'All'),
                  const SizedBox(width: 8),
                  _buildFilterChip('LOCKED', 'Locked'),
                  const SizedBox(width: 8),
                  _buildFilterChip('GENERATED', 'Generated'),
                  const SizedBox(width: 8),
                  _buildFilterChip('NOT GENERATED', 'Not Generated'),
                ],
              ),
            ),

            const SizedBox(height: 12),
            const Divider(height: 1, color: AppColors.borderLight),

            // List of Weeks
            Flexible(
              child: filtered.isEmpty
                  ? _buildEmptyState()
                  : ListView.builder(
                      shrinkWrap: true,
                      padding: const EdgeInsets.fromLTRB(18, 12, 18, 16),
                      itemCount: filtered.length,
                      physics: const BouncingScrollPhysics(),
                      itemBuilder: (context, index) {
                        final item = filtered[index];
                        final isSelected = item.scheduleWeekId == widget.selectedWeekId;
                        final isCurrent = _isCurrentWeek(item);

                        return _buildWeekItemCard(item, isSelected, isCurrent);
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFilterChip(String filterKey, String label) {
    final isSelected = _selectedStatusFilter == filterKey;
    final count = _countByStatus(filterKey);

    return InkWell(
      onTap: () {
        setState(() {
          _selectedStatusFilter = filterKey;
        });
      },
      borderRadius: BorderRadius.circular(20),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary : const Color(0xFFF1F5F9),
          borderRadius: BorderRadius.circular(20),
          boxShadow: isSelected
              ? const [
                  BoxShadow(
                    color: Color(0x402563EB),
                    blurRadius: 6,
                    offset: Offset(0, 2),
                  ),
                ]
              : null,
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: TextStyle(
                fontSize: 12,
                fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                color: isSelected ? Colors.white : AppColors.textSecondary,
              ),
            ),
            const SizedBox(width: 5),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
              decoration: BoxDecoration(
                color: isSelected ? const Color(0x40FFFFFF) : const Color(0xFFE2E8F0),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                '$count',
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                  color: isSelected ? Colors.white : AppColors.textSecondary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildWeekItemCard(
    StaffTimesheetWeekOptionModel item,
    bool isSelected,
    bool isCurrent,
  ) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: isSelected ? const Color(0xFFF0F7FF) : Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isSelected ? AppColors.primary : AppColors.border,
          width: isSelected ? 1.6 : 1.0,
        ),
        boxShadow: isSelected
            ? const [
                BoxShadow(
                  color: Color(0x142563EB),
                  blurRadius: 10,
                  offset: Offset(0, 3),
                ),
              ]
            : const [
                BoxShadow(
                  color: Color(0x06000000),
                  blurRadius: 4,
                  offset: Offset(0, 1),
                ),
              ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () => Navigator.pop(context, item.scheduleWeekId),
          borderRadius: BorderRadius.circular(16),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            child: Row(
              children: [
                // Icon Box
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: isSelected
                        ? AppColors.primary
                        : (isCurrent ? const Color(0xFFECFDF5) : const Color(0xFFF8FAFC)),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: isSelected
                          ? AppColors.primary
                          : (isCurrent ? const Color(0xFFA7F3D0) : AppColors.borderLight),
                    ),
                  ),
                  child: Icon(
                    isCurrent ? Icons.today_rounded : Icons.date_range_rounded,
                    color: isSelected
                        ? Colors.white
                        : (isCurrent ? const Color(0xFF059669) : AppColors.textSecondary),
                    size: 22,
                  ),
                ),
                const SizedBox(width: 12),

                // Week info
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Text(
                            'Week ${item.period}',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              color: isSelected ? AppColors.primary : AppColors.textPrimary,
                            ),
                          ),
                          if (isCurrent) ...[
                            const SizedBox(width: 6),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                              decoration: BoxDecoration(
                                color: const Color(0xFFECFDF5),
                                borderRadius: BorderRadius.circular(6),
                                border: Border.all(color: const Color(0xFFA7F3D0)),
                              ),
                              child: const Text(
                                'THIS WEEK',
                                style: TextStyle(
                                  fontSize: 9,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFF059669),
                                ),
                              ),
                            ),
                          ],
                        ],
                      ),
                      const SizedBox(height: 4),
                      Row(
                        children: [
                          const Icon(
                            Icons.calendar_today_outlined,
                            size: 12,
                            color: AppColors.textMuted,
                          ),
                          const SizedBox(width: 5),
                          Text(
                            '${item.weekStartDate} – ${item.weekEndDate}',
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const SizedBox(width: 8),

                // Status pill and checkmark
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    _buildStatusBadge(item.status),
                    const SizedBox(height: 6),
                    if (isSelected)
                      const Icon(
                        Icons.check_circle_rounded,
                        color: AppColors.primary,
                        size: 22,
                      )
                    else
                      Container(
                        width: 18,
                        height: 18,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          border: Border.all(color: AppColors.border, width: 1.5),
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildStatusBadge(String status) {
    final upper = status.toUpperCase();
    Color bg = const Color(0xFFF1F5F9);
    Color text = const Color(0xFF64748B);
    IconData icon = Icons.info_outline_rounded;
    String label = status;

    if (upper == 'LOCKED') {
      bg = const Color(0xFFE0F2FE);
      text = const Color(0xFF0284C7);
      icon = Icons.lock_outline_rounded;
      label = 'LOCKED';
    } else if (upper == 'GENERATED') {
      bg = const Color(0xFFFEF3C7);
      text = const Color(0xFFD97706);
      icon = Icons.assignment_turned_in_outlined;
      label = 'GENERATED';
    } else if (upper == 'NOT GENERATED' || upper == 'NOT_GENERATED') {
      bg = const Color(0xFFF1F5F9);
      text = const Color(0xFF64748B);
      icon = Icons.pending_outlined;
      label = 'NOT GENERATED';
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 10, color: text),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.bold,
              color: text,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 40, horizontal: 24),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 64,
              height: 64,
              decoration: const BoxDecoration(
                color: Color(0xFFF8FAFC),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.search_off_rounded,
                size: 32,
                color: AppColors.textMuted,
              ),
            ),
            const SizedBox(height: 14),
            const Text(
              'No schedule weeks found',
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              _searchQuery.isNotEmpty
                  ? 'No weeks match "$_searchQuery". Try searching with another date or clear your search.'
                  : 'No weeks found with status "$_selectedStatusFilter".',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textMuted,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 16),
            OutlinedButton.icon(
              onPressed: () {
                _searchController.clear();
                setState(() {
                  _searchQuery = '';
                  _selectedStatusFilter = 'ALL';
                });
              },
              icon: const Icon(Icons.refresh_rounded, size: 16),
              label: const Text('Reset Search & Filters'),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: const BorderSide(color: AppColors.primary),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
