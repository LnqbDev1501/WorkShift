import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Reusable action tile with an accessible Material touch surface.
class QuickActionItem extends StatelessWidget {
  final String title;
  final String subtitle;
  final IconData icon;
  final Color iconColor;
  final Color iconBgColor;
  final VoidCallback? onTap;
  final bool isGrayedOut;

  const QuickActionItem({
    super.key,
    required this.title,
    required this.subtitle,
    required this.icon,
    required this.iconColor,
    required this.iconBgColor,
    this.onTap,
    this.isGrayedOut = false,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: isGrayedOut ? const Color(0xFFF3F4F6) : AppColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(
          color: isGrayedOut ? Colors.grey.shade300 : AppColors.border,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    padding: const EdgeInsets.all(11),
                    decoration: BoxDecoration(
                      color: isGrayedOut ? Colors.grey.shade200 : iconBgColor,
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: Icon(
                      icon,
                      color: isGrayedOut ? Colors.grey.shade500 : iconColor,
                      size: 24,
                    ),
                  ),
                  const Spacer(),
                  Icon(
                    Icons.north_east_rounded,
                    size: 16,
                    color: isGrayedOut ? Colors.grey.shade400 : AppColors.textMuted,
                  ),
                ],
              ),
              const SizedBox(height: 16),
              Text(
                title,
                style: TextStyle(
                  fontSize: 14,
                  height: 1.35,
                  fontWeight: FontWeight.w700,
                  color: isGrayedOut ? Colors.grey.shade600 : AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                subtitle,
                style: TextStyle(
                  fontSize: 12,
                  height: 1.4,
                  color: isGrayedOut ? Colors.grey.shade500 : AppColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
