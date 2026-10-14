import 'package:flutter/material.dart';

import '../../../models/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/status_badge.dart';
import 'shift_display_format.dart';

class AvailableShiftCard extends StatelessWidget {
  final AvailableShiftModel shift;
  final bool isOpen;
  final bool isClosingSoon;

  const AvailableShiftCard({
    super.key,
    required this.shift,
    required this.isOpen,
    this.isClosingSoon = false,
  });

  @override
  Widget build(BuildContext context) {
    final crossesMidnight = !ShiftDisplayFormat.sameDay(
      shift.scheduledStartAt,
      shift.scheduledEndAt,
    );
    return Card(
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
                      fontWeight: FontWeight.w600,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                isOpen
                    ? StatusBadge.success(text: 'Open')
                    : StatusBadge.neutral(text: 'Closed'),
              ],
            ),
            const SizedBox(height: 10),
            Wrap(
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: 12,
              runSpacing: 6,
              children: [
                Text(
                  '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)}',
                  style: const TextStyle(
                    fontSize: 25,
                    fontWeight: FontWeight.w700,
                    letterSpacing: -0.5,
                    color: AppColors.textPrimary,
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 4,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.borderLight,
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    ShiftDisplayFormat.duration(
                      shift.scheduledStartAt,
                      shift.scheduledEndAt,
                    ),
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),
            if (crossesMidnight) ...[
              const SizedBox(height: 4),
              Text(
                'Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}',
                style: const TextStyle(
                  fontSize: 12,
                  color: AppColors.textSecondary,
                ),
              ),
            ],
            const SizedBox(height: 10),
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(
                  Icons.location_on_outlined,
                  size: 17,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: 5),
                Expanded(
                  child: Text(
                    shift.branchName,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            const Text(
              'Open spots',
              style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
            ),
            const SizedBox(height: 8),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: shift.employeeRequirements.map((requirement) {
                final remaining = requirement.remainingCount;
                return Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 8,
                  ),
                  decoration: BoxDecoration(
                    color: remaining > 0
                        ? AppColors.primaryLight
                        : AppColors.borderLight,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    '${requirement.employeeTypeName} · ${remaining > 0 ? '$remaining left' : 'Full'}',
                    style: TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w600,
                      color: remaining > 0
                          ? AppColors.primaryDark
                          : AppColors.textSecondary,
                    ),
                  ),
                );
              }).toList(),
            ),
            if (shift.note?.trim().isNotEmpty == true)
              Theme(
                data: Theme.of(context)
                    .copyWith(dividerColor: Colors.transparent),
                child: ExpansionTile(
                  key: PageStorageKey('shift-note-${shift.shiftId}'),
                  tilePadding: EdgeInsets.zero,
                  childrenPadding: const EdgeInsets.only(bottom: 12),
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
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textSecondary,
                          height: 1.5,
                        ),
                      ),
                    ),
                  ],
                ),
              )
            else
              const SizedBox(height: 14),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 10),
              decoration: BoxDecoration(
                color: isClosingSoon
                    ? AppColors.warningBg
                    : AppColors.borderLight,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.schedule_rounded,
                    size: 16,
                    color: isClosingSoon
                        ? AppColors.warning
                        : AppColors.textSecondary,
                  ),
                  const SizedBox(width: 7),
                  Expanded(
                    child: Text(
                      '${isClosingSoon ? 'Closing soon · ' : 'Register by '}${ShiftDisplayFormat.deadline(shift.registrationDeadline)}',
                      style: TextStyle(
                        fontSize: 12,
                        color: isClosingSoon
                            ? AppColors.warning
                            : AppColors.textSecondary,
                        fontWeight: isClosingSoon
                            ? FontWeight.w600
                            : FontWeight.normal,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
