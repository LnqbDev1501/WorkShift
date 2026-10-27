import 'package:flutter/material.dart';
import '../../controllers/staff_timesheet_controller.dart';
import '../../models/staff_timesheet_model.dart';
import '../../theme/app_colors.dart';
import 'components/timesheet_adjustment_sheet.dart';
import 'components/week_selection_bottom_sheet.dart';

class StaffTimesheetView extends StatefulWidget {
  const StaffTimesheetView({super.key});

  @override
  State<StaffTimesheetView> createState() => _StaffTimesheetViewState();
}

class _StaffTimesheetViewState extends State<StaffTimesheetView> {
  final StaffTimesheetController _controller = StaffTimesheetController();
  final Set<int> _selectedAssignmentIds = {};

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

  void _toggleShiftSelection(int assignmentId) {
    setState(() {
      if (_selectedAssignmentIds.contains(assignmentId)) {
        _selectedAssignmentIds.remove(assignmentId);
      } else {
        _selectedAssignmentIds.add(assignmentId);
      }
    });
  }

  void _selectAllEligibleShifts() {
    final shifts = _controller.timesheet?.shifts ?? [];
    setState(() {
      final eligible = shifts.where((s) => s.canRequestAdjustment).map((s) => s.assignmentId);
      if (_selectedAssignmentIds.length == eligible.length) {
        _selectedAssignmentIds.clear();
      } else {
        _selectedAssignmentIds.addAll(eligible);
      }
    });
  }

  void _clearSelection() {
    setState(() {
      _selectedAssignmentIds.clear();
    });
  }

