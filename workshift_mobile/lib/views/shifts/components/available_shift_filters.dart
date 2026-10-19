import 'package:flutter/material.dart';
import '../../../controllers/available_shifts_controller.dart';
import '../../../widgets/app_date_filters.dart';

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
  Widget build(BuildContext context) => AppDateFilters<AvailableShiftFilter>(
    options: const [
      (AvailableShiftFilter.allUpcoming, 'All dates'),
      (AvailableShiftFilter.thisWeek, 'This week'),
      (AvailableShiftFilter.nextWeek, 'Next week'),
    ],
    selected: selected,
    isCustom: selected == AvailableShiftFilter.custom,
    dateLabel: dateLabel,
    onSelected: onSelected,
    onChooseDates: onChooseDates,
    onClearDates: () => onSelected(AvailableShiftFilter.allUpcoming),
  );
}
