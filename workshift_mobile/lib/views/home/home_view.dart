import 'package:flutter/material.dart';
import '../../theme/app_colors.dart';
import '../../widgets/app_button.dart';
import '../../widgets/status_badge.dart';
import '../../widgets/quick_action_item.dart';

/// Màn hình Trang chủ chính của ứng dụng WorkShift
class HomeView extends StatelessWidget {
  final String? userName;

  const HomeView({super.key, this.userName});

  @override
  Widget build(BuildContext context) {
    final displayName = userName ?? 'Minh';

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Row(
          children: [
            const Text(
              'WorkShift',
              style: TextStyle(
                color: AppColors.primary,
                fontWeight: FontWeight.w800,
                fontSize: 22,
              ),
            ),
            const SizedBox(width: 8),
            StatusBadge.branch(text: 'Nguyễn Trãi'),
          ],
        ),
        actions: [
          IconButton(
            onPressed: () {},
            icon: const Icon(Icons.notifications_outlined),
          ),
          Padding(
            padding: const EdgeInsets.only(right: 16),
            child: CircleAvatar(
              radius: 18,
              backgroundColor: AppColors.primaryLight,
              child: const Icon(Icons.person, color: AppColors.primary),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Lời chào
            const Text(
              'Thứ Ba, 06/10/2026',
              style: TextStyle(fontSize: 13, color: AppColors.textMuted),
            ),
            const SizedBox(height: 4),
            Text(
              'Chào buổi sáng, $displayName!',
              style: const TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 16),

            // Card Ca làm hôm nay
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Container(
                              width: 8,
                              height: 8,
                              decoration: const BoxDecoration(
                                color: AppColors.warning,
                                shape: BoxShape.circle,
                              ),
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'Ca làm hôm nay',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                fontSize: 15,
                                color: AppColors.textPrimary,
                              ),
                            ),
                          ],
                        ),
                        StatusBadge.upcoming(text: 'Sắp đến ca'),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text(
                          'Ca sáng • Pha chế',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textSecondary,
                          ),
                        ),
                        StatusBadge.neutral(text: 'Chưa chấm công'),
                      ],
                    ),
                    const SizedBox(height: 8),
                    const Row(
                      children: [
                        Icon(Icons.access_time_filled, color: AppColors.primary, size: 22),
                        SizedBox(width: 8),
                        Text(
                          '08:00 – 16:00',
                          style: TextStyle(
                            fontSize: 26,
                            fontWeight: FontWeight.w800,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                      decoration: BoxDecoration(
                        color: AppColors.background,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: const Row(
                        children: [
                          Icon(Icons.location_on_outlined, size: 16, color: AppColors.textMuted),
                          SizedBox(width: 6),
                          Expanded(
                            child: Text(
                              'Chi nhánh Nguyễn Trãi (Khu vực quầy chính)',
                              style: TextStyle(fontSize: 12, color: AppColors.textSecondary),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),
                    AppButton(
                      text: 'Chấm công vào',
                      icon: Icons.mark_email_read_outlined,
                      onPressed: () {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(content: Text('Đang kích hoạt quy trình chấm công!')),
                        );
                      },
                    ),
                    const SizedBox(height: 8),
                    Center(
                      child: AppButton(
                        text: 'Xem chi tiết ca',
                        type: AppButtonType.text,
                        icon: Icons.arrow_forward,
                        onPressed: () {},
                      ),
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 24),
            // Thao tác nhanh
            const Text(
              'Thao tác nhanh',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: QuickActionItem(
                    title: 'Tạo đơn hàng',
                    subtitle: 'Bán tại quầy',
                    icon: Icons.receipt_long_outlined,
                    iconColor: AppColors.actionOrderIcon,
                    iconBgColor: AppColors.actionOrderBg,
                    onTap: () {},
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: QuickActionItem(
                    title: 'Đăng ký ca',
                    subtitle: 'Tuần tới',
                    icon: Icons.calendar_month_outlined,
                    iconColor: AppColors.actionRegisterIcon,
                    iconBgColor: AppColors.actionRegisterBg,
                    onTap: () {},
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: QuickActionItem(
                    title: 'Đổi ca',
                    subtitle: 'Tìm đồng nghiệp...',
                    icon: Icons.swap_horiz_rounded,
                    iconColor: AppColors.actionSwapIcon,
                    iconBgColor: AppColors.actionSwapBg,
                    onTap: () {},
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: QuickActionItem(
                    title: 'Gửi yêu cầu',
                    subtitle: 'Nghỉ phép, bù',
                    icon: Icons.edit_note_rounded,
                    iconColor: AppColors.actionLeaveIcon,
                    iconBgColor: AppColors.actionLeaveBg,
                    onTap: () {},
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: 0,
        items: const [
          BottomNavigationBarItem(icon: Icon(Icons.home_outlined), label: 'Trang chủ'),
          BottomNavigationBarItem(icon: Icon(Icons.calendar_today_outlined), label: 'Lịch làm'),
          BottomNavigationBarItem(icon: Icon(Icons.assignment_outlined), label: 'Đơn hàng'),
          BottomNavigationBarItem(icon: Icon(Icons.description_outlined), label: 'Yêu cầu'),
          BottomNavigationBarItem(icon: Icon(Icons.person_outline), label: 'Cá nhân'),
        ],
      ),
    );
  }
}
