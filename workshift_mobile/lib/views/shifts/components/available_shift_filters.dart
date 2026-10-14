import 'package:flutter/material.dart';

import '../../../controllers/available_shifts_controller.dart';
import '../../../theme/app_colors.dart';

class AvailableShiftFilters extends StatelessWidget {
  final AvailableShiftFilter selected;
  final String dateLabel;
  final ValueChanged<AvailableShiftFilter> onSelected;
  final VoidCallback onChooseDates;

  const AvailableShiftFilters({
    super.key,
    required this.selected,
    required this.dateLabel,
    required this.onSelected,
    required this.onChooseDates,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 6,
          children: [
            for (final (filter, label) in [
              (AvailableShiftFilter.allUpcoming, 'All dates'),
              (AvailableShiftFilter.thisWeek, 'This week'),
              (AvailableShiftFilter.nextWeek, 'Next week'),
            ])
              ChoiceChip(
                label: Text(label),
                selected: selected == filter,
                onSelected: (_) => onSelected(filter),
                showCheckmark: false,
                selectedColor: AppColors.primary,
                backgroundColor: AppColors.surface,
                side: BorderSide(
                  color: selected == filter
                      ? AppColors.primary
                      : AppColors.border,
                ),
                labelStyle: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: selected == filter
                      ? AppColors.surface
                      : AppColors.textSecondary,
                ),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 10,
                ),
              ),
          ],
        ),
        const SizedBox(height: 10),
        Container(
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: selected == AvailableShiftFilter.custom
                  ? AppColors.primary
                  : AppColors.border,
            ),
          ),
          child: Row(
            children: [
              Expanded(
                child: TextButton.icon(
                  onPressed: onChooseDates,
                  style: TextButton.styleFrom(
                    alignment: Alignment.centerLeft,
                    padding: const EdgeInsets.symmetric(
                      horizontal: 14,
                      vertical: 14,
                    ),
                    foregroundColor: selected == AvailableShiftFilter.custom
                        ? AppColors.primary
                        : AppColors.textSecondary,
                  ),
                  icon: const Icon(Icons.date_range_outlined, size: 20),
                  label: Text(dateLabel, style: const TextStyle(fontSize: 13)),
                ),
              ),
              if (selected == AvailableShiftFilter.custom)
                IconButton(
                  tooltip: 'Clear date filter',
                  onPressed: () => onSelected(AvailableShiftFilter.allUpcoming),
                  icon: const Icon(Icons.close_rounded, size: 18),
                )
              else
                Padding(
                  padding: const EdgeInsets.only(right: 12),
                  child: TextButton(
                    onPressed: onChooseDates,
                    child: const Text('Change'),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
