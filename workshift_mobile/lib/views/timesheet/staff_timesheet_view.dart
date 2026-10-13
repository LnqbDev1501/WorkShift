import 'package:flutter/material.dart';
import '../../controllers/staff_timesheet_controller.dart';
import '../../models/staff_timesheet_model.dart';
import '../../theme/app_colors.dart';

class StaffTimesheetView extends StatefulWidget {
  const StaffTimesheetView({super.key});

  @override
  State<StaffTimesheetView> createState() => _StaffTimesheetViewState();
}

class _StaffTimesheetViewState extends State<StaffTimesheetView> {
  final StaffTimesheetController _controller = StaffTimesheetController();

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onControllerUpdated);
    _controller.loadTimesheet();
  }

  @override
  void dispose() {
    _controller.removeListener(_onControllerUpdated);
    _controller.dispose();
    super.dispose();
  }

  void _onControllerUpdated() {
    if (mounted) {
      setState(() {});
    }
  }

  void _showWeekSelectionModal() {
    final weeks = _controller.availableWeeks;
    if (weeks.isEmpty) return;

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 20, 20, 12),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Select Schedule Week',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close_rounded),
                      onPressed: () => Navigator.pop(ctx),
                      color: AppColors.textSecondary,
                    ),
                  ],
                ),
              ),
              const Divider(height: 1, color: AppColors.border),
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  itemCount: weeks.length,
                  separatorBuilder: (_, _) => const Divider(height: 1, color: AppColors.borderLight),
                  itemBuilder: (_, index) {
                    final item = weeks[index];
                    final isSelected = item.scheduleWeekId == _controller.selectedWeekId;

                    return ListTile(
                      contentPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 4),
                      title: Text(
                        'Week ${item.period}',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                          color: isSelected ? AppColors.primary : AppColors.textPrimary,
                        ),
                      ),
                      subtitle: Text(
                        '${item.weekStartDate} – ${item.weekEndDate}',
                        style: const TextStyle(fontSize: 12, color: AppColors.textMuted),
                      ),
                      trailing: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          _buildStatusPill(item.status),
                          const SizedBox(width: 8),
                          if (isSelected)
                            const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20),
                        ],
                      ),
                      onTap: () {
                        Navigator.pop(ctx);
                        _controller.selectWeek(item.scheduleWeekId);
                      },
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Future<void> _handleConfirmTimesheet() async {
    final noteController = TextEditingController();

    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(
          children: [
            Icon(Icons.verified_rounded, color: AppColors.success, size: 26),
            SizedBox(width: 10),
            Text(
              'Confirm Timesheet',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Are you sure you have verified all work hours, late minutes, and attendance records for this week? Once confirmed, this timesheet will be ready for final payroll locking.',
              style: TextStyle(fontSize: 14, color: AppColors.textSecondary, height: 1.4),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: noteController,
              decoration: InputDecoration(
                hintText: 'Optional confirmation note...',
                hintStyle: const TextStyle(fontSize: 13, color: AppColors.textMuted),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(color: AppColors.border),
                ),
                contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              ),
              maxLines: 2,
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel', style: TextStyle(color: AppColors.textSecondary)),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
            ),
            child: const Text('Confirm Now'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      final success = await _controller.confirmTimesheet(note: noteController.text);
      if (mounted) {
        if (success) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Weekly timesheet confirmed successfully!'),
              backgroundColor: AppColors.success,
            ),
          );
        } else if (_controller.errorMessage != null) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(_controller.errorMessage!),
              backgroundColor: AppColors.error,
            ),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'My Timesheet',
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
        ),
        centerTitle: false,
        backgroundColor: Colors.white,
        elevation: 0,
        surfaceTintColor: Colors.transparent,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            tooltip: 'Refresh Timesheet',
            onPressed: () => _controller.loadTimesheet(scheduleWeekId: _controller.selectedWeekId),
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_controller.isLoading) {
      return const Center(
        child: CircularProgressIndicator(
          strokeWidth: 2.5,
          color: AppColors.primary,
        ),
      );
    }

    if (_controller.errorMessage != null && _controller.timesheet == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(16),
                decoration: const BoxDecoration(
                  color: AppColors.errorBg,
                  shape: BoxShape.circle,
                ),
                child: const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 36),
              ),
              const SizedBox(height: 16),
              const Text(
                'Unable to Load Timesheet',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
              ),
              const SizedBox(height: 8),
              Text(
                _controller.errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
              ),
              const SizedBox(height: 20),
              ElevatedButton.icon(
                onPressed: () => _controller.loadTimesheet(),
                icon: const Icon(Icons.refresh_rounded, size: 18),
                label: const Text('Try Again'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
              ),
            ],
          ),
        ),
      );
    }

    final timesheet = _controller.timesheet;
    if (timesheet == null) {
      return const Center(
        child: Text(
          'No timesheet records available.',
          style: TextStyle(color: AppColors.textSecondary),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => _controller.loadTimesheet(scheduleWeekId: _controller.selectedWeekId),
      color: AppColors.primary,
      child: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // 1. Week Selector Banner
                  _buildWeekSelector(timesheet),
                  const SizedBox(height: 16),

                  // 2. Weekly KPI Summary Cards (2x2 Grid)
                  _buildKpiGrid(timesheet),
                  const SizedBox(height: 20),

                  // 3. Shift Breakdown Header
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          const Icon(Icons.event_available_rounded, size: 20, color: AppColors.primary),
                          const SizedBox(width: 8),
                          const Text(
                            'Shifts Breakdown',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: const Color(0xFFBFDBFE)),
                        ),
                        child: Text(
                          '${timesheet.shifts.length} Shifts',
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: AppColors.primary,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),

                  // 4. Shifts List
                  if (timesheet.shifts.isEmpty)
                    _buildEmptyShiftsCard()
                  else
                    ...timesheet.shifts.map(_buildShiftCard),
                ],
              ),
            ),
          ),

          // 5. Sticky Bottom Action Bar
          _buildBottomActionBar(timesheet),
        ],
      ),
    );
  }

  Widget _buildWeekSelector(StaffWeeklyTimesheetModel timesheet) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0A000000),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              IconButton(
                icon: const Icon(Icons.chevron_left_rounded),
                onPressed: _controller.hasPreviousWeek
                    ? () => _controller.navigateWeek(next: false)
                    : null,
                color: AppColors.textPrimary,
                disabledColor: AppColors.border,
                tooltip: 'Previous week',
              ),
              InkWell(
                onTap: _showWeekSelectionModal,
                borderRadius: BorderRadius.circular(10),
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  child: Row(
                    children: [
                      Column(
                        children: [
                          Text(
                            'Week ${timesheet.period}',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            '${timesheet.weekStartDate} – ${timesheet.weekEndDate}',
                            style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
                          ),
                        ],
                      ),
                      const SizedBox(width: 4),
                      const Icon(Icons.arrow_drop_down_rounded, color: AppColors.textSecondary),
                    ],
                  ),
                ),
              ),
              IconButton(
                icon: const Icon(Icons.chevron_right_rounded),
                onPressed: _controller.hasNextWeek
                    ? () => _controller.navigateWeek(next: true)
                    : null,
                color: AppColors.textPrimary,
                disabledColor: AppColors.border,
                tooltip: 'Next week',
              ),
            ],
          ),
          const Divider(height: 16, color: AppColors.borderLight),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  const Icon(Icons.storefront_rounded, size: 16, color: AppColors.textMuted),
                  const SizedBox(width: 6),
                  Text(
                    timesheet.branchName,
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w500, color: AppColors.textSecondary),
                  ),
                ],
              ),
              _buildReviewStatusBadge(timesheet),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildKpiGrid(StaffWeeklyTimesheetModel timesheet) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _buildKpiCard(
                title: 'Total Work Time',
                value: timesheet.totalWorkTimeDisplay,
                subtitle: '${timesheet.totalWorkHours.toStringAsFixed(1)} hrs compiled',
                icon: Icons.schedule_rounded,
                iconColor: const Color(0xFF2563EB),
                bgColor: const Color(0xFFEFF6FF),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildKpiCard(
                title: 'Shifts Done',
                value: '${timesheet.completedShifts} / ${timesheet.totalAssignedShifts}',
                subtitle: 'Assigned in week',
                icon: Icons.task_alt_rounded,
                iconColor: const Color(0xFF10B981),
                bgColor: const Color(0xFFD1FAE5),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildKpiCard(
                title: 'Total Late',
                value: timesheet.totalLateDisplay,
                subtitle: '${timesheet.totalLateMinutes} mins late',
                icon: Icons.alarm_off_rounded,
                iconColor: timesheet.totalLateMinutes > 0 ? const Color(0xFFD97706) : AppColors.textMuted,
                bgColor: timesheet.totalLateMinutes > 0 ? const Color(0xFFFEF3C7) : const Color(0xFFF1F5F9),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildKpiCard(
                title: 'Early Leave',
                value: timesheet.totalEarlyLeaveDisplay,
                subtitle: '${timesheet.totalEarlyLeaveMinutes} mins early',
                icon: Icons.logout_rounded,
                iconColor: timesheet.totalEarlyLeaveMinutes > 0 ? const Color(0xFFEA580C) : AppColors.textMuted,
                bgColor: timesheet.totalEarlyLeaveMinutes > 0 ? const Color(0xFFFFEDD5) : const Color(0xFFF1F5F9),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildKpiCard({
    required String title,
    required String value,
    required String subtitle,
    required IconData icon,
    required Color iconColor,
    required Color bgColor,
  }) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                title,
                style: const TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: AppColors.textSecondary),
              ),
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(color: bgColor, borderRadius: BorderRadius.circular(8)),
                child: Icon(icon, size: 16, color: iconColor),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
          ),
          const SizedBox(height: 2),
          Text(
            subtitle,
            style: const TextStyle(fontSize: 10, color: AppColors.textMuted),
          ),
        ],
      ),
    );
  }

  Widget _buildShiftCard(StaffShiftAttendanceModel shift) {
    final status = shift.attendanceStatus.toUpperCase();

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Row 1: Date & Status Badge
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                shift.dateDisplay,
                style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
              ),
              _buildAttendanceStatusPill(status),
            ],
          ),
          const SizedBox(height: 4),

          // Row 2: Shift Name & Scheduled Time
          Row(
            children: [
              const Icon(Icons.schedule_rounded, size: 14, color: AppColors.textMuted),
              const SizedBox(width: 4),
              Text(
                '${shift.shiftName} (${shift.scheduledRangeDisplay})',
                style: const TextStyle(fontSize: 12, color: AppColors.textSecondary, fontWeight: FontWeight.w500),
              ),
            ],
          ),
          const Divider(height: 18, color: AppColors.borderLight),

          // Row 3: Actual Check-In / Check-Out
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Check-In', style: TextStyle(fontSize: 11, color: AppColors.textMuted)),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        Text(
                          shift.checkInDisplay,
                          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                        ),
                        if (shift.totalLateMinutes > 0) ...[
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFEF3C7),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Text(
                              'Late ${shift.lateDisplay}',
                              style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFFB45309)),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Check-Out', style: TextStyle(fontSize: 11, color: AppColors.textMuted)),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        Text(
                          shift.checkOutDisplay,
                          style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                        ),
                        if (shift.totalEarlyLeaveMinutes > 0) ...[
                          const SizedBox(width: 6),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFFEDD5),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Text(
                              'Early ${shift.earlyLeaveDisplay}',
                              style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFFC2410C)),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),

          // Row 4: Computed Work Time
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: AppColors.borderLight),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Compiled Work Time',
                  style: TextStyle(fontSize: 11, color: AppColors.textSecondary),
                ),
                Text(
                  shift.workTimeDisplay,
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.primary),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyShiftsCard() {
    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: const Center(
        child: Column(
          children: [
            Icon(Icons.event_busy_rounded, size: 36, color: AppColors.textMuted),
            SizedBox(height: 8),
            Text(
              'No Shifts Assigned This Week',
              style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
            ),
            SizedBox(height: 4),
            Text(
              'You have no shift schedule records for the selected week.',
              style: TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewStatusBadge(StaffWeeklyTimesheetModel timesheet) {
    if (timesheet.isLocked) {
      return _buildBadge(label: 'LOCKED', bgColor: const Color(0xFFE0F2FE), textColor: const Color(0xFF0284C7));
    }
    if (timesheet.staffReviewStatus.toUpperCase() == 'CONFIRMED') {
      return _buildBadge(label: 'CONFIRMED', bgColor: const Color(0xFFD1FAE5), textColor: const Color(0xFF059669));
    }
    if (timesheet.status.toUpperCase() == 'GENERATED') {
      return _buildBadge(label: 'PENDING REVIEW', bgColor: const Color(0xFFFEF3C7), textColor: const Color(0xFFD97706));
    }
    return _buildBadge(label: 'NOT GENERATED', bgColor: const Color(0xFFF1F5F9), textColor: const Color(0xFF64748B));
  }

  Widget _buildStatusPill(String status) {
    final upper = status.toUpperCase();
    if (upper == 'LOCKED') {
      return _buildBadge(label: 'LOCKED', bgColor: const Color(0xFFE0F2FE), textColor: const Color(0xFF0284C7));
    }
    if (upper == 'GENERATED') {
      return _buildBadge(label: 'GENERATED', bgColor: const Color(0xFFFEF3C7), textColor: const Color(0xFFD97706));
    }
    return _buildBadge(label: 'NOT GENERATED', bgColor: const Color(0xFFF1F5F9), textColor: const Color(0xFF64748B));
  }

  Widget _buildAttendanceStatusPill(String status) {
    Color bg = const Color(0xFFF1F5F9);
    Color text = const Color(0xFF64748B);

    if (status == 'COMPLETED') {
      bg = const Color(0xFFD1FAE5);
      text = const Color(0xFF059669);
    } else if (status == 'PRESENT') {
      bg = const Color(0xFFDBEAFE);
      text = const Color(0xFF2563EB);
    } else if (status == 'ABSENT') {
      bg = const Color(0xFFFEE2E2);
      text = const Color(0xFFDC2626);
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(
        status,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: text),
      ),
    );
  }

  Widget _buildBadge({required String label, required Color bgColor, required Color textColor}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(color: bgColor, borderRadius: BorderRadius.circular(8)),
      child: Text(
        label,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: textColor),
      ),
    );
  }

  Widget _buildBottomActionBar(StaffWeeklyTimesheetModel timesheet) {
    if (timesheet.canConfirm) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: const BoxDecoration(
          color: Colors.white,
          border: Border(top: BorderSide(color: AppColors.border)),
          boxShadow: [
            BoxShadow(
              color: Color(0x0F000000),
              blurRadius: 10,
              offset: Offset(0, -4),
            ),
          ],
        ),
        child: SizedBox(
          width: double.infinity,
          height: 48,
          child: ElevatedButton.icon(
            onPressed: _controller.isConfirming ? null : _handleConfirmTimesheet,
            icon: _controller.isConfirming
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                  )
                : const Icon(Icons.check_circle_outline_rounded, size: 20),
            label: Text(
              _controller.isConfirming ? 'Confirming...' : 'Confirm Weekly Timesheet',
              style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
            ),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              elevation: 0,
            ),
          ),
        ),
      );
    }

    if (timesheet.staffReviewStatus.toUpperCase() == 'CONFIRMED') {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(
          color: Color(0xFFECFDF5),
          border: Border(top: BorderSide(color: Color(0xFFA7F3D0))),
        ),
        child: const Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.verified_rounded, color: Color(0xFF059669), size: 18),
            SizedBox(width: 8),
            Text(
              'You have confirmed this weekly timesheet.',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Color(0xFF047857)),
            ),
          ],
        ),
      );
    }

    if (timesheet.isLocked) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(
          color: Color(0xFFF0F9FF),
          border: Border(top: BorderSide(color: Color(0xFFBAE6FD))),
        ),
        child: const Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.lock_rounded, color: Color(0xFF0284C7), size: 18),
            SizedBox(width: 8),
            Text(
              'Timesheet locked for payroll calculation.',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Color(0xFF0369A1)),
            ),
          ],
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
