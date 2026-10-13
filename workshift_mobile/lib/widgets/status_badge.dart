import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Thẻ trạng thái nhỏ (Badge) như trong thiết kế: "Sắp đến ca", "Chưa chấm công"
class StatusBadge extends StatelessWidget {
  final String text;
  final Color backgroundColor;
  final Color textColor;
  final IconData? icon;
  final bool hasDot;
  final Color? dotColor;

  const StatusBadge({
    super.key,
    required this.text,
    this.backgroundColor = AppColors.borderLight,
    this.textColor = AppColors.textSecondary,
    this.icon,
    this.hasDot = false,
    this.dotColor,
  });

  /// Factory cho trạng thái "Sắp đến ca" (màu cam nhạt)
  factory StatusBadge.upcoming({required String text}) {
    return StatusBadge(
      text: text,
      icon: Icons.timer_outlined,
      backgroundColor: AppColors.warningBg,
      textColor: AppColors.warning,
    );
  }

  /// Factory cho trạng thái "In Progress / Đang làm việc"
  factory StatusBadge.warning({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.warning,
      backgroundColor: AppColors.warningBg,
      textColor: AppColors.warning,
    );
  }

  /// Factory cho trạng thái "Completed / Hoàn thành"
  factory StatusBadge.success({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.success,
      backgroundColor: const Color(0xFFDCFCE7),
      textColor: const Color(0xFF15803D),
    );
  }

  /// Factory cho trạng thái "Chưa chấm công" (chấm xám + chữ xám)
  factory StatusBadge.neutral({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.textMuted,
      backgroundColor: AppColors.borderLight,
      textColor: AppColors.textSecondary,
    );
  }

  /// Factory cho thông tin Chi nhánh (chấm xanh lá + nền xanh nhạt)
  factory StatusBadge.branch({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.success,
      backgroundColor: AppColors.primaryLight,
      textColor: AppColors.primaryDark,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 13, color: textColor),
            const SizedBox(width: 4),
          ],
          if (hasDot) ...[
            Container(
              width: 6,
              height: 6,
              decoration: BoxDecoration(
                color: dotColor ?? textColor,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 5),
          ],
          Text(
            text,
            style: TextStyle(
              color: textColor,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
