import 'package:flutter/material.dart';
import '../../controllers/notification_controller.dart';
import '../../models/notification_model.dart';
import '../../theme/app_colors.dart';
import 'shifts/available_shifts_view.dart';
import 'payroll/payroll_home_view.dart';

/// Màn hình chi tiết thông báo - hiển thị toàn bộ nội dung thư thông báo
class StaffNotificationDetailView extends StatefulWidget {
  final int userNotificationId;

  const StaffNotificationDetailView({
    super.key,
    required this.userNotificationId,
  });

  @override
  State<StaffNotificationDetailView> createState() =>
      _StaffNotificationDetailViewState();
}

class _StaffNotificationDetailViewState
    extends State<StaffNotificationDetailView> {
  final NotificationController _controller = NotificationController();
  NotificationModel? _notification;
  bool _isLoading = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  Future<void> _loadDetail() async {
    try {
      final detail = await _controller.service.getNotificationById(
        widget.userNotificationId,
      );
      if (mounted) {
        setState(() {
          _notification = detail;
          _isLoading = false;
        });
      }
      // Đánh dấu đã đọc khi mở chi tiết
      if (detail != null && !detail.isRead && mounted) {
        await _controller.markAsRead(detail.userNotificationId);
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString();
          _isLoading = false;
        });
      }
    }
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

  String _formatDateTime(DateTime dateTime) {
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
          'Chi tiết thông báo',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700),
        ),
        centerTitle: false,
        elevation: 0,
        backgroundColor: AppColors.background,
        foregroundColor: AppColors.textPrimary,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _isLoading
          ? const Center(
              child: CircularProgressIndicator(
                strokeWidth: 2.5,
                color: AppColors.primary,
              ),
            )
          : _errorMessage != null
          ? Center(
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
                    _errorMessage!,
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
                      setState(() {
                        _isLoading = true;
                        _errorMessage = null;
                      });
                      _loadDetail();
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
            )
          : _notification == null
          ? const Center(child: Text('Không tìm thấy thông báo.'))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Header info - Title + Meta below
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.border),
                      boxShadow: [
                        BoxShadow(
                          color: AppColors.primary.withValues(alpha: 0.03),
                          blurRadius: 12,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: _getTypeColor(
                                  _notification!.notificationType,
                                ).withValues(alpha: 0.1),
                                borderRadius: BorderRadius.circular(10),
                              ),
                              child: Icon(
                                _getTypeIcon(_notification!.notificationType),
                                color: _getTypeColor(
                                  _notification!.notificationType,
                                ),
                                size: 24,
                              ),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    _notification!.title,
                                    style: const TextStyle(
                                      fontSize: 18,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                  const SizedBox(height: 6),
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
                                      _notification!.notificationType
                                          .toUpperCase(),
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
                            if (!_notification!.isRead)
                              Container(
                                width: 10,
                                height: 10,
                                decoration: BoxDecoration(
                                  color: AppColors.primary,
                                  shape: BoxShape.circle,
                                ),
                              ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        // Meta info: Received At, Sender, Viewed At
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              flex: 2,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Received At',
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textMuted,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    _formatDateTime(_notification!.createdAt),
                                    style: const TextStyle(
                                      fontSize: 14,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Expanded(
                              flex: 2,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Sender',
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textMuted,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    _notification!.createdByName ?? 'Hệ thống',
                                    style: const TextStyle(
                                      fontSize: 14,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textPrimary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Expanded(
                              flex: 2,
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'Viewed At',
                                    style: TextStyle(
                                      fontSize: 11,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textMuted,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    _notification!.readAt != null
                                        ? _formatDateTime(
                                            _notification!.readAt!,
                                          )
                                        : 'Chưa xem',
                                    style: const TextStyle(
                                      fontSize: 14,
                                      fontWeight: FontWeight.w500,
                                      color: AppColors.textSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  // Message content
                  const Text(
                    'Nội dung',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Text(
                      _notification!.message.isNotEmpty
                          ? _notification!.message
                                .replaceAll(
                                  'Lý do: NOT_ACCEPTED_BY_START_DATE (quá ngày bắt đầu',
                                  'Lý do: Quá ngày bắt đầu',
                                )
                                .replaceAll(
                                  'Lý do: NOT_ACCEPTED_BY_START_DATE (Quá ngày bắt đầu',
                                  'Lý do: Quá ngày bắt đầu',
                                )
                                .replaceAll(
                                  'chưa xác nhận chấp thuận).',
                                  'chưa xác nhận chấp thuận.',
                                )
                                .replaceAll(
                                  'chưa xác nhận chấp thuận)',
                                  'chưa xác nhận chấp thuận',
                                )
                                .replaceAll(
                                  'NOT_ACCEPTED_BY_START_DATE',
                                  'Quá ngày bắt đầu mà nhân viên chưa xác nhận chấp thuận',
                                )
                          : 'Không có nội dung chi tiết.',
                      style: const TextStyle(
                        fontSize: 15,
                        color: AppColors.textPrimary,
                        height: 1.5,
                      ),
                    ),
                  ),
                  if (_notification!.notificationType.toLowerCase() ==
                          'openshift' ||
                      _notification!.title.contains('nhận ca') ||
                      _notification!.message.contains('nhận ca')) ...[
                    const SizedBox(height: 20),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const AvailableShiftsView(),
                            ),
                          );
                        },
                        icon: const Icon(
                          Icons.event_available_rounded,
                          size: 20,
                        ),
                        label: const Text(
                          'Xem danh sách & Nhận ca (Pick up)',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          elevation: 2,
                        ),
                      ),
                    ),
                  ],
                  if (_notification!.notificationType.toLowerCase() ==
                          'payroll' ||
                      _notification!.title.toLowerCase().contains('payroll') ||
                      _notification!.title.toLowerCase().contains('lương') ||
                      _notification!.message.toLowerCase().contains(
                        'payroll preview',
                      ) ||
                      _notification!.message.toLowerCase().contains(
                        'bản xem trước lương',
                      ) ||
                      _notification!.message.toLowerCase().contains(
                        'phiếu lương',
                      )) ...[
                    const SizedBox(height: 20),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: () {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const PayrollHomeView(),
                            ),
                          );
                        },
                        icon: const Icon(Icons.payments_rounded, size: 20),
                        label: const Text(
                          'Xem Bảng lương & Phản hồi',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          elevation: 2,
                        ),
                      ),
                    ),
                  ],
                  const SizedBox(height: 32),
                ],
              ),
            ),
    );
  }
}
