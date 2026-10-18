import 'package:flutter/material.dart';
import '../../controllers/home_controller.dart';
import '../../models/attendance_history_model.dart';
import '../../models/attendance_shift_model.dart';
import '../../services/home_service.dart';
import '../../theme/app_colors.dart';
import '../home/handlers/attendance_flow_handler.dart';
import 'attendance_detail_view.dart';

class AttendanceHistoryView extends StatefulWidget {
  final HomeController homeController;

  const AttendanceHistoryView({
    super.key,
    required this.homeController,
  });

  @override
  State<AttendanceHistoryView> createState() => _AttendanceHistoryViewState();
}

class _AttendanceHistoryViewState extends State<AttendanceHistoryView> {
  final HomeService _homeService = HomeService();
  final TextEditingController _searchController = TextEditingController();

  bool _isLoading = true;
  String? _errorMessage;
  List<StaffAttendanceHistoryModel> _history = [];
  String _selectedFilter = 'ALL';

  @override
  void initState() {
    super.initState();
    _loadHistory();
    _searchController.addListener(_onSearchChanged);
  }

  @override
  void dispose() {
    _searchController.removeListener(_onSearchChanged);
    _searchController.dispose();
    super.dispose();
  }

  void _onSearchChanged() {
    setState(() {});
  }

