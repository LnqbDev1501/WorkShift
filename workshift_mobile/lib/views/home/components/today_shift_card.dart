import 'package:flutter/material.dart';
import '../../../models/shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/app_button.dart';
import '../../../widgets/status_badge.dart';

/// Component: Thẻ thông tin Ca làm việc hôm nay
class TodayShiftCard extends StatelessWidget {
  final EligibleShiftModel? shift;
  final VoidCallback? onCheckInTap;
  final VoidCallback? onViewDetailTap;

  const TodayShiftCard({
    super.key,
    this.shift,
    this.onCheckInTap,
    this.onViewDetailTap,
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
      // Trường hợp hôm nay không có ca làm việc
      return Container(
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(20),
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
              decoration: const BoxDecoration(
                color: AppColors.primaryLight,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.event_available_rounded,
                color: AppColors.primary,
                size: 30,
              ),
            ),
            const SizedBox(height: 14),
            const Text(
              'No shift scheduled for today',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 6),
            const Text(
              'Take a rest or view your upcoming schedule for this week!',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 13,
                color: AppColors.textSecondary,
              ),
            ),
            const SizedBox(height: 18),
            AppButton(
              text: 'View weekly schedule',
              type: AppButtonType.secondary,
              height: 44,
              onPressed: onViewDetailTap,
            ),
          ],
        ),
      );
    }

    final hasReason = !shift!.canCheckInNow &&
        shift!.canNotCheckInReason != null &&
        shift!.canNotCheckInReason!.trim().isNotEmpty;

    // Shift scheduled for today
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: shift!.canCheckInNow
              ? AppColors.primary.withValues(alpha: 0.25)
              : AppColors.borderLight,
          width: shift!.canCheckInNow ? 1.5 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: shift!.canCheckInNow
                ? AppColors.primary.withValues(alpha: 0.06)
                : Colors.black.withValues(alpha: 0.03),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 1. Shift title & compact status badge
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 9,
                    height: 9,
                    decoration: BoxDecoration(
                      color: shift!.canCheckInNow ? AppColors.success : AppColors.warning,
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
                text: shift!.canCheckInNow
                    ? 'Ready to check-in'
                    : (shift!.hasApprovedLeave ? 'On Leave' : 'Off Hours'),
                backgroundColor: shift!.canCheckInNow
                    ? AppColors.successBg
                    : (shift!.hasApprovedLeave ? AppColors.infoBg : AppColors.warningBg),
                textColor: shift!.canCheckInNow
                    ? AppColors.success
                    : (shift!.hasApprovedLeave ? AppColors.info : AppColors.warning),
                icon: shift!.canCheckInNow
                    ? Icons.check_circle_outline_rounded
                    : Icons.schedule_rounded,
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
              StatusBadge.neutral(
                text: shift!.canCheckInNow ? 'Awaiting check-in' : 'Not checked in',
              ),
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
              Text(
                shift!.formattedTimeRange,
                style: const TextStyle(
                  fontSize: 26,
                  fontWeight: FontWeight.w800,
                  color: AppColors.primary,
                  letterSpacing: -0.5,
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

          // 6. Check-in Button
          AppButton(
            text: 'Check In',
            icon: Icons.how_to_reg_rounded,
            onPressed: onCheckInTap,
          ),

          const SizedBox(height: 6),

          // 7. View Shift Details
          Center(
            child: AppButton(
              text: 'View shift details',
              type: AppButtonType.text,
              icon: Icons.arrow_forward_rounded,
              onPressed: onViewDetailTap,
            ),
          ),
        ],
      ),
    );
  }
}

