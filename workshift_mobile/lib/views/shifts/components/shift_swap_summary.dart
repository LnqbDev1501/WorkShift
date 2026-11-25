import 'package:flutter/material.dart';
import '../../../models/shifts/shift_swap_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class ShiftSwapStatusBadge extends StatelessWidget {
  final String status;
  const ShiftSwapStatusBadge({super.key, required this.status});
  @override
  Widget build(BuildContext context) => switch (status) {
    'PENDING' => StatusBadge.pending(text: 'Pending'),
    'ACCEPTED' => StatusBadge.success(text: 'Accepted'),
    'REJECTED' => StatusBadge.rejected(text: 'Rejected'),
    'CANCELLED' => StatusBadge.neutral(text: 'Cancelled'),
    'EXPIRED' => StatusBadge.neutral(text: 'Expired'),
    _ => StatusBadge.neutral(text: status),
  };
}

String swapAssignmentText(ShiftSwapAssignmentModel assignment) =>
    '${ShiftDisplayFormat.dayHeading(assignment.scheduledStartAt)}\n'
    '${ShiftDisplayFormat.time(assignment.scheduledStartAt)} – ${ShiftDisplayFormat.time(assignment.scheduledEndAt)}'
    '${ShiftDisplayFormat.sameDay(assignment.scheduledStartAt, assignment.scheduledEndAt) ? '' : '\nEnds ${ShiftDisplayFormat.dayHeading(assignment.scheduledEndAt)}'}';

class ShiftSwapSummary extends StatelessWidget {
  final ShiftSwapAssignmentModel give;
  final ShiftSwapAssignmentModel receive;
  final bool compact;
  const ShiftSwapSummary({
    super.key,
    required this.give,
    required this.receive,
    this.compact = false,
  });
  @override
  Widget build(BuildContext context) => Column(
    children: [
      _assignment(
        context,
        'You give',
        give,
        Icons.north_east_rounded,
        AppColors.warning,
      ),
      const SizedBox(height: 10),
      _assignment(
        context,
        'You receive',
        receive,
        Icons.south_west_rounded,
        AppColors.primary,
      ),
    ],
  );
  Widget _assignment(
    BuildContext context,
    String label,
    ShiftSwapAssignmentModel assignment,
    IconData icon,
    Color color,
  ) => Container(
    width: double.infinity,
    padding: const EdgeInsets.all(12),
    decoration: BoxDecoration(
      color: AppColors.background,
      borderRadius: BorderRadius.circular(10),
    ),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 18, color: color),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                compact ? '$label · ${assignment.shiftName}' : label,
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: color,
                ),
              ),
              if (!compact) ...[
                const SizedBox(height: 4),
                Text(
                  assignment.shiftName,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
              ],
              const SizedBox(height: 3),
              Text(
                swapAssignmentText(assignment),
                style: const TextStyle(
                  fontSize: 12,
                  height: 1.5,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}
