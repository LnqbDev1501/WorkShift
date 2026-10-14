import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

class AppChoiceFilters<T> extends StatelessWidget {
  final List<(T, String)> options;
  final T selected;
  final ValueChanged<T> onSelected;
  const AppChoiceFilters({
    super.key,
    required this.options,
    required this.selected,
    required this.onSelected,
  });
  @override
  Widget build(BuildContext context) => Wrap(
    spacing: 8,
    runSpacing: 6,
    children: [
      for (final (value, label) in options)
        ChoiceChip(
          label: Text(label),
          selected: value == selected,
          onSelected: (_) => onSelected(value),
          showCheckmark: false,
          selectedColor: AppColors.primary,
          backgroundColor: AppColors.surface,
          labelStyle: TextStyle(
            fontWeight: FontWeight.w600,
            color: value == selected ? Colors.white : AppColors.textSecondary,
          ),
        ),
    ],
  );
}
