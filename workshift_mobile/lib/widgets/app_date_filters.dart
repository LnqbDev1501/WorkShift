import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Shared date-range controls; controllers own the actual period and date logic.
class AppDateFilters<T> extends StatelessWidget {
  final List<(T, String)> options;
  final T selected;
  final bool isCustom;
  final String dateLabel;
  final ValueChanged<T> onSelected;
  final VoidCallback onChooseDates;
  final VoidCallback onClearDates;

  const AppDateFilters({
    super.key,
    required this.options,
    required this.selected,
    required this.isCustom,
    required this.dateLabel,
    required this.onSelected,
    required this.onChooseDates,
    required this.onClearDates,
  });

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Wrap(
        spacing: 8,
        runSpacing: 6,
        children: [
          for (final (value, label) in options)
            ChoiceChip(
              label: Text(label),
              selected: selected == value,
              onSelected: (_) => onSelected(value),
              showCheckmark: false,
              selectedColor: AppColors.primary,
              backgroundColor: AppColors.borderLight,
              side: BorderSide(
                color: selected == value
                    ? AppColors.primary
                    : Colors.transparent,
              ),
              labelStyle: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: selected == value
                    ? AppColors.surface
                    : AppColors.textSecondary,
              ),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
            ),
        ],
      ),
      const SizedBox(height: 4),
      Container(
        decoration: BoxDecoration(
          color: isCustom ? AppColors.primaryLight : Colors.transparent,
          borderRadius: BorderRadius.circular(10),
        ),
        child: Row(
          children: [
            Expanded(
              child: TextButton.icon(
                onPressed: onChooseDates,
                style: TextButton.styleFrom(
                  alignment: Alignment.centerLeft,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 4,
                    vertical: 10,
                  ),
                  foregroundColor: isCustom
                      ? AppColors.primary
                      : AppColors.textSecondary,
                ),
                icon: const Icon(Icons.date_range_outlined, size: 20),
                label: Text(dateLabel, style: const TextStyle(fontSize: 13)),
              ),
            ),
            if (isCustom)
              IconButton(
                tooltip: 'Clear date filter',
                onPressed: onClearDates,
                icon: const Icon(Icons.close_rounded, size: 18),
              )
            else
              const Padding(
                padding: EdgeInsets.only(right: 4),
                child: Icon(
                  Icons.keyboard_arrow_down_rounded,
                  size: 20,
                  color: AppColors.textSecondary,
                ),
              ),
          ],
        ),
      ),
    ],
  );
}
