import 'package:flutter/material.dart';
import '../../../models/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_button.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class RegisteredShiftCard extends StatelessWidget {
  final AvailableShiftModel shift;
  final String? blockedReason;
  final bool isProcessing;
  final VoidCallback? onCancel;
  final VoidCallback? onViewDetails;
  const RegisteredShiftCard({
    super.key,
    required this.shift,
    required this.blockedReason,
    required this.isProcessing,
    this.onCancel,
    this.onViewDetails,
  });
  @override
  Widget build(BuildContext context) {
    final source = switch (shift.myRegistration?.assignmentSource) {
      'SELF_REGISTRATION' => 'Self-registered',
      'MANAGER_ASSIGNMENT' => 'Assigned by manager',
      'SHIFT_SWAP' => 'Assigned through swap',
      _ => 'Assigned shift',
    };
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              shift.shiftName,
              style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
            ),
            const SizedBox(height: 10),
            Text(
              '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
              style: const TextStyle(
                fontWeight: FontWeight.w700,
                fontSize: 25,
                color: AppColors.textPrimary,
              ),
            ),
            if (!ShiftDisplayFormat.sameDay(
              shift.scheduledStartAt,
              shift.scheduledEndAt,
            ))
              Text(
                'Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}',
                style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
            const SizedBox(height: 8),
            Text(
              shift.branchName,
              style: const TextStyle(color: AppColors.textSecondary),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 6,
              children: [
                StatusBadge.neutral(text: source),
                shift.status == 'CANCELLED'
                    ? StatusBadge.neutral(text: 'Shift cancelled')
                    : shift.myRegistration?.status == 'COMPLETED'
                    ? StatusBadge.neutral(text: 'Completed')
                    : StatusBadge.success(text: 'Confirmed'),
              ],
            ),
            const SizedBox(height: 14),
            if (onViewDetails != null)
              AppButton(
                text: 'View details',
                type: AppButtonType.text,
                icon: Icons.arrow_forward_rounded,
                onPressed: onViewDetails,
              ),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: blockedReason == null
                    ? AppColors.successBg
                    : AppColors.borderLight,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    blockedReason == null
                        ? Icons.check_circle_outline
                        : Icons.lock_outline,
                    size: 18,
                    color: blockedReason == null
                        ? AppColors.textPrimary
                        : AppColors.textSecondary,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      blockedReason ??
                          'Can cancel until ${ShiftDisplayFormat.deadline(shift.registrationDeadline)}',
                      style: const TextStyle(
                        fontSize: 13,
                        height: 1.4,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            AppButton(
              text: blockedReason == null
                  ? 'Cancel registration'
                  : 'Cancellation unavailable',
              type: AppButtonType.dangerOutline,
              icon: Icons.event_busy_outlined,
              width: double.infinity,
              isLoading: isProcessing,
              onPressed: onCancel,
            ),
          ],
        ),
      ),
    );
  }
}
