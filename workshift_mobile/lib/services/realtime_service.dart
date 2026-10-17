import 'dart:async';
import 'dart:convert';
import 'dart:developer';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'api_client.dart';
import 'auth_service.dart';

/// Global Navigation Key để hiển thị thông báo In-App toàn hệ thống
final GlobalKey<NavigatorState> rootNavigatorKey = GlobalKey<NavigatorState>();
final GlobalKey<ScaffoldMessengerState> rootScaffoldMessengerKey =
    GlobalKey<ScaffoldMessengerState>();

/// Model thông báo realtime nhận từ backend qua SignalR
class RealtimeNotificationPayload {
  final int notificationId;
  final String title;
  final String message;
  final DateTime createdAt;

  RealtimeNotificationPayload({
    required this.notificationId,
    required this.title,
    required this.message,
    required this.createdAt,
  });

  factory RealtimeNotificationPayload.fromMap(Map<String, dynamic> map) {
    return RealtimeNotificationPayload(
      notificationId: (map['notificationId'] ?? map['NotificationId'] ?? 0) as int,
      title: (map['title'] ?? map['Title'] ?? '') as String,
      message: (map['message'] ?? map['Message'] ?? '') as String,
      createdAt: map['createdAt'] != null || map['CreatedAt'] != null
          ? DateTime.tryParse((map['createdAt'] ?? map['CreatedAt']).toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}

/// Dịch vụ kết nối SignalR Realtime - Tự động cập nhật dữ liệu khi Backend có thay đổi
class RealtimeService {
  static final RealtimeService _instance = RealtimeService._internal();
  static RealtimeService get instance => _instance;

  RealtimeService._internal();

  HubConnection? _hubConnection;
  bool _isConnecting = false;
  String? _registeredUserId;
  String? get registeredUserId => _registeredUserId;

  final StreamController<RealtimeNotificationPayload> _notificationStreamController =
      StreamController<RealtimeNotificationPayload>.broadcast();

  Stream<RealtimeNotificationPayload> get onNotification =>
      _notificationStreamController.stream;

  /// Khởi động kết nối SignalR Hub
  Future<void> connect() async {
    if (_hubConnection != null &&
        _hubConnection!.state == HubConnectionState.Connected) {
      return;
    }
    if (_isConnecting) return;
    _isConnecting = true;

    try {
      final authService = AuthService();
      final savedUser = await authService.getSavedUser();
      final userId = savedUser?['userId']?.toString();
      if (userId == null || userId.isEmpty) {
        _isConnecting = false;
        return;
      }

      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString(ApiClient.tokenKey);

      final baseUrl = ApiClient.defaultBaseUrl.trim();
      final hubUrl = '$baseUrl/hubs/notifications';

      log('🔄 [SignalR] Connecting to $hubUrl for user $userId...');

      final httpOptions = HttpConnectionOptions(
        accessTokenFactory: token != null ? () async => token : null,
      );

      _hubConnection = HubConnectionBuilder()
          .withUrl(hubUrl, options: httpOptions)
          .withAutomaticReconnect(retryDelays: [2000, 5000, 10000, 20000])
          .build();

      _hubConnection!.onclose(({error}) {
        log('⚠️ [SignalR] Connection closed: $error');
      });

      _hubConnection!.onreconnected(({connectionId}) {
        log('✅ [SignalR] Reconnected. Re-registering user: $userId');
        _registerUser(userId);
      });

      // Lắng nghe sự kiện NotificationReceived từ backend
      _hubConnection!.on('NotificationReceived', (arguments) {
        log('📢 [SignalR] NotificationReceived: $arguments');
        if (arguments != null && arguments.isNotEmpty) {
          try {
            final arg = arguments[0];
            Map<String, dynamic> data;
            if (arg is Map<String, dynamic>) {
              data = arg;
            } else if (arg is Map) {
              data = Map<String, dynamic>.from(arg);
            } else if (arg is String) {
              data = jsonDecode(arg) as Map<String, dynamic>;
            } else {
              return;
            }

            final payload = RealtimeNotificationPayload.fromMap(data);
            _notificationStreamController.add(payload);

            // Tự động hiển thị SnackBar In-App Notification nếu app đang mở
            _showInAppNotificationBanner(payload);
          } catch (e) {
            log('❌ [SignalR] Error parsing notification payload: $e');
          }
        }
      });

      await _hubConnection!.start();
      log('🟢 [SignalR] Connected successfully!');
      await _registerUser(userId);
    } catch (e) {
      log('❌ [SignalR] Connection error: $e');
    } finally {
      _isConnecting = false;
    }
  }

  Future<void> _registerUser(String userId) async {
    if (_hubConnection == null ||
        _hubConnection!.state != HubConnectionState.Connected) {
      return;
    }
    try {
      await _hubConnection!.invoke('RegisterUser', args: [userId]);
      _registeredUserId = userId;
      log('👤 [SignalR] Registered user $userId successfully on Hub');
    } catch (e) {
      log('❌ [SignalR] RegisterUser error: $e');
    }
  }

  /// Hiển thị Banner thông báo nổi bật khi nhận được dữ liệu realtime
  void _showInAppNotificationBanner(RealtimeNotificationPayload payload) {
    final messenger = rootScaffoldMessengerKey.currentState;
    if (messenger == null) return;

    final isSpecialShift = payload.title.contains('Ca làm việc') ||
        payload.message.contains('nhận ca') ||
        payload.message.contains('Barista') ||
        payload.message.contains('làm thay');

    messenger.showSnackBar(
      SnackBar(
        duration: const Duration(seconds: 6),
        behavior: SnackBarBehavior.floating,
        margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        backgroundColor: isSpecialShift ? const Color(0xFF1E1B4B) : const Color(0xFF1F2937),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(
            color: isSpecialShift ? const Color(0xFF6366F1) : const Color(0xFF4B5563),
            width: 1.2,
          ),
        ),
        content: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: isSpecialShift
                    ? const Color(0xFF6366F1).withValues(alpha: 0.2)
                    : Colors.white10,
                shape: BoxShape.circle,
              ),
              child: Icon(
                isSpecialShift
                    ? Icons.event_available_rounded
                    : Icons.notifications_active_rounded,
                color: isSpecialShift ? const Color(0xFFA5B4FC) : Colors.amber,
                size: 22,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    payload.title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 14,
                      color: Colors.white,
                    ),
                  ),
                  const SizedBox(height: 3),
                  Text(
                    payload.message,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 12,
                      color: Color(0xFFD1D5DB),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// Ngắt kết nối khi người dùng đăng xuất
  Future<void> disconnect() async {
    try {
      if (_hubConnection != null) {
        await _hubConnection!.stop();
        _hubConnection = null;
      }
      _registeredUserId = null;
    } catch (_) {}
  }
}
