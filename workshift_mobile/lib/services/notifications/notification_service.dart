import 'package:dio/dio.dart';
import '../../models/notifications/notification_model.dart';
import '../core/api_client.dart';

/// Dịch vụ gọi API thông báo cá nhân cho nhân viên
class NotificationService {
  final ApiClient _apiClient = ApiClient();

  /// Lấy danh sách thông báo cá nhân: GET /api/notifications/personal
  Future<List<NotificationModel>> getMyNotifications({
    int page = 1,
    int pageSize = 20,
    String filter = 'all',
    String sortBy = 'CreatedAt',
    String sortOrder = 'desc',
    String? search,
  }) async {
    try {
      final queryParams = <String, dynamic>{
        'Page': page,
        'PageSize': pageSize,
        'Filter': filter,
        'SortBy': sortBy,
        'SortOrder': sortOrder,
        if (search != null && search.isNotEmpty) 'Search': search,
      };

      final response = await _apiClient.get(
        '/api/notifications/personal',
        queryParameters: queryParams,
      );

      if (response.statusCode == 200 &&
          response.data != null &&
          response.data is Map) {
        final data = response.data as Map<String, dynamic>;
        final items = data['items'] as List<dynamic>? ?? [];
        return items
            .map((e) => NotificationModel.fromJson(e as Map<String, dynamic>))
            .toList();
      }
      return [];
    } on DioException catch (e) {
      final msg = e.response?.data?['message']?.toString() ?? '';
      throw msg.isNotEmpty ? msg : 'Unable to load notifications.';
    } catch (e) {
      throw 'Unable to load notifications.';
    }
  }

  /// Lấy chi tiết một thông báo: GET /api/notifications/personal/{id}
  Future<NotificationModel?> getNotificationById(int userNotificationId) async {
    try {
      final response = await _apiClient.get(
        '/api/notifications/personal/$userNotificationId',
      );

      if (response.statusCode == 200 && response.data != null) {
        return NotificationModel.fromJson(
          response.data as Map<String, dynamic>,
        );
      }
      return null;
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) {
        return null;
      }
      final msg = e.response?.data?['message']?.toString() ?? '';
      throw msg.isNotEmpty ? msg : 'Unable to load notification detail.';
    } catch (e) {
      throw 'Unable to load notification detail.';
    }
  }

  /// Lấy số thông báo chưa đọc: GET /api/notifications/personal/unread-count
  Future<int> getUnreadCount() async {
    try {
      final response = await _apiClient.get(
        '/api/notifications/personal/unread-count',
      );

      if (response.statusCode == 200 &&
          response.data != null &&
          response.data is Map) {
        final data = response.data as Map<String, dynamic>;
        return data['count']?.toInt() ?? 0;
      }
      return 0;
    } catch (_) {
      return 0;
    }
  }

  /// Đánh dấu thông báo là đã đọc: POST /api/notifications/personal/{id}/read
  Future<bool> markAsRead(int userNotificationId) async {
    try {
      final response = await _apiClient.post(
        '/api/notifications/personal/$userNotificationId/read',
      );
      return response.statusCode == 200;
    } catch (_) {
      return false;
    }
  }
}
