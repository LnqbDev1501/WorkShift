import 'package:flutter/material.dart';
import '../../../models/shifts/available_shift_model.dart';
import '../../../theme/app_colors.dart';

/// Enum representing the real-world attendance/shift status for an employee.
enum ShiftWorkStatus {
  notYet,
  inProgress,
  completed,
  absent,
  cancelled;

  String get label => switch (this) {
    ShiftWorkStatus.notYet => 'Not yet',
    ShiftWorkStatus.inProgress => 'In progress',
    ShiftWorkStatus.completed => 'Completed',
    ShiftWorkStatus.absent => 'Absent',
    ShiftWorkStatus.cancelled => 'Cancelled',
  };

  Color get accentColor => switch (this) {
    ShiftWorkStatus.notYet => AppColors.primary,
    ShiftWorkStatus.inProgress => const Color(0xFFF59E0B),
    ShiftWorkStatus.completed => const Color(0xFF10B981),
    ShiftWorkStatus.absent => const Color(0xFFEF4444),
    ShiftWorkStatus.cancelled => const Color(0xFF94A3B8),
  };

  Color get backgroundColor => switch (this) {
    ShiftWorkStatus.notYet => const Color(0xFFEFF6FF),
    ShiftWorkStatus.inProgress => const Color(0xFFFEF3C7),
    ShiftWorkStatus.completed => const Color(0xFFDCFCE7),
    ShiftWorkStatus.absent => const Color(0xFFFEE2E2),
    ShiftWorkStatus.cancelled => const Color(0xFFF1F5F9),
  };

  Color get textColor => switch (this) {
    ShiftWorkStatus.notYet => const Color(0xFF1D4ED8),
    ShiftWorkStatus.inProgress => const Color(0xFFB45309),
    ShiftWorkStatus.completed => const Color(0xFF15803D),
    ShiftWorkStatus.absent => const Color(0xFFB91C1C),
    ShiftWorkStatus.cancelled => const Color(0xFF475569),
  };

  IconData get icon => switch (this) {
    ShiftWorkStatus.notYet => Icons.schedule_rounded,
    ShiftWorkStatus.inProgress => Icons.timelapse_rounded,
    ShiftWorkStatus.completed => Icons.check_circle_rounded,
    ShiftWorkStatus.absent => Icons.cancel_outlined,
    ShiftWorkStatus.cancelled => Icons.block_rounded,
  };

  String get bannerTitle => switch (this) {
    ShiftWorkStatus.notYet => 'Upcoming Shift (Not checked in)',
    ShiftWorkStatus.inProgress => 'Shift In Progress',
    ShiftWorkStatus.completed => 'Shift Completed',
    ShiftWorkStatus.absent => 'Absent (No Attendance Recorded)',
    ShiftWorkStatus.cancelled => 'Shift Assignment Cancelled',
  };

  String get bannerDescription => switch (this) {
    ShiftWorkStatus.notYet =>
      'You are registered for this shift. Check-in will be open when the shift window arrives.',
    ShiftWorkStatus.inProgress =>
      'This shift is currently ongoing. Please complete your check-in or check-out on time.',
    ShiftWorkStatus.completed =>
      'You have successfully completed this shift and your attendance has been recorded.',
    ShiftWorkStatus.absent =>
      'The scheduled time for this shift has passed with no check-in or check-out recorded.',
    ShiftWorkStatus.cancelled =>
      'This shift or assignment has been cancelled and is no longer active.',
  };
}

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

  /// Resolves the comprehensive real-world work/attendance status of a shift.
  static ShiftWorkStatus resolveWorkStatus({
    required AvailableShiftModel shift,
    String? attendanceStatus,
    DateTime? now,
  }) {
    final currentTime = now ?? DateTime.now().toUtc();
    final reg = shift.myRegistration;

    // 1. Cancelled
    if (shift.status.toUpperCase() == 'CANCELLED' ||
        reg?.status.toUpperCase() == 'CANCELLED') {
      return ShiftWorkStatus.cancelled;
    }

    // 2. Completed (registration status, shift status, or attendance completed)
    final att = attendanceStatus?.toUpperCase();
    if (reg?.status.toUpperCase() == 'COMPLETED' ||
        shift.status.toUpperCase() == 'COMPLETED' ||
        att == 'COMPLETED' ||
        att == 'PRESENT' ||
        att == 'LATE' ||
        att == 'EARLY_LEAVE' ||
        att == 'LATE_AND_EARLY_LEAVE') {
      return ShiftWorkStatus.completed;
    }

    // 3. Attendance explicitly recorded as ABSENT or INCOMPLETE
    if (att == 'ABSENT' || att == 'INCOMPLETE') {
      return ShiftWorkStatus.absent;
    }

    // 4. Past shift that was not completed -> Absent
    if (currentTime.isAfter(shift.scheduledEndAt)) {
      return ShiftWorkStatus.absent;
    }

    // 5. In-progress shift
    if ((currentTime.isAfter(shift.scheduledStartAt) &&
            currentTime.isBefore(shift.scheduledEndAt)) ||
        shift.status.toUpperCase() == 'IN_PROGRESS' ||
        att == 'IN_PROGRESS') {
      return ShiftWorkStatus.inProgress;
    }

    // 6. Future shift: registered / assigned, not yet started
    return ShiftWorkStatus.notYet;
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
