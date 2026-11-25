import 'package:flutter/material.dart';
import '../../../models/attendance/attendance_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_button.dart';
import '../../../widgets/status_badge.dart';

/// Component: Thẻ thông tin Ca làm việc hôm nay
class TodayShiftCard extends StatelessWidget {
  final EligibleShiftModel? shift;
  final VoidCallback? onCheckInTap;
  final VoidCallback? onCheckOutTap;
  final VoidCallback? onViewAttendanceHistoryTap;
  final VoidCallback? onRegisterFaceTap;
  final bool isFaceNotRegistered;
  final bool isCheckingIn;
  final bool isCheckingOut;

  const TodayShiftCard({
    super.key,
    this.shift,
    this.onCheckInTap,
    this.onCheckOutTap,
    this.onViewAttendanceHistoryTap,
    this.onRegisterFaceTap,
    this.isFaceNotRegistered = false,
    this.isCheckingIn = false,
    this.isCheckingOut = false,
  });

  // Format backend check-in reason to user-friendly English
  String _formatReason(String reason) {
    final lower = reason.toLowerCase();
    if (lower.contains('closed')) {
      final match = RegExp(r'(\d{1,2}:\d{2})').firstMatch(reason);
      if (match != null) {
        return 'Check-in window is closed (Cutoff: ${match.group(1)}).';
      }
      return 'Check-in window is closed for this shift.';
    }
    if (lower.contains('not opened')) {
      final match = RegExp(r'(\d{1,2}:\d{2})').firstMatch(reason);
      if (match != null) {
        return 'Check-in window not open yet (Opens at ${match.group(1)}).';
      }
      return 'Check-in window is not open yet for this shift.';
    }
    if (lower.contains('leave')) {
      return 'You have approved leave for this shift.';
    }
    return reason;
  }

