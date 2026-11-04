import 'package:flutter/material.dart';
import '../../../controllers/home/home_controller.dart';
import '../../../models/attendance/attendance_shift_model.dart';
import '../../../theme/app_colors.dart';
import '../../attendance/attendance_modal.dart';

import '../../face_registration/face_registration_modal.dart';

/// Helper handler tách biệt toàn bộ nghiệp vụ Check-In & Check-Out
/// Giúp home_view.dart sạch sẽ và độc lập cho người khác phát triển UI
class AttendanceFlowHandler {
  static bool _isProcessing = false;

  /// Cờ kiểm tra xem có tác vụ chấm công nào đang xử lý không (tránh bấm liên tiếp)
  static bool get isProcessing => _isProcessing;

  /// Xử lý bấm nút Check-In
  static Future<void> handleCheckIn({
    required BuildContext context,
    required HomeController homeController,
    EligibleShiftModel? targetShift,
  }) async {
    if (_isProcessing) return;
    _isProcessing = true;
    homeController.setCheckingIn(true);

    try {
      // 1. Silent sync dữ liệu mới nhất từ server mà không làm giật màn hình
      await homeController.loadHomeData(silent: true);
      if (!context.mounted) return;

      // Guard: Chưa đăng ký gương mặt
      if (homeController.isFaceNotRegistered) {
        homeController.setCheckingIn(false);
        final registerNow = await showDialog<bool>(
          context: context,
          builder: (ctx) => AlertDialog(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(20),
            ),
            title: const Row(
              children: [
                Icon(
                  Icons.face_retouching_off_rounded,
                  color: Colors.deepOrange,
                ),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Face Profile Required',
                    style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                  ),
                ),
              ],
            ),
            content: Text(
              homeController.shiftErrorMessage ??
                  'You have not registered your biometric face profile yet.\n\nYou must complete face registration before performing attendance check-in.',
              style: const TextStyle(fontSize: 14, height: 1.4),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(ctx, false),
                child: const Text(
                  'Cancel',
                  style: TextStyle(color: AppColors.textSecondary),
                ),
              ),
              ElevatedButton.icon(
                onPressed: () => Navigator.pop(ctx, true),
                icon: const Icon(Icons.face_unlock_rounded, size: 18),
                label: const Text('Register Face Now'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF7C3AED),
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
              ),
            ],
          ),
        );

        if (registerNow == true && context.mounted) {
          final registered = await showFaceRegistrationModal(context: context);
          if (registered == true && context.mounted) {
            await homeController.loadHomeData();
          }
        }
        return;
      }

      final currentShift = targetShift ?? homeController.todayShift;
      if (currentShift == null) {
        homeController.setCheckingIn(false);
        if (homeController.shiftErrorMessage != null) {
          await _showAlertDialog(
            context: context,
            icon: Icons.warning_amber_rounded,
            iconColor: Colors.orange,
            title: 'Shift Attendance Notice',
            message: homeController.shiftErrorMessage!,
            buttonText: 'OK',
          );
          return;
        }
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No shift assigned for today.')),
        );
        return;
      }

      // Guard: Có ca làm việc trước đó chưa Checkout
      final prevShift =
          currentShift.unfinishedPreviousShift ??
          homeController.unCheckedOutPreviousShift;
      if (prevShift != null) {
        homeController.setCheckingIn(false);
        final prevStart = prevShift.scheduledStartAt
            .toLocal()
            .toString()
            .substring(11, 16);
        final prevEnd = prevShift.scheduledEndAt.toLocal().toString().substring(
          11,
          16,
        );
        await _showAlertDialog(
          context: context,
          icon: Icons.warning_amber_rounded,
          iconColor: Colors.deepOrange,
          title: 'Check-Out Required',
          message:
              'You have an unfinished shift: ${prevShift.shiftName} ($prevStart – $prevEnd) that has not been checked out.\n\nPlease complete Check-Out in Attendance History before checking in for this shift.',
          buttonText: 'Understood',
        );
        return;
      }

      // Guard: Đã check-in rồi
      if (currentShift.hasCheckedIn) {
        homeController.setCheckingIn(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.info_outline_rounded,
          iconColor: AppColors.primary,
          title: 'Already Checked In',
          message:
              'You have already checked in for this shift at ${currentShift.checkInTime?.toLocal().toString().substring(11, 16) ?? ''}.\nYou cannot check in again.',
          buttonText: 'OK',
        );
        return;
      }

      // Guard: Ca đã hết thời gian checkout luôn rồi
      if (currentShift.isCheckoutExpired) {
        homeController.setCheckingIn(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.cancel_outlined,
          iconColor: Colors.red,
          title: 'Attendance Window Closed',
          message:
              'The attendance window for this shift has closed (cut-off passed).\n\nYou cannot check in for this shift.',
          buttonText: 'Understood',
        );
        return;
      }

      // Guard: Chưa tới giờ hoặc hết hạn check-in
      if (!currentShift.canCheckInNow) {
        homeController.setCheckingIn(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.warning_amber_rounded,
          iconColor: Colors.orange,
          title: 'Cannot Check In',
          message:
              currentShift.canNotCheckInReason ??
              'Check-in is currently not available for this shift.',
          buttonText: 'Understood',
        );
        return;
      }

      // Tắt trạng thái loading trên nút trước khi hiển thị popup quét sinh trắc học
      homeController.setCheckingIn(false);

      // Mở popup quét sinh trắc học Check-In
      final refreshed = await showAttendanceModal(
        context: context,
        shift: currentShift,
        isCheckIn: true,
      );

      if (refreshed == true && context.mounted) {
        homeController.markCheckInSuccess(
          assignmentId: currentShift.assignmentId,
        );
        homeController.loadHomeData(silent: true);
      }
    } finally {
      _isProcessing = false;
      homeController.setCheckingIn(false);
    }
  }

  /// Xử lý bấm nút Check-Out
  static Future<void> handleCheckOut({
    required BuildContext context,
    required HomeController homeController,
    EligibleShiftModel? targetShift,
  }) async {
    if (_isProcessing) return;
    _isProcessing = true;
    homeController.setCheckingOut(true);

    try {
      // 1. Silent sync dữ liệu mới nhất từ server
      await homeController.loadHomeData(silent: true);
      if (!context.mounted) return;

      // Guard: Chưa đăng ký gương mặt
      if (homeController.isFaceNotRegistered) {
        homeController.setCheckingOut(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.face_retouching_off_rounded,
          iconColor: Colors.deepOrange,
          title: 'Face Profile Required',
          message:
              'You have not registered your biometric face profile yet.\n\nPlease complete face registration before checking out.',
          buttonText: 'Understood',
        );
        return;
      }

      final currentShift = targetShift ?? homeController.todayShift;
      if (currentShift == null) {
        homeController.setCheckingOut(false);
        if (homeController.shiftErrorMessage != null) {
          await _showAlertDialog(
            context: context,
            icon: Icons.warning_amber_rounded,
            iconColor: Colors.orange,
            title: 'Shift Notice',
            message: homeController.shiftErrorMessage!,
            buttonText: 'OK',
          );
          return;
        }
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No shift scheduled for today.')),
        );
        return;
      }

      // Guard: Chưa check-in
      if (!currentShift.hasCheckedIn) {
        homeController.setCheckingOut(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.warning_amber_rounded,
          iconColor: Colors.orange,
          title: 'Check-In Required',
          message:
              'You have not checked in for this shift yet.\nYou must check in before checking out.',
          buttonText: 'Understood',
        );
        return;
      }

      // Guard: Đã check-out hoàn thành rồi
      if (currentShift.hasCheckedOut) {
        homeController.setCheckingOut(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.check_circle_rounded,
          iconColor: Colors.green,
          title: 'Shift Completed',
          message:
              'You have already completed check-out for this shift at ${currentShift.checkOutTime?.toLocal().toString().substring(11, 16) ?? ''}.',
          buttonText: 'OK',
        );
        return;
      }

      // Guard: Ca đã hết thời gian checkout (Hiển thị popup thông báo ngay lập tức, không cho vào quét mặt)
      if (currentShift.isCheckoutExpired ||
          (!currentShift.canCheckOutNow &&
              (currentShift.canNotCheckOutReason?.toLowerCase().contains(
                        'closed',
                      ) ==
                      true ||
                  currentShift.canNotCheckOutReason?.toLowerCase().contains(
                        'quá hạn',
                      ) ==
                      true))) {
        homeController.setCheckingOut(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.access_time_filled_rounded,
          iconColor: Colors.red,
          title: 'Check-Out Window Closed',
          message:
              currentShift.canNotCheckOutReason ??
              'Check-out window has closed for this shift.\n\nYou cannot check out for this shift anymore. Please contact your Manager.',
          buttonText: 'Understood',
        );
        return;
      }

      // Guard: Chưa đủ điều kiện check-out (Ví dụ dưới 2 phút sau check-in)
      if (!currentShift.canCheckOutNow) {
        homeController.setCheckingOut(false);
        await _showAlertDialog(
          context: context,
          icon: Icons.info_outline_rounded,
          iconColor: Colors.orange,
          title: 'Cannot Check Out',
          message:
              currentShift.canNotCheckOutReason ??
              'You cannot check out at this time.',
          buttonText: 'Understood',
        );
        return;
      }

      // Tắt trạng thái loading trên nút trước khi hiển thị popup quét sinh trắc học
      homeController.setCheckingOut(false);

      // Mở popup quét sinh trắc học Check-Out
      final refreshed = await showAttendanceModal(
        context: context,
        shift: currentShift,
        isCheckIn: false,
      );

      if (refreshed == true && context.mounted) {
        homeController.markCheckOutSuccess(
          assignmentId: currentShift.assignmentId,
        );
        homeController.loadHomeData(silent: true);
      }
    } finally {
      _isProcessing = false;
      homeController.setCheckingOut(false);
    }
  }

  static Future<void> _showAlertDialog({
    required BuildContext context,
    required IconData icon,
    required Color iconColor,
    required String title,
    required String message,
    required String buttonText,
  }) async {
    if (!context.mounted) return;
    await showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Row(
          children: [
            Icon(icon, color: iconColor),
            const SizedBox(width: 8),
            Text(
              title,
              style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          message,
          style: const TextStyle(fontSize: 14, height: 1.4),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(
              buttonText,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }
}
