import 'package:flutter/material.dart';
import '../../../models/attendance/attendance_history_model.dart';
import '../../../models/shifts/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_info_row.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class ShiftDetailContent extends StatelessWidget {
  final AvailableShiftModel shift;
  final StaffAttendanceDetailModel? attendanceDetail;

  const ShiftDetailContent({
    super.key,
    required this.shift,
    this.attendanceDetail,
  });

  @override
  Widget build(BuildContext context) {
    final own = shift.myRegistration;
    final statusInfo = ShiftDisplayFormat.resolveWorkStatus(
      shift: shift,
      attendanceStatus: attendanceDetail?.status,
    );
    final now = DateTime.now().toUtc();
    final isUpcoming = now.isBefore(shift.scheduledStartAt);
    final isOngoing =
        now.isAfter(shift.scheduledStartAt) && now.isBefore(shift.scheduledEndAt);
    final isEnded = now.isAfter(shift.scheduledEndAt);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // 1. Shift Overview Section
        _DetailSection(
          icon: Icons.access_time_filled_rounded,
          iconColor: AppColors.primary,
          title: shift.shiftName,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Expanded(
                  child: Text(
                    '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
                    style: const TextStyle(
                      fontSize: 26,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      letterSpacing: -0.5,
                    ),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: AppColors.borderLight),
                  ),
                  child: Text(
                    ShiftDisplayFormat.duration(
                      shift.scheduledStartAt,
                      shift.scheduledEndAt,
                    ),
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            _StatusInfo(
              icon: Icons.work_outline,
              label: 'Shift status',
              badge: _buildShiftStatusBadge(
                shift.status,
                isUpcoming,
                isOngoing,
                isEnded,
              ),
            ),
            const SizedBox(height: 8),
            AppInfoRow(
              icon: Icons.calendar_today_outlined,
              label: 'Starts · Vietnam time (UTC+7)',
              value:
                  '${ShiftDisplayFormat.dayHeading(shift.scheduledStartAt)} · ${ShiftDisplayFormat.time(shift.scheduledStartAt)}',
            ),
            AppInfoRow(
              icon: Icons.event_outlined,
              label: 'Ends · Vietnam time (UTC+7)',
              value:
                  '${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)} · ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
            ),
            AppInfoRow(
              icon: Icons.location_on_outlined,
              label: 'Branch',
              value: shift.branchName,
            ),
          ],
        ),

        const SizedBox(height: 16),

        // 2. Your Assignment Section
        _DetailSection(
          icon: Icons.assignment_ind_rounded,
          iconColor: const Color(0xFF4F46E5),
          title: 'Your assignment',
          children: [
            if (own == null)
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppColors.borderLight),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.info_outline_rounded,
                      size: 16,
                      color: AppColors.textSecondary,
                    ),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'You are not currently assigned to this shift.',
                        style: TextStyle(
                          color: AppColors.textSecondary,
                          fontSize: 13,
                        ),
                      ),
                    ),
                  ],
                ),
              )
            else ...[
              // Visual Status Banner
              Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: statusInfo.backgroundColor,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(
                    color: statusInfo.accentColor.withValues(alpha: 0.35),
                  ),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      statusInfo.icon,
                      size: 20,
                      color: statusInfo.accentColor,
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            statusInfo.bannerTitle,
                            style: TextStyle(
                              fontSize: 13.5,
                              fontWeight: FontWeight.w700,
                              color: statusInfo.textColor,
                            ),
                          ),
                          const SizedBox(height: 3),
                          Text(
                            statusInfo.bannerDescription,
                            style: TextStyle(
                              fontSize: 12,
                              color: statusInfo.textColor.withValues(
                                alpha: 0.85,
                              ),
                              height: 1.35,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              // Fine-grained Status Badge Row
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Icon(
                      Icons.check_circle_outline,
                      size: 20,
                      color: AppColors.textSecondary,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Your assignment status',
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.textSecondary,
                            ),
                          ),
                          const SizedBox(height: 6),
                          StatusBadge(
                            text: statusInfo.label,
                            hasDot: true,
                            dotColor: statusInfo.accentColor,
                            backgroundColor: statusInfo.backgroundColor,
                            textColor: statusInfo.textColor,
                            icon: statusInfo.icon,
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),

              AppInfoRow(
                icon: Icons.person_outline,
                label: 'Assigned through',
                value: ShiftDisplayFormat.assignmentSource(
                  own.assignmentSource,
                ),
              ),

              AppInfoRow(
                icon: Icons.event_available_outlined,
                label: 'Assigned at · UTC+7',
                value: ShiftDisplayFormat.deadline(own.assignedAt),
              ),

              if (own.cancelledAt != null)
                AppInfoRow(
                  icon: Icons.event_busy_outlined,
                  label: 'Cancelled at · UTC+7',
                  value: ShiftDisplayFormat.deadline(own.cancelledAt!),
                ),

              if (own.cancelReason?.trim().isNotEmpty == true)
                AppInfoRow(
                  icon: Icons.notes_outlined,
                  label: 'Cancellation reason',
                  value: own.cancelReason!,
                ),

              // Attendance History / Details if loaded
              if (attendanceDetail != null) ...[
                const SizedBox(height: 10),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF8FAFC),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AppColors.borderLight),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Row(
                        children: [
                          Icon(
                            Icons.fingerprint_rounded,
                            size: 16,
                            color: AppColors.primary,
                          ),
                          SizedBox(width: 6),
                          Text(
                            'Attendance Details',
                            style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                      const Divider(height: 16, color: AppColors.borderLight),
                      _AttendanceField(
                        label: 'Check-in time',
                        value: attendanceDetail!.checkInTime != null
                            ? '${ShiftDisplayFormat.calendarDate(ShiftDisplayFormat.vietnamTime(attendanceDetail!.checkInTime!))} at ${ShiftDisplayFormat.time(attendanceDetail!.checkInTime!)}'
                            : 'Not recorded',
                        icon: Icons.login_rounded,
                        isRecorded: attendanceDetail!.checkInTime != null,
                      ),
                      const SizedBox(height: 8),
                      _AttendanceField(
                        label: 'Check-out time',
                        value: attendanceDetail!.checkOutTime != null
                            ? '${ShiftDisplayFormat.calendarDate(ShiftDisplayFormat.vietnamTime(attendanceDetail!.checkOutTime!))} at ${ShiftDisplayFormat.time(attendanceDetail!.checkOutTime!)}'
                            : 'Not recorded',
                        icon: Icons.logout_rounded,
                        isRecorded: attendanceDetail!.checkOutTime != null,
                      ),
                      if (attendanceDetail!.actualWorkingMinutes > 0) ...[
                        const SizedBox(height: 8),
                        _AttendanceField(
                          label: 'Actual worked duration',
                          value:
                              '${attendanceDetail!.actualWorkingMinutes ~/ 60}h ${attendanceDetail!.actualWorkingMinutes % 60}m',
                          icon: Icons.timer_outlined,
                          isRecorded: true,
                        ),
                      ],
                      if (attendanceDetail!.totalLateMinutes > 0) ...[
                        const SizedBox(height: 8),
                        _AttendanceField(
                          label: 'Late arrival',
                          value: '${attendanceDetail!.totalLateMinutes} minutes',
                          icon: Icons.alarm_off_rounded,
                          isRecorded: true,
                          highlightColor: const Color(0xFFEA580C),
                        ),
                      ],
                      if (attendanceDetail!.totalEarlyLeaveMinutes > 0) ...[
                        const SizedBox(height: 8),
                        _AttendanceField(
                          label: 'Early leave',
                          value:
                              '${attendanceDetail!.totalEarlyLeaveMinutes} minutes',
                          icon: Icons.exit_to_app_rounded,
                          isRecorded: true,
                          highlightColor: const Color(0xFFEA580C),
                        ),
                      ],
                    ],
                  ),
                ),
              ],

              const SizedBox(height: 12),

              // Cancellation eligibility notice
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 8,
                ),
                decoration: BoxDecoration(
                  color: shift.canCancelRegistration
                      ? const Color(0xFFEFF6FF)
                      : const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: shift.canCancelRegistration
                        ? const Color(0xFFBFDBFE)
                        : AppColors.borderLight,
                  ),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      shift.canCancelRegistration
                          ? Icons.info_outline_rounded
                          : Icons.lock_outline_rounded,
                      size: 15,
                      color: shift.canCancelRegistration
                          ? AppColors.primary
                          : AppColors.textSecondary,
                    ),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        shift.canCancelRegistration
                            ? 'This registration is currently eligible for cancellation. Manage it in My Work Schedule.'
                            : shift.cancellationBlockedReason ??
                                  'Cancellation is unavailable for this assignment.',
                        style: TextStyle(
                          fontSize: 12,
                          color: shift.canCancelRegistration
                              ? AppColors.primaryDark
                              : AppColors.textSecondary,
                          height: 1.4,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),

        const SizedBox(height: 16),

        // 3. Weekly Schedule Section
        _DetailSection(
          icon: Icons.calendar_month_rounded,
          iconColor: const Color(0xFF0284C7),
          title: 'Weekly schedule',
          children: [
            _StatusInfo(
              icon: Icons.date_range_outlined,
              label: 'Weekly schedule status',
              badge: StatusBadge(
                text: ShiftDisplayFormat.weekStatus(shift.scheduleWeekStatus),
                hasDot: true,
                dotColor: shift.scheduleWeekStatus == 'OPEN_REGISTRATION'
                    ? AppColors.success
                    : AppColors.textSecondary,
                backgroundColor: shift.scheduleWeekStatus == 'OPEN_REGISTRATION'
                    ? const Color(0xFFDCFCE7)
                    : const Color(0xFFF1F5F9),
                textColor: shift.scheduleWeekStatus == 'OPEN_REGISTRATION'
                    ? const Color(0xFF15803D)
                    : AppColors.textSecondary,
              ),
            ),
            AppInfoRow(
              icon: Icons.schedule_outlined,
              label: 'Registration deadline · UTC+7',
              value: ShiftDisplayFormat.deadline(shift.registrationDeadline),
            ),
          ],
        ),

        const SizedBox(height: 16),

        // 4. Staffing Requirements Section
        _DetailSection(
          icon: Icons.people_alt_rounded,
          iconColor: const Color(0xFFD97706),
          title: 'Staffing requirements',
          children: [
            if (shift.employeeRequirements.isEmpty)
              const Text(
                'No staffing requirements provided.',
                style: TextStyle(color: AppColors.textSecondary),
              ),
            for (final requirement in shift.employeeRequirements)
              Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: AppColors.borderLight),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          requirement.employeeTypeName,
                          style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 13.5,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 6,
                            vertical: 2,
                          ),
                          decoration: BoxDecoration(
                            color: requirement.remainingCount > 0
                                ? const Color(0xFFFEF3C7)
                                : const Color(0xFFDCFCE7),
                            borderRadius: BorderRadius.circular(6),
                          ),
                          child: Text(
                            requirement.remainingCount > 0
                                ? '${requirement.remainingCount} open'
                                : 'Full',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: requirement.remainingCount > 0
                                  ? const Color(0xFFB45309)
                                  : const Color(0xFF15803D),
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      '${requirement.assignedCount} of ${requirement.requiredCount} assigned',
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),

        const SizedBox(height: 16),

        // 5. Shift Note Section
        _DetailSection(
          icon: Icons.description_rounded,
          iconColor: const Color(0xFF64748B),
          title: 'Shift note',
          children: [
            Text(
              shift.note?.trim().isNotEmpty == true
                  ? shift.note!
                  : 'No additional notes for this shift.',
              style: const TextStyle(
                color: AppColors.textSecondary,
                height: 1.5,
                fontSize: 13,
              ),
            ),
          ],
        ),
      ],
    );
  }

  static Widget _buildShiftStatusBadge(
    String status,
    bool isUpcoming,
    bool isOngoing,
    bool isEnded,
  ) {
    if (status.toUpperCase() == 'CANCELLED') {
      return StatusBadge.rejected(text: 'Cancelled');
    }
    if (status.toUpperCase() == 'COMPLETED') {
      return StatusBadge.success(text: 'Completed');
    }
    if (isOngoing) {
      return StatusBadge(
        text: 'In progress',
        hasDot: true,
        dotColor: const Color(0xFFF59E0B),
        backgroundColor: const Color(0xFFFEF3C7),
        textColor: const Color(0xFFB45309),
        icon: Icons.timelapse_rounded,
      );
    }
    if (isEnded) {
      return StatusBadge(
        text: 'Ended',
        hasDot: true,
        dotColor: const Color(0xFF64748B),
        backgroundColor: const Color(0xFFF1F5F9),
        textColor: const Color(0xFF475569),
        icon: Icons.history_rounded,
      );
    }
    return StatusBadge(
      text: 'Scheduled',
      hasDot: true,
      dotColor: AppColors.primary,
      backgroundColor: AppColors.primaryLight,
      textColor: AppColors.primaryDark,
      icon: Icons.schedule_rounded,
    );
  }
}

