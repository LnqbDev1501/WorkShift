import 'package:flutter/material.dart';
import '../../../theme/app_colors.dart';

/// Component: Mục "Cần xử lý" (Bảng công chờ xác nhận, yêu cầu đổi ca)
class PendingTasks extends StatelessWidget {
  const PendingTasks({super.key});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Title + Badge count (2)
        Row(
          children: [
            const Text(
              'Pending Tasks',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(width: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
              decoration: BoxDecoration(
                color: AppColors.errorBg,
                borderRadius: BorderRadius.circular(10),
              ),
              child: const Text(
                '2',
                style: TextStyle(
                  color: AppColors.error,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),

        // Task 1: Weekly Timesheet
        _buildPendingItem(
          context: context,
          icon: Icons.assignment_outlined,
          iconColor: AppColors.warning,
          iconBgColor: AppColors.warningBg,
          title: 'Weekly Timesheet',
          subtitle: 'Pending your confirmation',
          onTap: () {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Viewing Weekly Timesheet details')),
            );
          },
        ),
        const SizedBox(height: 10),

        // Task 2: Shift Swap Request
        _buildPendingItem(
          context: context,
          icon: Icons.sync_rounded,
          iconColor: AppColors.primary,
          iconBgColor: AppColors.primaryLight,
          title: 'Shift Swap Request',
          subtitle: 'Afternoon shift • Oct 08',
          onTap: () {
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Viewing Shift Swap details')),
            );
          },
        ),
      ],
    );
  }

  Widget _buildPendingItem({
    required BuildContext context,
    required IconData icon,
    required Color iconColor,
    required Color iconBgColor,
    required String title,
    required String subtitle,
    required VoidCallback onTap,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderLight),
      ),
      clipBehavior: Clip.antiAlias,
      child: IntrinsicHeight(
        child: Row(
          children: [
            // Dải màu cam nổi bật bên mép trái
            Container(
              width: 4,
              color: AppColors.warning,
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                child: Row(
                  children: [
                    // Icon bo góc
                    Container(
                      width: 42,
                      height: 42,
                      decoration: BoxDecoration(
                        color: iconBgColor,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(icon, color: iconColor, size: 22),
                    ),
                    const SizedBox(width: 12),
                    // Tiêu đề & mô tả
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            title,
                            style: const TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            subtitle,
                            style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.textMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                    // Button "View"
                    TextButton(
                      onPressed: onTap,
                      style: TextButton.styleFrom(
                        backgroundColor: AppColors.borderLight,
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(8),
                        ),
                      ),
                      child: const Text(
                        'View',
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
