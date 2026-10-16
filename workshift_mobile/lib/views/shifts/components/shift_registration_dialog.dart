import 'package:flutter/material.dart';

import '../../../models/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_confirmation_dialog.dart';
import 'shift_display_format.dart';

Future<bool?> showShiftRegistrationDialog(
  BuildContext context,
  AvailableShiftModel shift,
) => showDialog<bool>(
  context: context,
  builder: (_) => AppConfirmationDialog(
    title: 'Register for this shift?',
    confirmLabel: 'Confirm registration',
    content: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          shift.shiftName,
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        const SizedBox(height: 12),
        Text(ShiftDisplayFormat.dayHeading(shift.scheduledStartAt)),
        const SizedBox(height: 6),
        Text(
          '${ShiftDisplayFormat.time(shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(shift.scheduledEndAt)} (UTC+7)',
        ),
        if (!ShiftDisplayFormat.sameDay(
          shift.scheduledStartAt,
          shift.scheduledEndAt,
        ))
          Text('Ends ${ShiftDisplayFormat.dayHeading(shift.scheduledEndAt)}'),
        const SizedBox(height: 6),
        Text(shift.branchName),
        const SizedBox(height: 16),
        const Text(
          'Confirm to add this shift to your work schedule.',
          style: TextStyle(color: AppColors.textSecondary, height: 1.5),
        ),
      ],
    ),
  ),
);