  Future<void> _loadHistory() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final results = await _homeService.getAttendanceHistory();
      if (mounted) {
        setState(() {
          _history = results;
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

  String _formatTime(DateTime? dt) {
    if (dt == null) return '--:--';
    final local = dt.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year.toString();
    return '$day/$month/$year';
  }

  String _formatDateWithDay(DateTime dt) {
    final local = dt.toLocal();
    const weekdays = [
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday'
    ];
    final dayOfWeek = weekdays[local.weekday - 1];
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year.toString();
    return '$dayOfWeek, $day/$month/$year';
  }

  String _formatDuration(int minutes) {
    if (minutes <= 0) return '0m';
    final h = minutes ~/ 60;
    final m = minutes % 60;
    if (h > 0) {
      return '${h}h ${m}m';
    }
    return '${m}m';
  }

  int _getCategoryCount(String category) {
    switch (category) {
      case 'IN_PROGRESS':
        return _history.where((s) => s.status.toUpperCase() == 'IN_PROGRESS' || s.canCheckInNow).length;
      case 'COMPLETED':
        return _history.where((s) => s.status.toUpperCase() == 'COMPLETED').length;
      case 'VIOLATION':
        return _history.where((s) => s.totalLateMinutes > 0 || s.totalEarlyLeaveMinutes > 0 || s.status.toUpperCase() == 'INCOMPLETE').length;
      case 'ABSENT':
        return _history.where((s) => s.status.toUpperCase() == 'ABSENT' || s.status.toUpperCase() == 'ON_LEAVE').length;
      case 'ALL':
      default:
        return _history.length;
    }
  }

  List<StaffAttendanceHistoryModel> get _filteredHistory {
    final query = _searchController.text.trim().toLowerCase();
    return _history.where((item) {
      // 1. Status Filter match
      bool matchesCategory = true;
      switch (_selectedFilter) {
        case 'IN_PROGRESS':
          matchesCategory = item.status.toUpperCase() == 'IN_PROGRESS' || item.canCheckInNow;
          break;
        case 'COMPLETED':
          matchesCategory = item.status.toUpperCase() == 'COMPLETED';
          break;
        case 'VIOLATION':
          matchesCategory = item.totalLateMinutes > 0 ||
              item.totalEarlyLeaveMinutes > 0 ||
              item.status.toUpperCase() == 'INCOMPLETE';
          break;
        case 'ABSENT':
          matchesCategory = item.status.toUpperCase() == 'ABSENT' ||
              item.status.toUpperCase() == 'ON_LEAVE';
          break;
        case 'ALL':
        default:
          matchesCategory = true;
          break;
      }

      if (!matchesCategory) return false;

      // 2. Search Query match
      if (query.isEmpty) return true;

      final shiftName = item.shiftName.toLowerCase();
      final branchName = item.branchName.toLowerCase();
      final dateStr = _formatDate(item.scheduledStartAt).toLowerCase();
      final fullDateStr = _formatDateWithDay(item.scheduledStartAt).toLowerCase();

      return shiftName.contains(query) ||
          branchName.contains(query) ||
          dateStr.contains(query) ||
          fullDateStr.contains(query);
    }).toList();
  }

  EligibleShiftModel _toEligibleShift(StaffAttendanceHistoryModel h) {
    return EligibleShiftModel(
      assignmentId: h.assignmentId,
      shiftId: h.shiftId,
      shiftName: h.shiftName,
      scheduledStartAt: h.scheduledStartAt,
      scheduledEndAt: h.scheduledEndAt,
      branchId: 0,
      branchName: h.branchName,
      branchAddress: h.branchAddress,
      branchLatitude: h.branchLatitude,
      branchLongitude: h.branchLongitude,
      maxAttendanceDistanceMeters: h.maxAttendanceDistanceMeters,
      canCheckInNow: h.canCheckInNow,
      canNotCheckInReason: h.cannotCheckInReason,
      checkOutCutoffAt: h.checkOutCutoffAt,
      attendanceStatus: h.status,
      hasApprovedLeave: false,
      hasCheckedIn: h.checkInTime != null,
      hasCheckedOut: h.checkOutTime != null,
      checkInTime: h.checkInTime,
      checkOutTime: h.checkOutTime,
      canCheckOutNow: h.canCheckOutNow,
      canNotCheckOutReason: h.cannotCheckOutReason,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Attendance History',
          style: TextStyle(
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
          IconButton(
            icon: const Icon(Icons.refresh_rounded, color: AppColors.textPrimary, size: 22),
            tooltip: 'Refresh',
            onPressed: _loadHistory,
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadHistory,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 48),
              const SizedBox(height: 12),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _loadHistory,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_history.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.history_toggle_off_rounded, size: 56, color: AppColors.textMuted),
              SizedBox(height: 14),
              Text(
                'No Attendance Records',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textSecondary,
                ),
              ),
              SizedBox(height: 6),
              Text(
                'Your shift attendance history will appear here.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.textMuted, fontSize: 13),
              ),
            ],
          ),
        ),
      );
    }

    final filtered = _filteredHistory;

    return Column(
      children: [
        // Search & Filter Header pinned at top
        _buildSearchAndFilterHeader(),

        // Result Count Banner (if filter is applied)
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Showing ${filtered.length} shift(s)',
                style: const TextStyle(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textSecondary,
                ),
              ),
              if (_searchController.text.isNotEmpty || _selectedFilter != 'ALL')
                GestureDetector(
                  onTap: () {
                    setState(() {
                      _searchController.clear();
                      _selectedFilter = 'ALL';
                    });
                  },
                  child: const Text(
                    'Reset filters',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.primary,
                    ),
                  ),
                ),
            ],
          ),
        ),

        // List of cards or search empty state
        Expanded(
          child: filtered.isEmpty
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Icon(Icons.search_off_rounded, size: 52, color: AppColors.textMuted),
                        const SizedBox(height: 12),
                        const Text(
                          'No matching shifts found',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 6),
                        const Text(
                          'Try adjusting your search terms or clearing filters.',
                          textAlign: TextAlign.center,
                          style: TextStyle(color: AppColors.textMuted, fontSize: 13),
                        ),
                        const SizedBox(height: 14),
                        OutlinedButton(
                          onPressed: () {
                            setState(() {
                              _searchController.clear();
                              _selectedFilter = 'ALL';
                            });
                          },
                          child: const Text('Clear filters'),
                        ),
                      ],
                    ),
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 20),
                  itemCount: filtered.length,
                  separatorBuilder: (_, index) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final item = filtered[index];
                    return _buildHistoryCard(item);
                  },
                ),
        ),
      ],
    );
  }

  Widget _buildSearchAndFilterHeader() {
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
      child: Column(
        children: [
          // 1. Search Bar
          Container(
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: TextField(
              controller: _searchController,
              style: const TextStyle(fontSize: 14, color: AppColors.textPrimary),
              decoration: InputDecoration(
                hintText: 'Search by shift, branch, date...',
                hintStyle: const TextStyle(fontSize: 13.5, color: AppColors.textMuted),
                prefixIcon: const Icon(Icons.search_rounded, color: AppColors.textSecondary, size: 20),
                suffixIcon: _searchController.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.cancel_rounded, color: AppColors.textMuted, size: 18),
                        onPressed: () {
                          _searchController.clear();
                        },
                      )
                    : null,
                border: InputBorder.none,
                contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              ),
            ),
          ),
          const SizedBox(height: 10),

          // 2. Horizontal Filter Chips
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _buildFilterChip(id: 'ALL', label: 'All'),
                const SizedBox(width: 8),
                _buildFilterChip(id: 'IN_PROGRESS', label: 'In Progress'),
                const SizedBox(width: 8),
                _buildFilterChip(id: 'COMPLETED', label: 'Completed'),
                const SizedBox(width: 8),
                _buildFilterChip(id: 'VIOLATION', label: 'Violations'),
                const SizedBox(width: 8),
                _buildFilterChip(id: 'ABSENT', label: 'Absent / Leave'),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip({required String id, required String label}) {
    final isSelected = _selectedFilter == id;
    final count = _getCategoryCount(id);

    return InkWell(
      onTap: () {
        setState(() {
          _selectedFilter = id;
        });
      },
      borderRadius: BorderRadius.circular(20),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary : const Color(0xFFF1F5F9),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: isSelected ? AppColors.primary : const Color(0xFFE2E8F0),
          ),
          boxShadow: isSelected
              ? [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.25),
                    blurRadius: 6,
                    offset: const Offset(0, 2),
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
                fontSize: 12.5,
                fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                color: isSelected ? Colors.white : AppColors.textSecondary,
              ),
            ),
            const SizedBox(width: 5),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
              decoration: BoxDecoration(
                color: isSelected
                    ? Colors.white.withValues(alpha: 0.25)
                    : const Color(0xFFE2E8F0),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                '$count',
                style: TextStyle(
                  fontSize: 11,
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

  Widget _buildStatusBadge(StaffAttendanceHistoryModel item) {
    Color badgeBg;
    Color badgeText;
    String badgeTitle;
    IconData badgeIcon;

    switch (item.status.toUpperCase()) {
      case 'READY_TO_CHECK_IN':
        badgeBg = const Color(0xFFE8F5E9);
        badgeText = const Color(0xFF166534);
        badgeTitle = 'Ready to Check-In';
        badgeIcon = Icons.login_rounded;
        break;
      case 'UPCOMING':
        badgeBg = const Color(0xFFEFF6FF);
        badgeText = const Color(0xFF1D4ED8);
        badgeTitle = 'Upcoming';
        badgeIcon = Icons.schedule_rounded;
        break;
      case 'CHECKIN_EXPIRED':
        badgeBg = const Color(0xFFFEF3C7);
        badgeText = const Color(0xFFB45309);
        badgeTitle = 'Check-in Expired';
        badgeIcon = Icons.alarm_off_rounded;
        break;
      case 'COMPLETED':
        badgeBg = const Color(0xFFDCFCE7);
        badgeText = const Color(0xFF15803D);
        badgeTitle = 'Completed';
        badgeIcon = Icons.check_circle_rounded;
        break;
      case 'IN_PROGRESS':
        badgeBg = const Color(0xFFFFEDD5);
        badgeText = const Color(0xFFC2410C);
        badgeTitle = 'In Progress';
        badgeIcon = Icons.timelapse_rounded;
        break;
      case 'INCOMPLETE':
        badgeBg = const Color(0xFFFEE2E2);
        badgeText = const Color(0xFFB91C1C);
        badgeTitle = 'Incomplete';
        badgeIcon = Icons.warning_amber_rounded;
        break;
      case 'ABSENT':
        badgeBg = const Color(0xFFF1F5F9);
        badgeText = const Color(0xFF475569);
        badgeTitle = 'Absent';
        badgeIcon = Icons.cancel_outlined;
        break;
      case 'ON_LEAVE':
        badgeBg = const Color(0xFFF3E8FF);
        badgeText = const Color(0xFF7E22CE);
        badgeTitle = 'On Leave';
        badgeIcon = Icons.event_busy_rounded;
        break;
      default:
        badgeBg = const Color(0xFFF1F5F9);
        badgeText = const Color(0xFF475569);
        badgeTitle = item.status;
        badgeIcon = Icons.info_outline_rounded;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: badgeBg,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(badgeIcon, size: 13, color: badgeText),
          const SizedBox(width: 4),
          Text(
            badgeTitle,
            style: TextStyle(
              fontSize: 11.5,
              fontWeight: FontWeight.bold,
              color: badgeText,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHistoryCard(StaffAttendanceHistoryModel item) {
    final hasPenalties = item.totalLateMinutes > 0 || item.totalEarlyLeaveMinutes > 0;
    final isSpecialStatus = item.status.toUpperCase() == 'IN_PROGRESS' || item.canCheckInNow;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isSpecialStatus
              ? const Color(0xFF93C5FD)
              : const Color(0xFFE2E8F0),
          width: isSpecialStatus ? 1.5 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: () {
            Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => AttendanceDetailView(
                  assignmentId: item.assignmentId,
                ),
              ),
            );
          },
          borderRadius: BorderRadius.circular(16),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // 1. Date Header & Status Badge
                Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                      decoration: BoxDecoration(
                        color: const Color(0xFFEFF6FF),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.calendar_today_rounded, size: 12, color: AppColors.primary),
                          const SizedBox(width: 5),
                          Text(
                            _formatDateWithDay(item.scheduledStartAt),
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.primaryDark,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Spacer(),
                    _buildStatusBadge(item),
                  ],
                ),
                const SizedBox(height: 12),

                // 2. Shift Name & Scheduled Time
                Text(
                  item.shiftName,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 6),

                // Branch & Scheduled Hours Row
                Row(
                  children: [
                    const Icon(Icons.access_time_rounded, size: 14, color: AppColors.textSecondary),
                    const SizedBox(width: 5),
                    Text(
                      '${_formatTime(item.scheduledStartAt)} – ${_formatTime(item.scheduledEndAt)}',
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(width: 12),
                    const Icon(Icons.location_on_outlined, size: 14, color: AppColors.textMuted),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        item.branchName,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontSize: 12.5, color: AppColors.textMuted),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // 3. Mini Check-in / Check-out Grid
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8FAFC),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: const Color(0xFFE2E8F0)),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      // Check-in
                      Expanded(
                        child: Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(5),
                              decoration: BoxDecoration(
                                color: item.checkInTime != null
                                    ? const Color(0xFFDCFCE7)
                                    : const Color(0xFFF1F5F9),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Icon(
                                Icons.login_rounded,
                                size: 14,
                                color: item.checkInTime != null
                                    ? const Color(0xFF15803D)
                                    : AppColors.textMuted,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Check-In',
                                  style: TextStyle(fontSize: 11, color: AppColors.textMuted),
                                ),
                                Text(
                                  _formatTime(item.checkInTime),
                                  style: TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                    color: item.checkInTime != null
                                        ? AppColors.textPrimary
                                        : AppColors.textMuted,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),

                      // Check-out
                      Expanded(
                        child: Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(5),
                              decoration: BoxDecoration(
                                color: item.checkOutTime != null
                                    ? const Color(0xFFFFEDD5)
                                    : const Color(0xFFF1F5F9),
                                borderRadius: BorderRadius.circular(6),
                              ),
                              child: Icon(
                                Icons.logout_rounded,
                                size: 14,
                                color: item.checkOutTime != null
                                    ? const Color(0xFFEA580C)
                                    : AppColors.textMuted,
                              ),
                            ),
                            const SizedBox(width: 8),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Check-Out',
                                  style: TextStyle(fontSize: 11, color: AppColors.textMuted),
                                ),
                                Text(
                                  _formatTime(item.checkOutTime),
                                  style: TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                    color: item.checkOutTime != null
                                        ? AppColors.textPrimary
                                        : AppColors.textMuted,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ),

                      // Working Hours
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          const Text(
                            'Duration',
                            style: TextStyle(fontSize: 11, color: AppColors.textMuted),
                          ),
                          Text(
                            item.actualWorkingMinutes > 0
                                ? _formatDuration(item.actualWorkingMinutes)
                                : '--',
                            style: const TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.bold,
                              color: AppColors.primary,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // 4. Violation notice tags
                if (hasPenalties || item.status.toUpperCase() == 'INCOMPLETE') ...[
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    runSpacing: 4,
                    children: [
                      if (item.totalLateMinutes > 0)
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEF3C7),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(Icons.alarm_off_rounded, size: 12, color: Color(0xFFB45309)),
                              const SizedBox(width: 4),
                              Text(
                                'Late: ${item.totalLateMinutes}m',
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFFB45309),
                                ),
                              ),
                            ],
                          ),
                        ),
                      if (item.totalEarlyLeaveMinutes > 0)
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFFF7ED),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              const Icon(Icons.directions_run_rounded, size: 12, color: Color(0xFFEA580C)),
                              const SizedBox(width: 4),
                              Text(
                                'Early Leave: ${item.totalEarlyLeaveMinutes}m',
                                style: const TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFFEA580C),
                                ),
                              ),
                            ],
                          ),
                        ),
                      if (item.status.toUpperCase() == 'INCOMPLETE')
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                          decoration: BoxDecoration(
                            color: const Color(0xFFFEE2E2),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: const Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(Icons.warning_amber_rounded, size: 12, color: Color(0xFFB91C1C)),
                              SizedBox(width: 4),
                              Text(
                                'Incomplete Shift',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.bold,
                                  color: Color(0xFFB91C1C),
                                ),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                ],

                // 5. Actions (Check-In / Check-Out) if available
                if (item.canCheckInNow) ...[
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                        elevation: 0,
                      ),
                      icon: const Icon(Icons.login_rounded, size: 16),
                      label: const Text(
                        'Check In Now',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                      ),
                      onPressed: () async {
                        await AttendanceFlowHandler.handleCheckIn(
                          context: context,
                          homeController: widget.homeController,
                          targetShift: _toEligibleShift(item),
                        );
                        _loadHistory();
                      },
                    ),
                  ),
                ],

                if (item.canCheckOutNow) ...[
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFFEA580C),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 10),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10),
                        ),
                        elevation: 0,
                      ),
                      icon: const Icon(Icons.logout_rounded, size: 16),
                      label: const Text(
                        'Check Out Now',
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                      ),
                      onPressed: () async {
                        await AttendanceFlowHandler.handleCheckOut(
                          context: context,
                          homeController: widget.homeController,
                          targetShift: _toEligibleShift(item),
                        );
                        _loadHistory();
                      },
                    ),
                  ),
                ],

                // 6. View Details CTA
                const SizedBox(height: 10),
                const Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    Text(
                      'View Details',
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: AppColors.primary,
                      ),
                    ),
                    SizedBox(width: 4),
                    Icon(Icons.arrow_forward_ios_rounded, size: 11, color: AppColors.primary),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