  @override
  Widget build(BuildContext context) {
    if (shift == null) {
      // Trường hợp hôm nay không có ca làm việc hoặc các ca đã kết thúc / hết thời gian checkout
      return Container(
        width: double.infinity,
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [AppColors.surface, AppColors.primaryLight],
          ),
          borderRadius: BorderRadius.circular(24),
          border: Border.all(color: AppColors.borderLight),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.03),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
        child: Column(
          children: [
            Container(
              width: 56,
              height: 56,
              decoration: BoxDecoration(
                color: isFaceNotRegistered
                    ? Colors.deepOrange.withValues(alpha: 0.1)
                    : AppColors.primaryLight,
                shape: BoxShape.circle,
              ),
              child: Icon(
                isFaceNotRegistered
                    ? Icons.face_retouching_off_rounded
                    : Icons.event_available_rounded,
                color: isFaceNotRegistered
                    ? Colors.deepOrange
                    : AppColors.primary,
                size: 30,
              ),
            ),
            const SizedBox(height: 14),
            Text(
              isFaceNotRegistered
                  ? 'Face Profile Required'
                  : 'No Upcoming Shift',
              style: const TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              isFaceNotRegistered
                  ? 'You have not registered your face profile yet. Please complete face registration to enable attendance check-in.'
                  : 'You do not have any upcoming shifts scheduled for today.',
              textAlign: TextAlign.center,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
                height: 1.4,
              ),
            ),
            const SizedBox(height: 18),
            if (isFaceNotRegistered && onRegisterFaceTap != null) ...[
              AppButton(
                text: 'Register Face Profile',
                type: AppButtonType.primary,
                height: 44,
                icon: Icons.face_unlock_rounded,
                onPressed: onRegisterFaceTap,
              ),
              const SizedBox(height: 10),
              AppButton(
                text: 'View Attendance History',
                type: AppButtonType.secondary,
                height: 44,
                icon: Icons.history_rounded,
                onPressed: onViewAttendanceHistoryTap,
              ),
            ] else ...[
              AppButton(
                text: 'View Attendance History',
                type: AppButtonType.primary,
                height: 44,
                icon: Icons.history_rounded,
                onPressed: onViewAttendanceHistoryTap,
              ),
            ],
          ],
        ),
      );
    }

    final isCompleted = shift!.hasCheckedOut;
    final isInProgress = shift!.hasCheckedIn && !shift!.hasCheckedOut;
    final hasReason =
        !isCompleted &&
        !isInProgress &&
        !shift!.canCheckInNow &&
        shift!.canNotCheckInReason != null &&
        shift!.canNotCheckInReason!.trim().isNotEmpty;

    // Shift scheduled for today
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.surface, AppColors.primaryLight],
        ),
        borderRadius: BorderRadius.circular(24),
        border: Border.all(
          color: isInProgress
              ? const Color(0xFFF59E0B).withValues(alpha: 0.3)
              : (isCompleted
                    ? const Color(0xFF10B981).withValues(alpha: 0.3)
                    : (shift!.canCheckInNow
                          ? AppColors.primary.withValues(alpha: 0.25)
                          : AppColors.borderLight)),
          width: (shift!.canCheckInNow || isInProgress) ? 1.5 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: isInProgress
                ? const Color(0xFFF59E0B).withValues(alpha: 0.05)
                : (shift!.canCheckInNow
                      ? AppColors.primary.withValues(alpha: 0.06)
                      : Colors.black.withValues(alpha: 0.03)),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 1. Shift title & compact status badge
          Wrap(
            alignment: WrapAlignment.spaceBetween,
            spacing: 12,
            runSpacing: 10,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 9,
                    height: 9,
                    decoration: BoxDecoration(
                      color: isCompleted
                          ? const Color(0xFF10B981)
                          : (isInProgress
                                ? const Color(0xFFF59E0B)
                                : (shift!.canCheckInNow
                                      ? AppColors.success
                                      : AppColors.warning)),
                      shape: BoxShape.circle,
                    ),
                  ),
                  const SizedBox(width: 8),
                  const Text(
                    "Today's Shift",
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 15,
                      color: AppColors.textPrimary,
                    ),
                  ),
                ],
              ),
              StatusBadge(
                text: isCompleted
                    ? 'Shift Finished'
                    : (isInProgress
                          ? 'Working Now'
                          : (shift!.canCheckInNow
                                ? 'Ready to check-in'
                                : (shift!.hasApprovedLeave
                                      ? 'On Leave'
                                      : 'Off Hours'))),
                backgroundColor: isCompleted
                    ? const Color(0xFFDCFCE7)
                    : (isInProgress
                          ? const Color(0xFFFEF3C7)
                          : (shift!.canCheckInNow
                                ? AppColors.successBg
                                : AppColors.warningBg)),
                textColor: isCompleted
                    ? const Color(0xFF15803D)
                    : (isInProgress
                          ? const Color(0xFFB45309)
                          : (shift!.canCheckInNow
                                ? AppColors.success
                                : AppColors.warning)),
                icon: isCompleted
                    ? Icons.check_circle_rounded
                    : (isInProgress
                          ? Icons.timer_outlined
                          : (shift!.canCheckInNow
                                ? Icons.check_circle_outline_rounded
                                : Icons.schedule_rounded)),
              ),
            ],
          ),

          // 2. Banner info if not ready for check-in
          if (hasReason) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                color: AppColors.warningBg.withValues(alpha: 0.7),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: AppColors.warning.withValues(alpha: 0.3),
                ),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.info_outline_rounded,
                    color: AppColors.warning,
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      _formatReason(shift!.canNotCheckInReason!),
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w500,
                        color: AppColors.warning,
                        height: 1.35,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ] else if (isCompleted) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                color: const Color(0xFFF0FDF4),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFBBF7D0)),
              ),
              child: Row(
                children: [
                  const Icon(
                    Icons.verified_rounded,
                    color: Color(0xFF16A34A),
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'You have completed this shift (${shift!.checkInTime != null ? shift!.checkInTime!.toLocal().toString().substring(11, 16) : ''} - ${shift!.checkOutTime != null ? shift!.checkOutTime!.toLocal().toString().substring(11, 16) : ''}).',
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF15803D),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ] else if (isInProgress) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                color: const Color(0xFFFFFBEB),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: const Color(0xFFFDE68A)),
              ),
              child: Row(
                children: [
                  const Icon(
                    Icons.timer_rounded,
                    color: Color(0xFFD97706),
                    size: 18,
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Checked in at ${shift!.checkInTime?.toLocal().toString().substring(11, 16) ?? ''}. Remember to check out when done.',
                      style: const TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFFB45309),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],

          const SizedBox(height: 14),

          // 3. Shift name & Check-in status
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Expanded(
                child: Text(
                  shift!.shiftName,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: 8),
              if (shift!.hasCheckedOut)
                StatusBadge.success(text: 'Completed')
              else if (shift!.hasCheckedIn)
                StatusBadge.warning(text: 'In Progress')
              else if (shift!.canCheckInNow)
                StatusBadge.neutral(text: 'Awaiting check-in')
              else
                StatusBadge.neutral(text: 'Not checked in'),
            ],
          ),

          const SizedBox(height: 8),

          // 4. Time range & Duration
          Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              const Icon(
                Icons.access_time_filled_rounded,
                color: AppColors.primary,
                size: 24,
              ),
              const SizedBox(width: 8),
              Flexible(
                child: FittedBox(
                  fit: BoxFit.scaleDown,
                  child: Text(
                    shift!.formattedTimeRange,
                    style: const TextStyle(
                      fontSize: 26,
                      fontWeight: FontWeight.w800,
                      color: AppColors.primary,
                      letterSpacing: -0.5,
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  shift!.durationText,
                  style: const TextStyle(
                    fontSize: 11.5,
                    fontWeight: FontWeight.w700,
                    color: AppColors.primary,
                  ),
                ),
              ),
            ],
          ),

          const SizedBox(height: 14),

          // 5. Branch Location
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: AppColors.background,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: AppColors.borderLight),
            ),
            child: Row(
              children: [
                const Icon(
                  Icons.storefront_rounded,
                  size: 18,
                  color: AppColors.textMuted,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    '${shift!.branchName} • ${shift!.branchAddress}',
                    style: const TextStyle(
                      fontSize: 12.5,
                      color: AppColors.textSecondary,
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),

          const SizedBox(height: 16),

          // 6. Check-in and Check-out Action Buttons
          Row(
            children: [
              Expanded(
                child: AppButton(
                  text: shift!.hasCheckedIn ? 'Checked In' : 'Check In',
                  icon: shift!.hasCheckedIn
                      ? Icons.check_circle_rounded
                      : Icons.how_to_reg_rounded,
                  type: shift!.hasCheckedIn
                      ? AppButtonType.secondary
                      : AppButtonType.primary,
                  isLoading: isCheckingIn,
                  onPressed: (isCheckingIn || isCheckingOut)
                      ? null
                      : onCheckInTap,
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: AppButton(
                  text: shift!.hasCheckedOut ? 'Checked Out' : 'Check Out',
                  icon: shift!.hasCheckedOut
                      ? Icons.check_circle_rounded
                      : Icons.logout_rounded,
                  type: isInProgress
                      ? AppButtonType.primary
                      : AppButtonType.secondary,
                  isLoading: isCheckingOut,
                  onPressed: (isCheckingIn || isCheckingOut)
                      ? null
                      : onCheckOutTap,
                ),
              ),
            ],
          ),

          const SizedBox(height: 6),

          // 7. View Attendance History
          if (onViewAttendanceHistoryTap != null) ...[
            Center(
              child: TextButton.icon(
                onPressed: onViewAttendanceHistoryTap,
                icon: const Icon(
                  Icons.history_rounded,
                  size: 16,
                  color: AppColors.textSecondary,
                ),
                label: const Text(
                  'Attendance History',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w600,
                    color: AppColors.textSecondary,
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