class _AttendanceField extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final bool isRecorded;
  final Color? highlightColor;

  const _AttendanceField({
    required this.label,
    required this.value,
    required this.icon,
    required this.isRecorded,
    this.highlightColor,
  });

  @override
  Widget build(BuildContext context) {
    final color = highlightColor ??
        (isRecorded ? AppColors.textPrimary : AppColors.textSecondary);
    return Row(
      children: [
        Icon(
          icon,
          size: 15,
          color: highlightColor ??
              (isRecorded ? AppColors.primary : AppColors.textSecondary),
        ),
        const SizedBox(width: 8),
        Text(
          '$label: ',
          style: const TextStyle(
            fontSize: 12,
            color: AppColors.textSecondary,
          ),
        ),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 12.5,
              fontWeight: isRecorded ? FontWeight.w600 : FontWeight.normal,
              color: color,
            ),
          ),
        ),
      ],
    );
  }
}

class _StatusInfo extends StatelessWidget {
  final IconData icon;
  final String label;
  final Widget badge;

  const _StatusInfo({
    required this.icon,
    required this.label,
    required this.badge,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: AppColors.textSecondary),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 6),
                badge,
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _DetailSection extends StatelessWidget {
  final IconData icon;
  final Color iconColor;
  final String title;
  final List<Widget> children;

  const _DetailSection({
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.children,
  });

  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: AppColors.border.withValues(alpha: 0.7)),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withValues(alpha: 0.03),
          blurRadius: 8,
          offset: const Offset(0, 2),
        ),
      ],
    ),
    padding: const EdgeInsets.all(16),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Container(
              padding: const EdgeInsets.all(6),
              decoration: BoxDecoration(
                color: iconColor.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, size: 17, color: iconColor),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                title,
                style: const TextStyle(
                  fontSize: 16.5,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        ...children,
      ],
    ),
  );
}
