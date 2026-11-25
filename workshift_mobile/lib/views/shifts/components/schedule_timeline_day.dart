import 'package:flutter/material.dart';
import '../../../models/shifts/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class ScheduleTimelineDay extends StatelessWidget {
  final DateTime date;
  final bool selected;
  final List<AvailableShiftModel> shifts;
  final String emptyLabel;
  final ValueChanged<AvailableShiftModel> onDetails;
  final ValueChanged<AvailableShiftModel> onCancel;
  final bool Function(AvailableShiftModel) canCancel;
  final String? Function(AvailableShiftModel) blockedReason;
  final int? processingId;

  const ScheduleTimelineDay({
    super.key,
    required this.date,
    required this.selected,
    required this.shifts,
    required this.emptyLabel,
    required this.onDetails,
    required this.onCancel,
    required this.canCancel,
    required this.blockedReason,
    this.processingId,
  });

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final isToday =
        date.year == now.year && date.month == now.month && date.day == now.day;

    return Container(
      decoration: const BoxDecoration(
        border: Border(top: BorderSide(color: AppColors.borderLight)),
      ),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Left Date Column
            Container(
              width: 72,
              padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 6),
              decoration: BoxDecoration(
                color: selected
                    ? AppColors.primaryLight.withValues(alpha: 0.7)
                    : isToday
                    ? const Color(0xFFF0FDF4)
                    : AppColors.surface,
                border: const Border(
                  right: BorderSide(color: AppColors.borderLight),
                ),
              ),
              child: Column(
                children: [
                  if (isToday)
                    Container(
                      margin: const EdgeInsets.only(bottom: 4),
                      padding: const EdgeInsets.symmetric(
                        horizontal: 6,
                        vertical: 1.5,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: const Text(
                        'TODAY',
                        style: TextStyle(
                          fontSize: 8.5,
                          fontWeight: FontWeight.w800,
                          color: Colors.white,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ),
                  Text(
                    '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: selected
                          ? AppColors.primaryDark
                          : isToday
                          ? AppColors.primary
                          : AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    ShiftDisplayFormat.weekday(date),
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: isToday ? FontWeight.w600 : FontWeight.normal,
                      color: isToday
                          ? AppColors.primary
                          : AppColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),

            // Right Shifts Content
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: shifts.isEmpty
                    ? Padding(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        child: Text(
                          emptyLabel,
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      )
                    : Column(
                        children: [
                          for (final shift in shifts)
                            Padding(
                              padding: const EdgeInsets.only(bottom: 12),
                              child: _TimelineShift(
                                shift: shift,
                                canCancel: canCancel(shift),
                                reason: blockedReason(shift),
                                processing: processingId == shift.shiftId,
                                onDetails: () => onDetails(shift),
                                onCancel: () => onCancel(shift),
                              ),
                            ),
                        ],
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _TimelineShift extends StatelessWidget {
  final AvailableShiftModel shift;
  final bool canCancel;
  final String? reason;
  final bool processing;
  final VoidCallback onDetails;
  final VoidCallback onCancel;

  const _TimelineShift({
    required this.shift,
    required this.canCancel,
    required this.reason,
    required this.processing,
    required this.onDetails,
    required this.onCancel,
  });

  @override
  Widget build(BuildContext context) {
    // Determine the fine-grained status of this shift
    final statusInfo = ShiftDisplayFormat.resolveWorkStatus(shift: shift);
    final accent = statusInfo.accentColor;
    final isManager =
        shift.myRegistration?.assignmentSource == 'MANAGER_ASSIGNMENT';
    final isSwap = shift.myRegistration?.assignmentSource == 'SHIFT_SWAP';

    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border.withValues(alpha: 0.6)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Left Colored Accent Line
            Container(width: 5, color: accent),

            // Card Body
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 12,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Top Row: Shift Name & Assignment Source Chip
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        Expanded(
                          child: Text(
                            shift.shiftName,
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ),
                        if (isManager || isSwap)
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 3,
                            ),
                            decoration: BoxDecoration(
                              color: isManager
                                  ? const Color(0xFFFFF7ED)
                                  : const Color(0xFFF3E8FF),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(
                                color: isManager
                                    ? const Color(0xFFFDBA74)
                                    : const Color(0xFFD8B4FE),
                                width: 0.8,
                              ),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  isManager
                                      ? Icons.admin_panel_settings_outlined
                                      : Icons.swap_horiz_rounded,
                                  size: 11,
                                  color: isManager
                                      ? const Color(0xFFC2410C)
                                      : const Color(0xFF7E22CE),
                                ),
                                const SizedBox(width: 3),
                                Text(
                                  isManager ? 'Manager' : 'Swapped',
                                  style: TextStyle(
                                    fontSize: 10.5,
                                    fontWeight: FontWeight.w600,
                                    color: isManager
                                        ? const Color(0xFFC2410C)
                                        : const Color(0xFF7E22CE),
                                  ),
                                ),
                              ],
                            ),
                          ),
                      ],
                    ),

                    const SizedBox(height: 10),

                    // Middle Row: Time Info & Branch Container
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Time Column
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                const Icon(
                                  Icons.access_time_rounded,
                                  size: 13,
                                  color: AppColors.textSecondary,
                                ),
                                const SizedBox(width: 4),
                                Text(
                                  ShiftDisplayFormat.time(
                                    shift.scheduledStartAt,
                                  ),
                                  style: const TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                    color: AppColors.textPrimary,
                                  ),
                                ),
                              ],
                            ),
                            Container(
                              width: 1.5,
                              height: 14,
                              margin: const EdgeInsets.only(left: 6, top: 2, bottom: 2),
                              color: AppColors.border,
                            ),
                            Padding(
                              padding: const EdgeInsets.only(left: 17),
                              child: Text(
                                ShiftDisplayFormat.time(shift.scheduledEndAt),
                                style: const TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w500,
                                  color: AppColors.textSecondary,
                                ),
                              ),
                            ),
                          ],
                        ),

                        const SizedBox(width: 14),

                        // Branch Box
                        Expanded(
                          child: Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 8,
                            ),
                            decoration: BoxDecoration(
                              color: const Color(0xFFF8FAFC),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(
                                color: AppColors.borderLight,
                              ),
                            ),
                            child: Row(
                              children: [
                                const Icon(
                                  Icons.storefront_outlined,
                                  size: 16,
                                  color: AppColors.primary,
                                ),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      const Text(
                                        'Branch',
                                        style: TextStyle(
                                          fontSize: 10.5,
                                          color: AppColors.textSecondary,
                                        ),
                                      ),
                                      Text(
                                        shift.branchName,
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
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
                          ),
                        ),
                      ],
                    ),

                    if (!ShiftDisplayFormat.sameDay(
                      shift.scheduledStartAt,
                      shift.scheduledEndAt,
                    ))
                      Padding(
                        padding: const EdgeInsets.only(top: 6),
                        child: Text(
                          'Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ),

                    const SizedBox(height: 10),

                    // Assignment Source info text
                    Row(
                      children: [
                        Icon(
                          isManager
                              ? Icons.admin_panel_settings_outlined
                              : Icons.person_outline,
                          size: 13,
                          color: AppColors.textSecondary,
                        ),
                        const SizedBox(width: 4),
                        Text(
                          ShiftDisplayFormat.assignmentSource(
                            shift.myRegistration?.assignmentSource ?? '',
                          ),
                          style: const TextStyle(
                            fontSize: 11.5,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ],
                    ),

                    // Absent specific notice banner
                    if (statusInfo == ShiftWorkStatus.absent)
                      Container(
                        margin: const EdgeInsets.only(top: 8),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 5,
                        ),
                        decoration: BoxDecoration(
                          color: const Color(0xFFFFF1F2),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(
                            color: const Color(0xFFFECDD3),
                            width: 0.8,
                          ),
                        ),
                        child: const Row(
                          children: [
                            Icon(
                              Icons.error_outline_rounded,
                              size: 13,
                              color: Color(0xFFE11D48),
                            ),
                            SizedBox(width: 5),
                            Expanded(
                              child: Text(
                                'Shift ended without check-in / check-out',
                                style: TextStyle(
                                  fontSize: 11,
                                  color: Color(0xFFBE123C),
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),

                    const SizedBox(height: 10),

                    // Bottom Row: Status Badge & View Details
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        StatusBadge(
                          text: statusInfo.label,
                          hasDot: true,
                          dotColor: statusInfo.accentColor,
                          backgroundColor: statusInfo.backgroundColor,
                          textColor: statusInfo.textColor,
                          icon: statusInfo.icon,
                        ),
                        InkWell(
                          onTap: onDetails,
                          borderRadius: BorderRadius.circular(8),
                          child: const Padding(
                            padding: EdgeInsets.symmetric(
                              horizontal: 6,
                              vertical: 4,
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Text(
                                  'View details',
                                  style: TextStyle(
                                    fontSize: 12.5,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.primary,
                                  ),
                                ),
                                SizedBox(width: 2),
                                Icon(
                                  Icons.chevron_right_rounded,
                                  size: 16,
                                  color: AppColors.primary,
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),

                    // Cancel Registration button (if eligible)
                    if (canCancel || processing)
                      Padding(
                        padding: const EdgeInsets.only(top: 8),
                        child: Align(
                          alignment: Alignment.centerRight,
                          child: processing
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                  ),
                                )
                              : TextButton.icon(
                                  onPressed: onCancel,
                                  icon: const Icon(
                                    Icons.event_busy_outlined,
                                    size: 15,
                                  ),
                                  label: const Text(
                                    'Cancel registration',
                                    style: TextStyle(fontSize: 12),
                                  ),
                                  style: TextButton.styleFrom(
                                    foregroundColor: AppColors.error,
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 8,
                                      vertical: 4,
                                    ),
                                    minimumSize: Size.zero,
                                    tapTargetSize:
                                        MaterialTapTargetSize.shrinkWrap,
                                  ),
                                ),
                        ),
                      ),

                    // Manager assignment note / blocked cancellation reason
                    if (!canCancel &&
                        !processing &&
                        reason != null &&
                        shift.myRegistration?.status == 'ASSIGNED')
                      Container(
                        margin: const EdgeInsets.only(top: 8),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 6,
                        ),
                        decoration: BoxDecoration(
                          color: const Color(0xFFF8FAFC),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(color: AppColors.borderLight),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Icon(
                              Icons.info_outline_rounded,
                              size: 13,
                              color: AppColors.textSecondary,
                            ),
                            const SizedBox(width: 5),
                            Expanded(
                              child: Text(
                                reason!,
                                maxLines: 2,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(
                                  fontSize: 11,
                                  color: AppColors.textSecondary,
                                  height: 1.3,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
