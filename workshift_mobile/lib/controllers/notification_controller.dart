import 'package:flutter/material.dart';
import '../models/notification_model.dart';
import '../services/notification_service.dart';

/// Bộ não điều khiển màn hình Thông báo (quản lý danh sách, trạng thái đọc, số chưa đọc)
class NotificationController extends ChangeNotifier {
  final NotificationService _service = NotificationService();

  List<NotificationModel> _notifications = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _errorMessage;
  String? _successMessage;

  List<NotificationModel> get notifications =>
      List.unmodifiable(_notifications);
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  String? get successMessage => _successMessage;
  int get readCount => _notifications.where((n) => n.isRead).length;
  NotificationService get service => _service;

  /// Tải danh sách thông báo
  Future<void> loadNotifications({
    int page = 1,
    int pageSize = 20,
    String filter = 'all',
    String? search,
    bool silent = false,
  }) async {
    if (!silent) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final result = await _service.getMyNotifications(
        page: page,
        pageSize: pageSize,
        filter: filter,
        search: search,
      );
      _notifications = result;
      _isLoading = false;
      notifyListeners();
      await loadUnreadCount(silent: true);
    } catch (e) {
      _errorMessage = e.toString();
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Tải số thông báo chưa đọc
  Future<void> loadUnreadCount({bool silent = false}) async {
    try {
      final count = await _service.getUnreadCount();
      _unreadCount = count;
      if (!silent) notifyListeners();
    } catch (_) {
      _unreadCount = 0;
    }
  }

  /// Đánh dấu một thông báo là đã đọc
  Future<bool> markAsRead(int userNotificationId) async {
    try {
      final success = await _service.markAsRead(userNotificationId);
      if (success) {
        _notifications = _notifications.map((n) {
          if (n.userNotificationId == userNotificationId) {
            return NotificationModel(
              userNotificationId: n.userNotificationId,
              notificationId: n.notificationId,
              title: n.title,
              message: n.message,
              notificationType: n.notificationType,
              audience: n.audience,
              channels: n.channels,
              createdAt: n.createdAt,
              isRead: true,
              readAt: DateTime.now(),
              page: n.page,
              pageSize: n.pageSize,
              totalCount: n.totalCount,
              targetRegionId: n.targetRegionId,
              targetBranchId: n.targetBranchId,
              targetRoles: n.targetRoles,
              createdByName: n.createdByName,
              inAppRecipientCount: n.inAppRecipientCount,
            );
          }
          return n;
        }).toList();
        if (_unreadCount > 0) _unreadCount--;
        notifyListeners();
      }
      return success;
    } catch (_) {
      return false;
    }
  }

  /// Đánh dấu tất cả là đã đọc
  Future<void> markAllAsRead() async {
    for (final n in _notifications.where((n) => !n.isRead)) {
      await markAsRead(n.userNotificationId);
    }
  }

  /// Xóa thông báo lỗi
  void clearError() {
    if (_errorMessage != null) {
      _errorMessage = null;
      notifyListeners();
    }
  }

  /// Xóa thông báo thành công
  void clearSuccess() {
    if (_successMessage != null) {
      _successMessage = null;
      notifyListeners();
    }
  }
}
