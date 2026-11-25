import 'package:flutter/material.dart';

import '../../../models/shifts/available_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_confirmation_dialog.dart';
import 'shift_display_format.dart';

Future<String?> showCancelShiftDialog(
  BuildContext context,
  AvailableShiftModel shift, {
  required String? Function(String?) validateReason,
  String initialReason = '',
}) => showDialog<String>(
  context: context,
  builder: (_) => _CancelShiftDialog(
    shift: shift,
    validateReason: validateReason,
    initialReason: initialReason,
  ),
);

class _CancelShiftDialog extends StatefulWidget {
  final AvailableShiftModel shift;
  final String? Function(String?) validateReason;
  final String initialReason;
  const _CancelShiftDialog({
    required this.shift,
    required this.validateReason,
    required this.initialReason,
  });
  @override
  State<_CancelShiftDialog> createState() => _CancelShiftDialogState();
}

class _CancelShiftDialogState extends State<_CancelShiftDialog> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _reason = TextEditingController(
    text: widget.initialReason,
  );
  @override
  void dispose() {
    _reason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AppConfirmationDialog(
    title: 'Cancel this registration?',
    confirmLabel: 'Cancel registration',
    cancelLabel: 'Keep shift',
    isDestructive: true,
    onCancel: () => Navigator.pop(context),
    onConfirm: () {
      if (_formKey.currentState!.validate()) {
        Navigator.pop(context, _reason.text.trim());
      }
    },
    content: Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            widget.shift.shiftName,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 8),
          Text(ShiftDisplayFormat.dayHeading(widget.shift.scheduledStartAt)),
          Text(
            '${ShiftDisplayFormat.time(widget.shift.scheduledStartAt)} – ${ShiftDisplayFormat.time(widget.shift.scheduledEndAt)} (UTC+7)',
          ),
          if (!ShiftDisplayFormat.sameDay(
            widget.shift.scheduledStartAt,
            widget.shift.scheduledEndAt,
          ))
            Text(
              'Ends ${ShiftDisplayFormat.dayHeading(widget.shift.scheduledEndAt)}',
            ),
          Text(widget.shift.branchName),
          const SizedBox(height: 16),
          const Text(
            'Your place will be released. Any pending swap requests linked to this shift will also be cancelled.',
            style: TextStyle(color: AppColors.textSecondary, height: 1.5),
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _reason,
            validator: widget.validateReason,
            autovalidateMode: AutovalidateMode.onUserInteraction,
            minLines: 2,
            maxLines: 4,
            maxLength: 500,
            decoration: const InputDecoration(
              labelText: 'Cancellation reason',
              hintText: 'Why can’t you attend this shift?',
            ),
          ),
        ],
      ),
    ),
  );
}
