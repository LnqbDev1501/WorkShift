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
  Widget build(BuildContext context) => Container(
    decoration: const BoxDecoration(
      border: Border(top: BorderSide(color: AppColors.borderLight)),
    ),
    child: IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            width: 68,
            padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 4),
            decoration: BoxDecoration(
              color: selected ? AppColors.primaryLight : AppColors.surface,
              border: const Border(
                right: BorderSide(color: AppColors.borderLight),
              ),
            ),
            child: Column(
              children: [
                Text(
                  '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}',
                  style: TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                    color: selected
                        ? AppColors.primaryDark
                        : AppColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 5),
                Text(
                  ShiftDisplayFormat.weekday(date),
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
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
                            padding: const EdgeInsets.only(bottom: 10),
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
    final completed = shift.myRegistration?.status == 'COMPLETED';
    final cancelled =
        shift.status == 'CANCELLED' ||
        shift.myRegistration?.status == 'CANCELLED';
    final accent = cancelled
        ? AppColors.error
        : completed
        ? AppColors.success
        : shift.myRegistration?.assignmentSource == 'MANAGER_ASSIGNMENT'
        ? AppColors.warning
        : AppColors.primary;
    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: IntrinsicHeight(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(width: 4, color: accent),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      shift.shiftName,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 10),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Column(
                          children: [
                            Text(
                              ShiftDisplayFormat.time(shift.scheduledStartAt),
                              style: const TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                            Container(
                              width: 1,
                              height: 18,
                              margin: const EdgeInsets.symmetric(vertical: 4),
                              color: AppColors.border,
                            ),
                            Text(
                              ShiftDisplayFormat.time(shift.scheduledEndAt),
                              style: const TextStyle(
                                fontSize: 13,
                                color: AppColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Container(
                            padding: const EdgeInsets.all(10),
                            decoration: BoxDecoration(
                              color: AppColors.borderLight,
                              borderRadius: BorderRadius.circular(10),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                const Text(
                                  'Branch',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: AppColors.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  shift.branchName,
                                  style: const TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textPrimary,
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
                        padding: const EdgeInsets.only(top: 8),
                        child: Text(
                          'Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                          ),
                        ),
                      ),
                    const SizedBox(height: 10),
                    Text(
                      ShiftDisplayFormat.assignmentSource(
                        shift.myRegistration?.assignmentSource ?? '',
                      ),
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 8,
                      runSpacing: 6,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        StatusBadge(
                          text: cancelled
                              ? 'Cancelled'
                              : completed
                              ? 'Completed'
                              : 'Confirmed',
                          hasDot: true,
                          dotColor: accent,
                          backgroundColor: accent.withValues(alpha: 0.10),
                          textColor: AppColors.textPrimary,
                        ),
                        TextButton(
                          onPressed: onDetails,
                          style: TextButton.styleFrom(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                          ),
                          child: const Text(
                            'View details',
                            style: TextStyle(fontSize: 12),
                          ),
                        ),
                      ],
                    ),
                    if (canCancel || processing)
                      Align(
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
                                  size: 16,
                                ),
                                label: const Text(
                                  'Cancel registration',
                                  style: TextStyle(fontSize: 12),
                                ),
                                style: TextButton.styleFrom(
                                  foregroundColor: AppColors.error,
                                ),
                              ),
                      ),
                    if (!canCancel &&
                        !processing &&
                        reason != null &&
                        shift.myRegistration?.status == 'ASSIGNED')
                      Padding(
                        padding: const EdgeInsets.only(top: 4),
                        child: Text(
                          reason!,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.textSecondary,
                          ),
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
