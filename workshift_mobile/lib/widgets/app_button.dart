import 'package:flutter/material.dart';

import '../theme/app_colors.dart';

enum AppButtonType {
  primary, // Nút xanh chính (như nút "Chấm công vào")
  secondary, // Nút nền xám nhạt (như nút "Xem" ở danh sách)
  outline, // Nút viền ngoài
  text, // Nút chữ (như "Xem chi tiết ca ->")
}

/// Nút bấm chuẩn của ứng dụng WorkShift
class AppButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final AppButtonType type;
  final IconData? icon;
  final bool isLoading;
  final double? width;
  final double height;
  final double borderRadius;

  const AppButton({
    super.key,
    required this.text,
    this.onPressed,
    this.type = AppButtonType.primary,
    this.icon,
    this.isLoading = false,
    this.width,
    this.height = 50,
    this.borderRadius = 14,
  });

  @override
  Widget build(BuildContext context) {
    if (type == AppButtonType.text) {
      return TextButton(
        onPressed: isLoading ? null : onPressed,
        style: TextButton.styleFrom(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          foregroundColor: AppColors.primary,
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              text,
              style: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.w600,
                color: AppColors.primary,
              ),
            ),
            if (icon != null) ...[
              const SizedBox(width: 4),
              Icon(icon, size: 16, color: AppColors.primary),
            ],
          ],
        ),
      );
    }

    Color bgColor;
    Color textColor;
    BorderSide borderSide = BorderSide.none;

    switch (type) {
      case AppButtonType.primary:
        bgColor = AppColors.primary;
        textColor = Colors.white;
        break;
      case AppButtonType.secondary:
        bgColor = AppColors.borderLight;
        textColor = AppColors.textPrimary;
        break;
      case AppButtonType.outline:
        bgColor = Colors.transparent;
        textColor = AppColors.primary;
        borderSide = const BorderSide(color: AppColors.primary, width: 1.5);
        break;
      default:
        bgColor = AppColors.primary;
        textColor = Colors.white;
    }

    return SizedBox(
      width: width,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: bgColor,
          foregroundColor: textColor,
          disabledBackgroundColor: AppColors.border,
          disabledForegroundColor: AppColors.textMuted,
          elevation: 0,
          minimumSize: Size(0, height),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(borderRadius),
            side: borderSide,
          ),
        ),
        child: isLoading
            ? SizedBox(
                width: 22,
                height: 22,
                child: CircularProgressIndicator(
                  strokeWidth: 2.5,
                  valueColor: AlwaysStoppedAnimation<Color>(textColor),
                ),
              )
            : Row(
                mainAxisAlignment: MainAxisAlignment.center,
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (icon != null) ...[
                    Icon(icon, size: 20, color: textColor),
                    const SizedBox(width: 8),
                  ],
                  Flexible(
                    child: Text(
                      text,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w600,
                        color: textColor,
                      ),
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}
