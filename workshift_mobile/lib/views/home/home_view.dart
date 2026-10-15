import 'package:flutter/material.dart';
import '../../controllers/home_controller.dart';
import '../../controllers/staff_request_controller.dart';
import '../requests/staff_request_detail_view.dart';
import '../../models/staff_request_model.dart';
import '../../services/auth_service.dart';
import '../../theme/app_colors.dart';
import '../auth/login_view.dart';
import 'components/documents_section.dart';
import 'components/home_header.dart';
import 'components/pending_tasks.dart';
import 'components/quick_actions.dart';
import 'components/today_shift_card.dart';
import 'components/unfinished_shift_banner.dart';
import 'handlers/attendance_flow_handler.dart';
import '../attendance/attendance_history_view.dart';
import '../shifts/my_shifts_view.dart';

import '../face_registration/face_registration_modal.dart';
import '../requests/staff_request_list_view.dart';
import '../timesheet/staff_timesheet_view.dart';

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
  bool _hasPromptedFaceRegistration = false;

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
    if (mounted) {
      setState(() {});
      if (!_controller.isLoading &&
          _controller.isFaceNotRegistered &&
          !_hasPromptedFaceRegistration) {
        _hasPromptedFaceRegistration = true;
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) {
            _showFaceRegistrationDialog();
          }
        });
      }
    }
  }

  Future<void> _showFaceRegistrationDialog() async {
    final registerNow = await showDialog<bool>(
      context: context,
      barrierDismissible: true,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: const BoxDecoration(
                color: Color(0xFFF3E8FF),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.face_retouching_natural_rounded,
                color: Color(0xFF7C3AED),
                size: 24,
              ),
            ),
            const SizedBox(width: 10),
            const Expanded(
              child: Text(
                'Face Profile Required',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
            ),
          ],
        ),
        content: Text(
          _controller.shiftErrorMessage ??
              'Your account has not registered a biometric face profile yet. You must complete face registration before performing shift attendance.',
          style: const TextStyle(
            fontSize: 14,
            color: AppColors.textSecondary,
            height: 1.4,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text(
              'Later',
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
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
            ),
          ),
        ],
      ),
    );

    if (registerNow == true && mounted) {
      final success = await showFaceRegistrationModal(context: context);
      if (success == true && mounted) {
        await _controller.loadHomeData();
      }
    }
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

  Future<void> _openRequest(StaffEmployeeRequestListItemModel request) async {
    final requestController = StaffRequestController();
    try {
      await Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => StaffRequestDetailView(
            requestId: request.requestId,
            controller: requestController,
          ),
        ),
      );
      if (mounted) await _controller.loadHomeData(silent: true);
    } finally {
      requestController.dispose();
    }
  }

  Widget _buildPlaceholderTab(String title, IconData icon) {
    return SafeArea(
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(20),
              decoration: const BoxDecoration(
                color: Color(0xFFEFF6FF),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, size: 48, color: AppColors.primary),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            const Text(
              'Chức năng đang được cập nhật',
              style: TextStyle(fontSize: 14, color: AppColors.textSecondary),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: IndexedStack(
        index: _currentTabIndex,
        children: [
          // Tab 0: Home
          SafeArea(
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
                      padding: const EdgeInsets.symmetric(
                        horizontal: 20,
                        vertical: 20,
                      ),
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
                            isFaceNotRegistered:
                                _controller.isFaceNotRegistered,
                            onRegisterFaceTap: () async {
                              final success = await showFaceRegistrationModal(
                                context: context,
                              );
                              if (success == true && mounted) {
                                await _controller.loadHomeData();
                              }
                            },
                            onCheckInTap: () =>
                                AttendanceFlowHandler.handleCheckIn(
                                  context: context,
                                  homeController: _controller,
                                ),
                            onCheckOutTap: () =>
                                AttendanceFlowHandler.handleCheckOut(
                                  context: context,
                                  homeController: _controller,
                                ),
                            onViewAttendanceHistoryTap: () {
                              Navigator.push(
                                context,
                                MaterialPageRoute(
                                  builder: (_) => AttendanceHistoryView(
                                    homeController: _controller,
                                  ),
                                ),
                              );
                            },
                          ),

                          // 2b. Cảnh báo ca làm việc trước chưa Checkout (nếu có)
                          if (_controller.unCheckedOutPreviousShift !=
                              null) ...[
                            UnfinishedShiftWarningBanner(
                              shift: _controller.unCheckedOutPreviousShift!,
                            ),
                          ],
                          const SizedBox(height: 24),

                          // 3. Quick Actions
                          QuickActions(
                            onFaceRegistrationSuccess: () =>
                                _controller.loadHomeData(),
                            onShiftsChanged: () =>
                                _controller.loadHomeData(silent: true),
                          ),
                          const SizedBox(height: 24),

                          // 4. Most recently updated requests
                          PendingTasks(
                            requests: _controller.recentRequests,
                            errorMessage: _controller.requestsError,
                            onRequestTap: _openRequest,
                            onViewAll: () =>
                                setState(() => _currentTabIndex = 3),
                          ),
                          const SizedBox(height: 24),

                          // 5. Recorded hours for the current week
                          DocumentsSection(
                            workingMinutes: _controller.weeklyWorkingMinutes,
                            weekStart: _controller.weekStart,
                            weekEnd: _controller.weekEnd,
                            errorMessage: _controller.weeklyHoursError,
                            onViewHistory: () => Navigator.of(context).push(
                              MaterialPageRoute(
                                builder: (_) => AttendanceHistoryView(
                                  homeController: _controller,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(height: 32),
                        ],
                      ),
                    ),
            ),
          ),
          // Tab 1: Timesheet
          const StaffTimesheetView(),
          // Tab 2: Schedule
          _currentTabIndex == 2
              ? const MyShiftsView(showBackButton: false)
              : const SizedBox.shrink(),
          // Tab 3: Requests
          const StaffRequestListView(showBackButton: false),
          // Tab 4: Profile
          _buildPlaceholderTab('Profile', Icons.person_outline),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        type: BottomNavigationBarType.fixed,
        elevation: 0,
        backgroundColor: AppColors.surface,
        selectedItemColor: AppColors.primary,
        unselectedItemColor: AppColors.textSecondary,
        selectedFontSize: 11,
        unselectedFontSize: 11,
        selectedLabelStyle: const TextStyle(fontWeight: FontWeight.w700),
        currentIndex: _currentTabIndex,
        onTap: (index) {
          final returningFromSchedule = index == 0 && _currentTabIndex != 0;
          setState(() {
            _currentTabIndex = index;
          });
          if (returningFromSchedule) _controller.loadHomeData(silent: true);
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
