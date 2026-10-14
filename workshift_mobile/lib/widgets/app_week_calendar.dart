import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Presentation only; the screen/controller supply calendar dates and markers.
class AppWeekCalendar extends StatelessWidget {
  final String title;
  final String rangeLabel;
  final List<DateTime> days;
  final DateTime selectedDay;
  final DateTime today;
  final Set<int> markedDays;
  final ValueChanged<DateTime> onDaySelected;
  final VoidCallback onPreviousWeek;
  final VoidCallback onNextWeek;
  const AppWeekCalendar({
    super.key,
    required this.title,
    required this.rangeLabel,
    required this.days,
    required this.selectedDay,
    required this.today,
    required this.markedDays,
    required this.onDaySelected,
    required this.onPreviousWeek,
    required this.onNextWeek,
  });
  @override
  Widget build(BuildContext context) => Container(
    color: AppColors.surface,
    child: Column(
      children: [
        Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
          color: AppColors.primaryLight,
          child: Text(
            rangeLabel,
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
              fontWeight: FontWeight.w500,
            ),
          ),
        ),
        Row(
          children: [
            IconButton(
              tooltip: 'Previous week',
              onPressed: onPreviousWeek,
              icon: const Icon(Icons.chevron_left_rounded),
            ),
            Expanded(
              child: Text(
                title,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
            ),
            IconButton(
              tooltip: 'Next week',
              onPressed: onNextWeek,
              icon: const Icon(Icons.chevron_right_rounded),
            ),
          ],
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
          child: Row(
            children: [
              for (final day in days)
                Expanded(
                  child: _DayButton(
                    day: day,
                    selected: day == selectedDay,
                    isToday: day == today,
                    marked: markedDays.contains(day.day),
                    onTap: () => onDaySelected(day),
                  ),
                ),
            ],
          ),
        ),
      ],
    ),
  );
}

class _DayButton extends StatelessWidget {
  final DateTime day;
  final bool selected;
  final bool isToday;
  final bool marked;
  final VoidCallback onTap;
  const _DayButton({
    required this.day,
    required this.selected,
    required this.isToday,
    required this.marked,
    required this.onTap,
  });
  @override
  Widget build(BuildContext context) => Semantics(
    button: true,
    selected: selected,
    label:
        '${day.day}/${day.month}/${day.year}${isToday ? ', today' : ''}${marked ? ', has shifts' : ''}',
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(20),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          children: [
            Text(
              const [
                'Mon',
                'Tue',
                'Wed',
                'Thu',
                'Fri',
                'Sat',
                'Sun',
              ][day.weekday - 1],
              style: const TextStyle(
                fontSize: 11,
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: 8),
            Container(
              width: 36,
              height: 36,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? AppColors.primaryDark : Colors.transparent,
                shape: BoxShape.circle,
                border: isToday && !selected
                    ? Border.all(color: AppColors.primary)
                    : null,
              ),
              child: Text(
                '${day.day}',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: selected || isToday
                      ? FontWeight.w700
                      : FontWeight.w500,
                  color: selected ? AppColors.surface : AppColors.textPrimary,
                ),
              ),
            ),
            const SizedBox(height: 5),
            Container(
              width: 5,
              height: 5,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: marked ? AppColors.primaryDark : Colors.transparent,
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
