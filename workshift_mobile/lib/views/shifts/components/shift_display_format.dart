/// Display every shift in Vietnam time, regardless of the device's time zone.
abstract final class ShiftDisplayFormat {
  static String weekday(DateTime date) => _weekdays[date.weekday - 1];
  static String monthTitle(DateTime first, DateTime last) {
    const names = [
      'January',
      'February',
      'March',
      'April',
      'May',
      'June',
      'July',
      'August',
      'September',
      'October',
      'November',
      'December',
    ];
    if (first.year != last.year) {
      return '${_months[first.month - 1]} ${first.year} – ${_months[last.month - 1]} ${last.year}';
    }
    if (first.month != last.month) {
      return '${_months[first.month - 1]} – ${_months[last.month - 1]} ${last.year}';
    }
    return '${names[first.month - 1]} ${first.year}';
  }

  static String shiftStatus(String status) => switch (status) {
    'SCHEDULED' => 'Scheduled',
    'IN_PROGRESS' => 'In progress',
    'COMPLETED' => 'Completed',
    'CANCELLED' => 'Cancelled',
    _ => status,
  };
  static String weekStatus(String status) => switch (status) {
    'OPEN_REGISTRATION' => 'Registration open',
    'REGISTRATION_CLOSED' => 'Registration closed',
    'COMPLETED' => 'Completed',
    'CANCELLED' => 'Cancelled',
    'DRAFT' => 'Draft',
    _ => status,
  };
  static String assignmentStatus(String status) => switch (status) {
    'ASSIGNED' => 'Confirmed',
    'COMPLETED' => 'Completed',
    'CANCELLED' => 'Cancelled',
    _ => status,
  };
  static String assignmentSource(String source) => switch (source) {
    'SELF_REGISTRATION' => 'Self-registered',
    'MANAGER_ASSIGNMENT' => 'Assigned by manager',
    'SHIFT_SWAP' => 'Assigned through swap',
    _ => source,
  };
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
