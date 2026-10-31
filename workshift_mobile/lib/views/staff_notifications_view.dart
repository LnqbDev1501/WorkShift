import 'dart:async';
import 'package:flutter/material.dart';
import '../../controllers/notification_controller.dart';
import '../../models/notification_model.dart';
import '../../services/realtime_service.dart';
import '../../theme/app_colors.dart';
import 'staff_notification_detail_view.dart';

/// Màn hình Thông báo của nhân viên - hiển thị danh sách thông báo in-app
class StaffNotificationsView extends StatefulWidget {
  const StaffNotificationsView({super.key});

  @override
  State<StaffNotificationsView> createState() => _StaffNotificationsViewState();
}

class _StaffNotificationsViewState extends State<StaffNotificationsView> {
  final NotificationController _controller = NotificationController();
  final TextEditingController _searchController = TextEditingController();
  String _searchQuery = '';
  Timer? _autoSyncTimer;
  StreamSubscription? _realtimeSubscription;

  @override
  void initState() {
    super.initState();
    _controller.loadNotifications();
    _searchController.addListener(() {
      setState(() {
        _searchQuery = _searchController.text;
      });
      _controller.loadNotifications(search: _searchQuery);
    });

    // 1. Tự động cập nhật ngay khi có thông báo Realtime từ Backend SignalR
    _realtimeSubscription = RealtimeService.instance.onNotification.listen((_) {
      if (mounted && _searchQuery.isEmpty) {
        _controller.loadNotifications(silent: true);
      }
    });

    // 2. Tự động sync ngầm định kỳ (mỗi 6s) để luôn cập nhật thông báo mới
    _autoSyncTimer = Timer.periodic(const Duration(seconds: 6), (_) {
      if (mounted && !_controller.isLoading && _searchQuery.isEmpty) {
        _controller.loadNotifications(silent: true);
      }
    });
  }

