import 'package:flutter/material.dart';
import '../../controllers/home_controller.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import '../auth/login_view.dart';
import 'components/documents_section.dart';
import 'components/home_header.dart';
import 'components/pending_tasks.dart';
import 'components/quick_actions.dart';
import 'components/today_shift_card.dart';
import 'components/upcoming_shifts.dart';

/// Màn hình Trang chủ chính của ứng dụng WorkShift (Đầy đủ tất cả các khu vực theo thiết kế)
class HomeView extends StatefulWidget {
  final String? userName;

  const HomeView({super.key, this.userName});

  @override
  State<HomeView> createState() => _HomeViewState();
}

class _HomeViewState extends State<HomeView> {
  final HomeController _controller = HomeController();
  final AuthService _authService = AuthService();
  int _currentTabIndex = 0;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_onControllerUpdated);
    // Tải dữ liệu từ Backend .NET
    _controller.loadHomeData();
  }

  @override
  void dispose() {
    _controller.removeListener(_onControllerUpdated);
    super.dispose();
  }

  void _onControllerUpdated() {
    if (mounted) setState(() {});
  }

  Future<void> _handleLogout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: const Row(
          children: [
            Icon(Icons.logout_rounded, color: AppColors.error),
            SizedBox(width: 8),
            Text(
              'Logout',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: const Text(
          'Are you sure you want to log out of this account?',
          style: TextStyle(fontSize: 14),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Cancel',
              style: TextStyle(color: AppColors.textSecondary),
            ),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.error,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(10),
              ),
            ),
            child: const Text('Logout'),
          ),
        ],
      ),
    );

    if (confirm == true && mounted) {
      await _authService.logout();
      if (mounted) {
        Navigator.pushAndRemoveUntil(
          context,
          MaterialPageRoute(builder: (_) => const LoginView()),
          (route) => false,
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _controller.loadHomeData,
          color: AppColors.primary,
          child: _controller.isLoading
              ? const Center(
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: AppColors.primary,
                  ),
                )
              : SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // 1. Tiêu đề & Lời chào (Chào buổi sáng, Minh!)
                      HomeHeader(
                        userName: _controller.displayName,
                        branchName: _controller.branchName,
                        avatarUrl: _controller.user?.avatarUrl,
                        onLogoutTap: _handleLogout,
                      ),
                      const SizedBox(height: 16),

                      // 2. Thẻ Ca làm hôm nay (08:00 – 16:00, Chấm công vào)
                      TodayShiftCard(
                        shift: _controller.todayShift,
                        onCheckInTap: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(
                                _controller.todayShift?.canCheckInNow == true
                                    ? 'Opening Face / GPS check-in verification...'
                                    : (_controller.todayShift?.canNotCheckInReason ??
                                        'Outside of check-in time window!'),
                              ),
                            ),
                          );
                        },
                        onViewDetailTap: () {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Viewing shift details')),
                          );
                        },
                      ),
                      const SizedBox(height: 24),

                      // 3. Quick Actions
                      const QuickActions(),
                      const SizedBox(height: 24),

                      // 4. Pending Tasks
                      const PendingTasks(),
                      const SizedBox(height: 24),

                      // 5. Upcoming Schedule
                      const UpcomingShifts(),
                      const SizedBox(height: 24),

                      // 6. Documents & Records
                      const DocumentsSection(),
                      const SizedBox(height: 32),
                    ],
                  ),
                ),
        ),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentTabIndex,
        onTap: (index) {
          setState(() {
            _currentTabIndex = index;
          });
        },
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.home_outlined),
            activeIcon: Icon(Icons.home_filled),
            label: 'Home',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.assignment_outlined),
            activeIcon: Icon(Icons.assignment),
            label: 'Timesheet',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.calendar_today_outlined),
            activeIcon: Icon(Icons.calendar_month),
            label: 'Schedule',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.description_outlined),
            activeIcon: Icon(Icons.description),
            label: 'Requests',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person_outline),
            activeIcon: Icon(Icons.person),
            label: 'Profile',
          ),
        ],
      ),
    );
  }
}
