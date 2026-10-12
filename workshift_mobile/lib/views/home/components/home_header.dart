import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';
import '../../../widgets/status_badge.dart';

/// Component 1: Phần đầu Trang chủ (Lời chào, Avatar, Chi nhánh, Thông báo)
class HomeHeader extends StatelessWidget {
  final String userName;
  final String branchName;
  final String? avatarUrl;
  final VoidCallback? onLogoutTap;

  const HomeHeader({
    super.key,
    required this.userName,
    required this.branchName,
    this.avatarUrl,
    this.onLogoutTap,
  });

  String _getFormattedDate() {
    final now = DateTime.now();
    const weekdays = [
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday'
    ];
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final weekday = weekdays[now.weekday - 1];
    final month = months[now.month - 1];
    final day = now.day.toString().padLeft(2, '0');
    final year = now.year;
    return '$weekday, $month $day, $year';
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // App header bar: Logo WorkShift + Branch + Bell + Avatar
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                const Text(
                  'WorkShift',
                  style: TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w900,
                    fontSize: 22,
                    letterSpacing: -0.5,
                  ),
                ),
                const SizedBox(width: 8),
                StatusBadge.branch(text: branchName),
              ],
            ),
            Row(
              children: [
                IconButton(
                  onPressed: () {},
                  icon: const Icon(Icons.notifications_outlined, color: AppColors.textPrimary),
                ),
                IconButton(
                  tooltip: 'Logout',
                  onPressed: onLogoutTap,
                  icon: const Icon(Icons.logout_rounded, color: AppColors.error, size: 20),
                ),
                const SizedBox(width: 4),
                CircleAvatar(
                  radius: 17,
                  backgroundColor: AppColors.primaryLight,
                  backgroundImage: (avatarUrl != null && avatarUrl!.isNotEmpty)
                      ? NetworkImage(avatarUrl!)
                      : null,
                  child: (avatarUrl == null || avatarUrl!.isEmpty)
                      ? const Icon(Icons.person, color: AppColors.primary, size: 18)
                      : null,
                ),
              ],
            ),
          ],
        ),
        const SizedBox(height: 14),

        // Greeting & Current date
        Text(
          _getFormattedDate(),
          style: const TextStyle(
            fontSize: 13,
            color: AppColors.textMuted,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          'Good morning, $userName!',
          style: const TextStyle(
            fontSize: 24,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
      ],
    );
  }
}
