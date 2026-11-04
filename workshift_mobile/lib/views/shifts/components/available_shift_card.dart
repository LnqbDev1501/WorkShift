import 'package:flutter/material.dart';

import '../../../models/shifts/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_button.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class AvailableShiftCard extends StatelessWidget {
  final AvailableShiftModel shift;
  final bool isOpen;
  final bool isClosingSoon;
  final bool isRegistering;
  final VoidCallback? onRegister;
  final VoidCallback? onViewDetails;

  const AvailableShiftCard({
    super.key,
    required this.shift,
    required this.isOpen,
    this.isClosingSoon = false,
    this.isRegistering = false,
    this.onRegister,
    this.onViewDetails,
  });

  @override
  Widget build(BuildContext context) => Card(
    margin: EdgeInsets.zero,
    elevation: 0,
    shape: RoundedRectangleBorder(
      borderRadius: BorderRadius.circular(16),
      side: const BorderSide(color: AppColors.border),
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  shift.shiftName,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              if (isOpen)
                StatusBadge.success(text: 'Open')
              else
                StatusBadge.neutral(text: 'Closed'),
            ],
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 10,
            runSpacing: 4,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(
                '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
                style: const TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                  letterSpacing: -0.5,
                  color: AppColors.textPrimary,
                ),
              ),
              Text(
                ShiftDisplayFormat.duration(
                  shift.scheduledStartAt,
                  shift.scheduledEndAt,
                ),
                style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
          ),
          if (!ShiftDisplayFormat.sameDay(
            shift.scheduledStartAt,
            shift.scheduledEndAt,
          )) ...[
            const SizedBox(height: 4),
            Text(
              'Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}',
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
              ),
            ),
          ],
          const SizedBox(height: 8),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(
                Icons.location_on_outlined,
                size: 16,
                color: AppColors.textSecondary,
              ),
              const SizedBox(width: 5),
              Expanded(
                child: Text(
                  shift.branchName,
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          const Text(
            'Open positions',
            style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 6),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              for (final requirement in shift.employeeRequirements)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 9,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: requirement.remainingCount > 0
                        ? AppColors.primaryLight
                        : AppColors.borderLight,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    '${requirement.employeeTypeName} · ${requirement.remainingCount > 0 ? '${requirement.remainingCount} left' : 'Full'}',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: requirement.remainingCount > 0
                          ? AppColors.primaryDark
                          : AppColors.textSecondary,
                    ),
                  ),
                ),
            ],
          ),
          if (shift.note?.trim().isNotEmpty == true)
            Theme(
              data: Theme.of(
                context,
              ).copyWith(dividerColor: Colors.transparent),
              child: ExpansionTile(
                key: PageStorageKey('shift-note-${shift.shiftId}'),
                tilePadding: EdgeInsets.zero,
                childrenPadding: const EdgeInsets.only(bottom: 8),
                dense: true,
                title: const Text(
                  'Shift note',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.textSecondary,
                  ),
                ),
                children: [
                  Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      shift.note!,
                      style: const TextStyle(fontSize: 13, height: 1.5),
                    ),
                  ),
                ],
              ),
            ),
          const SizedBox(height: 12),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(
                Icons.schedule_rounded,
                size: 15,
                color: isClosingSoon
                    ? AppColors.warning
                    : AppColors.textSecondary,
              ),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  '${isClosingSoon ? 'Closing soon · ' : 'Register by '}${ShiftDisplayFormat.deadline(shift.registrationDeadline)}',
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: isClosingSoon
                        ? FontWeight.w600
                        : FontWeight.normal,
                    color: isClosingSoon
                        ? AppColors.warning
                        : AppColors.textSecondary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          LayoutBuilder(
            builder: (context, constraints) {
              final register = AppButton(
                text: isOpen ? 'Register for shift' : 'Registration closed',
                height: 46,
                borderRadius: 10,
                width: double.infinity,
                isLoading: isRegistering,
                onPressed: onRegister,
              );
              final details = AppButton(
                text: 'View details',
                type: AppButtonType.outline,
                height: 46,
                borderRadius: 10,
                onPressed: onViewDetails,
              );
              if (onViewDetails == null) return register;
              if (constraints.maxWidth < 300 ||
                  MediaQuery.textScalerOf(context).scale(15) > 18) {
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [register, const SizedBox(height: 8), details],
                );
              }
              return Row(
                children: [
                  Expanded(flex: 4, child: details),
                  const SizedBox(width: 10),
                  Expanded(flex: 6, child: register),
                ],
              );
            },
          ),
        ],
      ),
    ),
  );
}