  Future<void> _handleOpenAdjustmentSheet({StaffShiftAttendanceModel? singleShift}) async {
    final timesheet = _controller.timesheet;
    if (timesheet == null) return;

    final targetShifts = singleShift != null
        ? [singleShift]
        : timesheet.shifts.where((s) => _selectedAssignmentIds.contains(s.assignmentId)).toList();

    if (targetShifts.isEmpty) return;

    final submitted = await TimesheetAdjustmentSheet.show(
      context,
      selectedShifts: targetShifts,
      checkInBeforeMinutes: timesheet.checkInBeforeMinutes,
      checkOutAfterMinutes: timesheet.checkOutAfterMinutes,
      onSubmit: (items) => _controller.submitAdjustments(
        adjustments: items,
      ),
    );

    if (submitted == true && mounted) {
      _clearSelection();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Timesheet adjustment request submitted successfully! Waiting for manager review.'),
          backgroundColor: AppColors.success,
        ),
      );
    }
  }

  Future<void> _showWeekSelectionModal() async {
    final weeks = _controller.availableWeeks;
    if (weeks.isEmpty) return;

    final selectedWeekId = await WeekSelectionBottomSheet.show(
      context: context,
      weeks: weeks,
      selectedWeekId: _controller.selectedWeekId,
    );

    if (selectedWeekId != null && selectedWeekId != _controller.selectedWeekId) {
      _clearSelection();
      await _controller.selectWeek(selectedWeekId);
    }
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
          _clearSelection();
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

  Future<void> _handleCancelAdjustment(StaffShiftAttendanceModel shift) async {
    if (shift.adjustmentRequestId == null) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(
          children: [
            Icon(Icons.warning_amber_rounded, color: AppColors.warning),
            SizedBox(width: 8),
            Text('Cancel Adjustment?'),
          ],
        ),
        content: Text(
          'Are you sure you want to cancel your adjustment request for ${shift.shiftName} on ${shift.dateDisplay}? This will immediately release the lock on this shift.',
          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Keep Request', style: TextStyle(color: AppColors.textMuted)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Cancel Request'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
      final error = await _controller.cancelAdjustment(
        adjustmentRequestId: shift.adjustmentRequestId!,
      );

      if (mounted) {
        if (error == null) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Adjustment request cancelled successfully.'),
              backgroundColor: AppColors.success,
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Failed to cancel adjustment: $error'),
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
            onPressed: () {
              _clearSelection();
              _controller.loadTimesheet(scheduleWeekId: _controller.selectedWeekId);
            },
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

    final eligibleShiftsCount = timesheet.shifts.where((s) => s.canRequestAdjustment).length;

    return RefreshIndicator(
      onRefresh: () async {
        _clearSelection();
        await _controller.loadTimesheet(scheduleWeekId: _controller.selectedWeekId);
      },
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
                  const SizedBox(height: 12),

                  // 1.1 Review Deadline Banner
                  if (timesheet.reviewDeadline != null) ...[
                    _buildDeadlineBanner(timesheet),
                    const SizedBox(height: 14),
                  ],

                  // 2. Weekly KPI Summary Cards (2x2 Grid)
                  _buildKpiGrid(timesheet),
                  const SizedBox(height: 20),

                  // 3. Shifts Breakdown Header with Selection Actions
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
                      Row(
                        children: [
                          if (eligibleShiftsCount > 0)
                            InkWell(
                              onTap: _selectAllEligibleShifts,
                              borderRadius: BorderRadius.circular(8),
                              child: Padding(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                                child: Text(
                                  _selectedAssignmentIds.length == eligibleShiftsCount
                                      ? 'Deselect All'
                                      : 'Select All ($eligibleShiftsCount)',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.primary,
                                  ),
                                ),
                              ),
                            ),
                          const SizedBox(width: 4),
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
                    ? () {
                        _clearSelection();
                        _controller.navigateWeek(next: false);
                      }
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
                    ? () {
                        _clearSelection();
                        _controller.navigateWeek(next: true);
                      }
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

  Widget _buildDeadlineBanner(StaffWeeklyTimesheetModel timesheet) {
    final isLocked = timesheet.isLocked;
    final isPast = timesheet.isPastDeadline;
    final deadlineText = timesheet.formattedDeadline;

    final (bg, border, iconColor, icon, badgeLabel, badgeBg, badgeText, title, message) = isLocked
        ? (
            const Color(0xFFF0FDF4),
            const Color(0xFFBBF7D0),
            const Color(0xFF16A34A),
            Icons.verified_rounded,
            'REVIEWED',
            const Color(0xFFDCFCE7),
            const Color(0xFF15803D),
            'Timesheet Reviewed',
            'All shift records for this period have been reviewed and finalized by the manager for payroll calculation.',
          )
        : isPast
            ? (
                const Color(0xFFFEF2F2),
                const Color(0xFFFECACA),
                const Color(0xFFDC2626),
                Icons.timer_off_outlined,
                'EXPIRED',
                const Color(0xFFFEE2E2),
                const Color(0xFFB91C1C),
                'Review Deadline Expired',
                'The review deadline ($deadlineText) has passed. Confirmation is closed, pending manager review.',
              )
            : (
                const Color(0xFFEFF6FF),
                const Color(0xFFBFDBFE),
                const Color(0xFF2563EB),
                Icons.schedule_rounded,
                'OPEN FOR REVIEW',
                const Color(0xFFDBEAFE),
                const Color(0xFF1D4ED8),
                'Review & Adjustment Deadline',
                'Please verify your shift hours before $deadlineText. Confirmation window will close automatically once expired, pending manager review.',
              );

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              Icon(icon, size: 20, color: iconColor),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: iconColor,
                  ),
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: badgeBg,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  badgeLabel,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: badgeText,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            message,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
              height: 1.35,
            ),
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
    final isSelected = _selectedAssignmentIds.contains(shift.assignmentId);

    return InkWell(
      onTap: shift.canRequestAdjustment
          ? () => _toggleShiftSelection(shift.assignmentId)
          : null,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        margin: const EdgeInsets.only(bottom: 12),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: isSelected ? const Color(0xFFEFF6FF) : Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.border,
            width: isSelected ? 1.5 : 1.0,
          ),
          boxShadow: isSelected
              ? const [
                  BoxShadow(
                    color: Color(0x142563EB),
                    blurRadius: 8,
                    offset: Offset(0, 2),
                  ),
                ]
              : null,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Row 1: Checkbox (if eligible), Date & Status Badge
            Row(
              children: [
                if (shift.canRequestAdjustment) ...[
                  SizedBox(
                    width: 24,
                    height: 24,
                    child: Checkbox(
                      value: isSelected,
                      activeColor: AppColors.primary,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
                      onChanged: (_) => _toggleShiftSelection(shift.assignmentId),
                    ),
                  ),
                  const SizedBox(width: 8),
                ],
                Expanded(
                  child: Text(
                    shift.dateDisplay,
                    style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                  ),
                ),
                _buildAttendanceStatusPill(shift),
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
                                color: shift.isLateExcused ? const Color(0xFFE0F2FE) : const Color(0xFFFEF3C7),
                                borderRadius: BorderRadius.circular(6),
                                border: shift.isLateExcused ? Border.all(color: const Color(0xFFBAE6FD)) : null,
                              ),
                              child: Text(
                                shift.isLateExcused ? 'Late ${shift.lateDisplay} (Excused)' : 'Late ${shift.lateDisplay}',
                                style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.bold,
                                  color: shift.isLateExcused ? const Color(0xFF0284C7) : const Color(0xFFB45309),
                                ),
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
                                color: shift.isEarlyLeaveExcused ? const Color(0xFFE0F2FE) : const Color(0xFFFFEDD5),
                                borderRadius: BorderRadius.circular(6),
                                border: shift.isEarlyLeaveExcused ? Border.all(color: const Color(0xFFBAE6FD)) : null,
                              ),
                              child: Text(
                                shift.isEarlyLeaveExcused ? 'Early ${shift.earlyLeaveDisplay} (Excused)' : 'Early ${shift.earlyLeaveDisplay}',
                                style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.bold,
                                  color: shift.isEarlyLeaveExcused ? const Color(0xFF0284C7) : const Color(0xFFC2410C),
                                ),
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
            if (shift.isLateExcused || shift.isEarlyLeaveExcused) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
                decoration: BoxDecoration(
                  color: const Color(0xFFF0F9FF),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFFBAE6FD)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.check_circle_rounded, size: 14, color: Color(0xFF0284C7)),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        shift.isLateExcused && shift.isEarlyLeaveExcused
                            ? 'Late arrival & early leave excused by approved request (No penalty)'
                            : shift.isLateExcused
                                ? 'Late arrival excused by approved request (No penalty)'
                                : 'Early leave excused by approved request (No penalty)',
                        style: const TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          color: Color(0xFF0369A1),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 10),

            // Row 4: Compiled Work Time
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: isSelected ? Colors.white : const Color(0xFFF8FAFC),
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

            // Penalties History (if any)
            if (shift.penalties.isNotEmpty) ...[
              const SizedBox(height: 10),
              _buildPenaltySection(shift),
            ],

            // Row 5: Adjustment Request Banner (if any)
            if (shift.hasAdjustmentRequest) ...[
              const SizedBox(height: 10),
              _buildAdjustmentStatusBanner(shift),
            ],

            // Row 6: Individual "Request Adjustment" shortcut button if eligible
            if (shift.canRequestAdjustment && !isSelected) ...[
              const SizedBox(height: 8),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: () => _handleOpenAdjustmentSheet(singleShift: shift),
                  icon: const Icon(Icons.edit_note_rounded, size: 16),
                  label: const Text('Request Adjustment'),
                  style: TextButton.styleFrom(
                    visualDensity: VisualDensity.compact,
                    foregroundColor: const Color(0xFFD97706),
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildAdjustmentStatusBanner(StaffShiftAttendanceModel shift) {
    final status = shift.adjustmentStatus?.toUpperCase();

    if (status == 'PENDING') {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFFFFBEB),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFFDE68A)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Row(
                  children: [
                    Icon(Icons.hourglass_top_rounded, size: 16, color: Color(0xFFD97706)),
                    SizedBox(width: 6),
                    Text(
                      'Adjustment Pending Review',
                      style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFFB45309)),
                    ),
                  ],
                ),
                if (shift.canCancelAdjustment && shift.adjustmentRequestId != null)
                  Tooltip(
                    message: 'Cancel Request',
                    child: InkWell(
                      onTap: () => _handleCancelAdjustment(shift),
                      borderRadius: BorderRadius.circular(16),
                      child: Container(
                        padding: const EdgeInsets.all(4),
                        decoration: const BoxDecoration(
                          color: Color(0xFFFEE2E2),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Icons.close_rounded,
                          size: 15,
                          color: AppColors.error,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
            if (shift.adjustmentReason != null && shift.adjustmentReason!.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                'Reason: "${shift.adjustmentReason}"',
                style: const TextStyle(fontSize: 11, color: Color(0xFF92400E)),
              ),
            ],
          ],
        ),
      );
    }

    if (status == 'CANCELLED') {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFF1F5F9),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFE2E8F0)),
        ),
        child: const Row(
          children: [
            Icon(Icons.cancel_outlined, size: 16, color: AppColors.textMuted),
            SizedBox(width: 6),
            Text(
              'Adjustment Request Cancelled',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textSecondary),
            ),
          ],
        ),
      );
    }

    if (status == 'REJECTED') {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFFEF2F2),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFFECACA)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Row(
              children: [
                Icon(Icons.cancel_outlined, size: 16, color: AppColors.error),
                SizedBox(width: 6),
                Text(
                  'Adjustment Rejected by Manager',
                  style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.error),
                ),
              ],
            ),
            if (shift.managerNote != null && shift.managerNote!.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text(
                'Manager Note: "${shift.managerNote}"',
                style: const TextStyle(fontSize: 11, color: Color(0xFF991B1B)),
              ),
            ],
            const SizedBox(height: 4),
            Text(
              shift.canRequestAdjustment
                  ? 'The rejected time is locked, but you can still request adjustment for the other time.'
                  : 'All adjustments for this shift have been resolved.',
              style: TextStyle(
                fontSize: 10,
                fontStyle: FontStyle.italic,
                color: shift.canRequestAdjustment ? const Color(0xFFD97706) : const Color(0xFFDC2626),
              ),
            ),
          ],
        ),
      );
    }

    if (status == 'APPROVED') {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFF0FDF4),
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: const Color(0xFFBBF7D0)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.check_circle_rounded, size: 16, color: Color(0xFF16A34A)),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    shift.managerNote != null && shift.managerNote!.isNotEmpty
                        ? 'Adjustment Approved: "${shift.managerNote}"'
                        : 'Adjustment Approved by Manager',
                    style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF15803D)),
                  ),
                ),
              ],
            ),
            if (shift.canRequestAdjustment) ...[
              const SizedBox(height: 4),
              const Text(
                'You can still submit an adjustment for the other time if needed.',
                style: TextStyle(fontSize: 10, fontStyle: FontStyle.italic, color: Color(0xFF16A34A)),
              ),
            ],
          ],
        ),
      );
    }

    return const SizedBox.shrink();
  }

  Widget _buildPenaltySection(StaffShiftAttendanceModel shift) {
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.gavel_rounded, size: 14, color: AppColors.textSecondary),
              const SizedBox(width: 6),
              const Text(
                'Violation & Penalty History',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const Spacer(),
              if (shift.activePenalties.isNotEmpty)
                Text(
                  '-${shift.formattedTotalActivePenaltyAmount}',
                  style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFFDC2626),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 6),
          ...shift.penalties.map((p) {
            final isApplied = p.isApplied;
            return Container(
              margin: const EdgeInsets.only(top: 4),
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 5),
              decoration: BoxDecoration(
                color: isApplied ? const Color(0xFFFEF2F2) : const Color(0xFFF1F5F9),
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: isApplied ? const Color(0xFFFECACA) : const Color(0xFFE2E8F0),
                ),
              ),
              child: Row(
                children: [
                  Icon(
                    isApplied ? Icons.warning_amber_rounded : Icons.check_circle_outline_rounded,
                    size: 13,
                    color: isApplied ? const Color(0xFFDC2626) : AppColors.textMuted,
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      '${p.violationTypeDisplay} (${p.violationMinutes}m): -${p.formattedPenaltyAmount}',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w500,
                        color: isApplied ? const Color(0xFFB91C1C) : AppColors.textMuted,
                        decoration: isApplied ? null : TextDecoration.lineThrough,
                      ),
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
                    decoration: BoxDecoration(
                      color: isApplied ? const Color(0xFFFEE2E2) : const Color(0xFFE0F2FE),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: Text(
                      isApplied ? 'Applied' : 'Excused',
                      style: TextStyle(
                        fontSize: 9.5,
                        fontWeight: FontWeight.bold,
                        color: isApplied ? const Color(0xFF991B1B) : const Color(0xFF0284C7),
                      ),
                    ),
                  ),
                ],
              ),
            );
          }),
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
      return _buildBadge(label: 'REVIEWED', bgColor: const Color(0xFFE0F2FE), textColor: const Color(0xFF0284C7));
    }
    if (timesheet.isConfirmed) {
      return _buildBadge(label: 'CONFIRMED', bgColor: const Color(0xFFD1FAE5), textColor: const Color(0xFF059669));
    }
    if (timesheet.isAdjustmentPending || timesheet.pendingAdjustmentCount > 0) {
      return _buildBadge(label: 'ADJUSTMENT PENDING', bgColor: const Color(0xFFEDE9FE), textColor: const Color(0xFF7C3AED));
    }
    if (timesheet.isNoResponse) {
      return _buildBadge(label: 'NO RESPONSE', bgColor: const Color(0xFFF1F5F9), textColor: const Color(0xFF475569));
    }
    if (timesheet.status.toUpperCase() == 'GENERATED') {
      return _buildBadge(label: 'PENDING REVIEW', bgColor: const Color(0xFFFEF3C7), textColor: const Color(0xFFD97706));
    }
    return _buildBadge(label: 'NOT GENERATED', bgColor: const Color(0xFFF1F5F9), textColor: const Color(0xFF64748B));
  }

  Widget _buildAttendanceStatusPill(StaffShiftAttendanceModel shift) {
    final status = shift.attendanceStatus.toUpperCase();

    if (shift.isPresentExcused) {
      return _buildExcusedPill('PRESENT (EXCUSED)');
    }

    if (status == 'LATE' && shift.isLateExcused) {
      return _buildExcusedPill('LATE (EXCUSED)');
    }

    if ((status == 'EARLY_LEAVE' || status == 'EARLY') && shift.isEarlyLeaveExcused) {
      return _buildExcusedPill('EARLY (EXCUSED)');
    }

    if (status == 'LATE_AND_EARLY_LEAVE') {
      if (shift.isLateExcused && shift.isEarlyLeaveExcused) {
        return _buildExcusedPill('LATE & EARLY (EXCUSED)');
      } else if (shift.isLateExcused) {
        return _buildPillWithIcon('LATE (EXCUSED) & EARLY', const Color(0xFFFFEDD5), const Color(0xFFC2410C), Icons.warning_rounded);
      } else if (shift.isEarlyLeaveExcused) {
        return _buildPillWithIcon('LATE & EARLY (EXCUSED)', const Color(0xFFFFEDD5), const Color(0xFFC2410C), Icons.warning_rounded);
      }
    }

    Color bg = const Color(0xFFF1F5F9);
    Color text = const Color(0xFF64748B);

    if (status == 'COMPLETED' || status == 'PRESENT') {
      bg = const Color(0xFFDBEAFE);
      text = const Color(0xFF2563EB);
    } else if (status == 'ABSENT') {
      bg = const Color(0xFFFEE2E2);
      text = const Color(0xFFDC2626);
    } else if (status == 'LATE') {
      bg = const Color(0xFFFEF3C7);
      text = const Color(0xFFD97706);
    } else if (status == 'EARLY_LEAVE' || status == 'LATE_AND_EARLY_LEAVE') {
      bg = const Color(0xFFFFEDD5);
      text = const Color(0xFFC2410C);
    } else if (status == 'LEAVE') {
      bg = const Color(0xFFE0E7FF);
      text = const Color(0xFF4338CA);
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(
        status == 'LATE_AND_EARLY_LEAVE' ? 'LATE & EARLY' : status,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: text),
      ),
    );
  }

  Widget _buildExcusedPill(String label) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: const Color(0xFFE0F2FE),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: const Color(0xFFBAE6FD)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.verified_rounded, size: 12, color: Color(0xFF0284C7)),
          const SizedBox(width: 4),
          Text(
            label,
            style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF0369A1)),
          ),
        ],
      ),
    );
  }

  Widget _buildPillWithIcon(String label, Color bg, Color text, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(6),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: text),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: text),
          ),
        ],
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
    // Mode 1: Shifts are selected for Adjustment
    if (_selectedAssignmentIds.isNotEmpty) {
      return Container(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
        decoration: const BoxDecoration(
          color: Colors.white,
          border: Border(top: BorderSide(color: AppColors.border)),
          boxShadow: [
            BoxShadow(
              color: Color(0x14000000),
              blurRadius: 10,
              offset: Offset(0, -4),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  '${_selectedAssignmentIds.length} shift(s) selected',
                  style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: AppColors.textPrimary),
                ),
                TextButton(
                  onPressed: _clearSelection,
                  style: TextButton.styleFrom(
                    visualDensity: VisualDensity.compact,
                    foregroundColor: AppColors.textSecondary,
                  ),
                  child: const Text('Deselect All'),
                ),
              ],
            ),
            const SizedBox(height: 8),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: ElevatedButton.icon(
                onPressed: () => _handleOpenAdjustmentSheet(),
                icon: const Icon(Icons.edit_calendar_rounded, size: 20),
                label: Text(
                  'Request Adjustment (${_selectedAssignmentIds.length})',
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFD97706),
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                  elevation: 0,
                ),
              ),
            ),
          ],
        ),
      );
    }

    // Mode 2: No shift selected -> Standard Status & Confirm Action
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

    if (timesheet.isAdjustmentPending || timesheet.pendingAdjustmentCount > 0) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(
          color: Color(0xFFF5F3FF),
          border: Border(top: BorderSide(color: Color(0xFFDDD6FE))),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.hourglass_top_rounded, color: Color(0xFF7C3AED), size: 18),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                timesheet.pendingAdjustmentCount > 0
                    ? 'Waiting for manager to review ${timesheet.pendingAdjustmentCount} pending adjustment request(s).'
                    : 'Adjustment request submitted. Awaiting manager review.',
                style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: Color(0xFF6D28D9)),
              ),
            ),
          ],
        ),
      );
    }

    if (timesheet.isConfirmed) {
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

    if (timesheet.isNoResponse) {
      return Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(
          color: Color(0xFFF8FAFC),
          border: Border(top: BorderSide(color: Color(0xFFE2E8F0))),
        ),
        child: const Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.timer_off_outlined, color: Color(0xFF64748B), size: 18),
            SizedBox(width: 8),
            Text(
              'Review deadline expired (No response).',
              style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Color(0xFF475569)),
            ),
          ],
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
