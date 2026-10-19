import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

class AppConfirmationDialog extends StatelessWidget {
  final String title;
  final Widget content;
  final String confirmLabel;
  final String cancelLabel;
  final bool isDestructive;
  final VoidCallback? onConfirm;
  final VoidCallback? onCancel;

  const AppConfirmationDialog({
    super.key,
    required this.title,
    required this.content,
    required this.confirmLabel,
    this.cancelLabel = 'Back',
    this.isDestructive = false,
    this.onConfirm,
    this.onCancel,
  });

  @override
  Widget build(BuildContext context) => AlertDialog(
    scrollable: true,
    title: Text(title),
    content: content,
    actions: [
      TextButton(
        onPressed: onCancel ?? () => Navigator.pop(context, false),
        child: Text(cancelLabel),
      ),
      FilledButton(
        onPressed: onConfirm ?? () => Navigator.pop(context, true),
        style: isDestructive
            ? FilledButton.styleFrom(
                backgroundColor: AppColors.error,
                foregroundColor: AppColors.surface,
              )
            : null,
        child: Text(confirmLabel),
      ),
    ],
  );
}