  @override
  void dispose() {
    _autoSyncTimer?.cancel();
    _realtimeSubscription?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _onRefresh() async {
    await _controller.loadNotifications(search: _searchQuery);
  }

  IconData _getTypeIcon(String type) {
    final t = type.toLowerCase();
    if (t.contains('warning') || t.contains('termination')) {
      return Icons.warning_amber_rounded;
    }
    switch (t) {
      case 'shift':
        return Icons.calendar_today_rounded;
      case 'request':
        return Icons.description_rounded;
      case 'schedule':
        return Icons.schedule_rounded;
      case 'system':
        return Icons.info_rounded;
      case 'alert':
        return Icons.warning_rounded;
      case 'payroll':
        return Icons.payments_rounded;
      default:
        return Icons.notifications_rounded;
    }
  }

  Color _getTypeColor(String type) {
    final t = type.toLowerCase();
    if (t.contains('warning') || t.contains('termination')) {
      return AppColors.warning;
    }
    switch (t) {
      case 'shift':
        return AppColors.primary;
      case 'request':
        return AppColors.actionLeaveIcon;
      case 'schedule':
        return AppColors.actionSwapIcon;
      case 'system':
        return AppColors.info;
      case 'alert':
        return AppColors.warning;
      case 'payroll':
        return AppColors.primary;
      default:
        return AppColors.textSecondary;
    }
  }

  String _formatTime(DateTime? dateTime) {
    if (dateTime == null) return '';
    final now = DateTime.now();
    final diff = now.difference(dateTime);
    if (diff.inDays > 0) return '${diff.inDays} ngày trước';
    if (diff.inHours > 0) return '${diff.inHours} giờ trước';
    if (diff.inMinutes > 0) return '${diff.inMinutes} phút trước';
    return 'Vừa xong';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Thông báo',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
        ),
        centerTitle: false,
        elevation: 0,
        backgroundColor: AppColors.background,
        foregroundColor: AppColors.textPrimary,
        actions: [
          if (_controller.unreadCount > 0)
            TextButton.icon(
              onPressed: () async {
                await _controller.markAllAsRead();
                if (mounted) setState(() {});
              },
              icon: const Icon(Icons.done_all_rounded, size: 18),
              label: const Text(
                'Đánh dấu tất cả đã đọc',
                style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
              ),
              style: TextButton.styleFrom(foregroundColor: AppColors.primary),
            ),
        ],
      ),
      body: Column(
        children: [
          // Search bar
          Container(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            color: AppColors.background,
            child: TextField(
              controller: _searchController,
              onChanged: (value) {
                setState(() {
                  _searchQuery = value;
                });
                _controller.loadNotifications(search: value);
              },
              decoration: InputDecoration(
                hintText: 'Tìm kiếm tiêu đề hoặc nội dung...',
                hintStyle: const TextStyle(
                  fontSize: 13,
                  color: AppColors.textMuted,
                ),
                prefixIcon: const Icon(
                  Icons.search_rounded,
                  size: 20,
                  color: AppColors.textMuted,
                ),
                suffixIcon: _searchQuery.isNotEmpty
                    ? IconButton(
                        icon: const Icon(
                          Icons.clear_rounded,
                          size: 18,
                          color: AppColors.textMuted,
                        ),
                        onPressed: () {
                          _searchController.clear();
                          setState(() {
                            _searchQuery = '';
                          });
                          _controller.loadNotifications(search: '');
                        },
                        constraints: const BoxConstraints(),
                        padding: EdgeInsets.zero,
                      )
                    : null,
                filled: true,
                fillColor: AppColors.surface,
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: AppColors.border),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide(color: AppColors.border),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: const BorderSide(
                    color: AppColors.primary,
                    width: 1.5,
                  ),
                ),
              ),
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _onRefresh,
              color: AppColors.primary,
              child: ListenableBuilder(
                listenable: _controller,
                builder: (context, _) {
                  if (_controller.isLoading &&
                      _controller.notifications.isEmpty) {
                    return const Center(
                      child: CircularProgressIndicator(
                        strokeWidth: 2.5,
                        color: AppColors.primary,
                      ),
                    );
                  }

                  if (_controller.errorMessage != null &&
                      _controller.notifications.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(
                            Icons.error_outline_rounded,
                            size: 48,
                            color: AppColors.error,
                          ),
                          const SizedBox(height: 16),
                          Text(
                            _controller.errorMessage!,
                            style: const TextStyle(
                              fontSize: 14,
                              color: AppColors.error,
                              fontWeight: FontWeight.w600,
                            ),
                            textAlign: TextAlign.center,
                          ),
                          const SizedBox(height: 16),
                          ElevatedButton.icon(
                            onPressed: () {
                              _controller.clearError();
                              _controller.loadNotifications(
                                search: _searchQuery,
                              );
                            },
                            icon: const Icon(Icons.refresh_rounded, size: 18),
                            label: const Text('Thử lại'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppColors.primary,
                              foregroundColor: Colors.white,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10),
                              ),
                            ),
                          ),
                        ],
                      ),
                    );
                  }

                  final notifications = _controller.notifications;
                  if (notifications.isEmpty) {
                    return Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Container(
                            padding: const EdgeInsets.all(24),
                            decoration: BoxDecoration(
                              color: AppColors.infoBg,
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(
                              Icons.notifications_none_rounded,
                              size: 48,
                              color: AppColors.info,
                            ),
                          ),
                          const SizedBox(height: 16),
                          Text(
                            _searchQuery.isNotEmpty
                                ? 'Không tìm thấy thông báo'
                                : 'Không có thông báo',
                            style: const TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.w700,
                              color: AppColors.textPrimary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            _searchQuery.isNotEmpty
                                ? 'Thử từ khóa tìm kiếm khác.'
                                : 'Bạn chưa có thông báo nào.',
                            style: TextStyle(
                              fontSize: 14,
                              color: AppColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                    );
                  }

                  return ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: notifications.length,
                    separatorBuilder: (_, index) => const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      final notification = notifications[index];
                      return _buildNotificationItem(notification);
                    },
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNotificationItem(NotificationModel notification) {
    final isUnread = !notification.isRead;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () async {
          // Đánh dấu đã đọc khi tap vào thông báo
          if (isUnread) {
            await _controller.markAsRead(notification.userNotificationId);
            if (mounted) setState(() {});
          }
          // Điều hướng đến màn hình chi tiết
          if (mounted) {
            await Navigator.of(context).push(
              MaterialPageRoute(
                builder: (_) => StaffNotificationDetailView(
                  userNotificationId: notification.userNotificationId,
                ),
              ),
            );
            // Refresh danh sách sau khi quay lại
            if (mounted) {
              await _controller.loadNotifications(search: _searchQuery);
            }
          }
        },
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: isUnread
                  ? AppColors.primary.withValues(alpha: 0.3)
                  : AppColors.border,
              width: 1,
            ),
            boxShadow: [
              BoxShadow(
                color: AppColors.primary.withValues(
                  alpha: isUnread ? 0.06 : 0.02,
                ),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: _getTypeColor(
                    notification.notificationType,
                  ).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(
                  _getTypeIcon(notification.notificationType),
                  color: _getTypeColor(notification.notificationType),
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: isUnread
                                  ? FontWeight.w700
                                  : FontWeight.w500,
                              color: AppColors.textPrimary,
                            ),
                          ),
                        ),
                        if (isUnread)
                          Container(
                            width: 8,
                            height: 8,
                            decoration: BoxDecoration(
                              color: AppColors.primary,
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      _formatTime(notification.createdAt),
                      style: TextStyle(
                        fontSize: 12,
                        color: isUnread
                            ? AppColors.textSecondary
                            : AppColors.textMuted,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 8,
                        vertical: 4,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.borderLight,
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(
                        notification.notificationType.toUpperCase(),
                        style: const TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textMuted,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              if (isUnread)
                IconButton(
                  onPressed: () async {
                    await _controller.markAsRead(
                      notification.userNotificationId,
                    );
                    if (mounted) setState(() {});
                  },
                  icon: const Icon(
                    Icons.check_circle_outline_rounded,
                    size: 20,
                    color: AppColors.success,
                  ),
                  constraints: const BoxConstraints(),
                  padding: EdgeInsets.zero,
                  splashRadius: 20,
                ),
            ],
          ),
        ),
      ),
    );
  }
}
