import 'package:flutter/material.dart';
import '../../../models/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_info_row.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class ShiftDetailContent extends StatelessWidget {
  final AvailableShiftModel shift;
  const ShiftDetailContent({super.key, required this.shift});
  @override
  Widget build(BuildContext context) {
    final own = shift.myRegistration;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _DetailSection(
          title: shift.shiftName,
          children: [
            Text(
              '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
              style: const TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w700,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 6,
              children: [
                StatusBadge.neutral(
                  text: ShiftDisplayFormat.shiftStatus(shift.status),
                ),
                StatusBadge.neutral(
                  text: ShiftDisplayFormat.weekStatus(shift.scheduleWeekStatus),
                ),
              ],
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
              icon: Icons.timer_outlined,
              label: 'Scheduled duration',
              value: ShiftDisplayFormat.duration(
                shift.scheduledStartAt,
                shift.scheduledEndAt,
              ),
            ),
            AppInfoRow(
              icon: Icons.location_on_outlined,
              label: 'Branch',
              value: shift.branchName,
            ),
            AppInfoRow(
              icon: Icons.schedule_outlined,
              label: 'Registration deadline · UTC+7',
              value: ShiftDisplayFormat.deadline(shift.registrationDeadline),
            ),
          ],
        ),
        const SizedBox(height: 16),
        _DetailSection(
          title: 'Your assignment',
          children: [
            if (own == null)
              const Text(
                'You are not currently assigned to this shift.',
                style: TextStyle(color: AppColors.textSecondary, height: 1.5),
              )
            else ...[
              AppInfoRow(
                icon: Icons.check_circle_outline,
                label: 'Assignment status',
                value: ShiftDisplayFormat.assignmentStatus(own.status),
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
              const SizedBox(height: 8),
              Text(
                shift.canCancelRegistration
                    ? 'This registration is currently eligible for cancellation. Manage it in My Work Schedule.'
                    : shift.cancellationBlockedReason ??
                          'Cancellation is unavailable for this assignment.',
                style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textSecondary,
                  height: 1.5,
                ),
              ),
            ],
          ],
        ),
        const SizedBox(height: 16),
        _DetailSection(
          title: 'Staffing requirements',
          children: [
            if (shift.employeeRequirements.isEmpty)
              const Text(
                'No staffing requirements provided.',
                style: TextStyle(color: AppColors.textSecondary),
              ),
            for (final requirement in shift.employeeRequirements)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      requirement.employeeTypeName,
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${requirement.assignedCount} of ${requirement.requiredCount} assigned · ${requirement.remainingCount} open',
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
        const SizedBox(height: 16),
        _DetailSection(
          title: 'Shift note',
          children: [
            Text(
              shift.note?.trim().isNotEmpty == true
                  ? shift.note!
                  : 'No additional notes for this shift.',
              style: const TextStyle(
                color: AppColors.textSecondary,
                height: 1.5,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

class _DetailSection extends StatelessWidget {
  final String title;
  final List<Widget> children;
  const _DetailSection({required this.title, required this.children});
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: const TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 12),
          ...children,
        ],
      ),
    ),
  );
}
