/// Display every shift in Vietnam time, regardless of the device's time zone.
abstract final class ShiftDisplayFormat {
  static const _weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  static const _months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];

  static DateTime vietnamTime(DateTime utc) =>
      utc.toUtc().add(const Duration(hours: 7));

  static String time(DateTime utc) {
    final date = vietnamTime(utc);
    return '${date.hour.toString().padLeft(2, '0')}:'
        '${date.minute.toString().padLeft(2, '0')}';
  }

  static String dayHeading(DateTime utc) {
    final date = vietnamTime(utc);
    return '${_weekdays[date.weekday - 1]}, ${date.day} ${_months[date.month - 1]} ${date.year}';
  }

  static String calendarDate(DateTime date) =>
      '${date.day.toString().padLeft(2, '0')}/'
      '${date.month.toString().padLeft(2, '0')}/${date.year}';

  static String deadline(DateTime utc) =>
      '${calendarDate(vietnamTime(utc))} at ${time(utc)}';

  static String duration(DateTime start, DateTime end) {
    final minutes = end.difference(start).inMinutes;
    final hours = minutes ~/ 60;
    final remainder = minutes % 60;
    return [
      if (hours > 0) '${hours}h',
      if (remainder > 0 || hours == 0) '${remainder}m',
    ].join(' ');
  }

  static bool sameDay(DateTime first, DateTime second) {
    final a = vietnamTime(first);
    final b = vietnamTime(second);
    return a.year == b.year && a.month == b.month && a.day == b.day;
  }
}
