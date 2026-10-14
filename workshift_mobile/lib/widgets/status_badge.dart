import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

/// Thẻ trạng thái nhỏ (Badge) như trong thiết kế: "Sắp đến ca", "Chưa chấm công"
class StatusBadge extends StatelessWidget {
  final String text;
  final Color backgroundColor;
  final Color textColor;
  final IconData? icon;
  final bool hasDot;
  final Color? dotColor;

  const StatusBadge({
    super.key,
    required this.text,
    this.backgroundColor = AppColors.borderLight,
    this.textColor = AppColors.textSecondary,
    this.icon,
    this.hasDot = false,
    this.dotColor,
  });

  /// Factory cho trạng thái "Sắp đến ca" (màu cam nhạt)
  factory StatusBadge.upcoming({required String text}) {
    return StatusBadge(
      text: text,
      icon: Icons.timer_outlined,
      backgroundColor: AppColors.warningBg,
      textColor: AppColors.warning,
    );
  }

  /// Factory cho trạng thái "In Progress / Đang làm việc"
  factory StatusBadge.warning({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.warning,
      backgroundColor: AppColors.warningBg,
      textColor: AppColors.warning,
    );
  }

  /// Factory cho trạng thái "Completed / Hoàn thành"
  factory StatusBadge.success({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.success,
      backgroundColor: const Color(0xFFDCFCE7),
      textColor: const Color(0xFF15803D),
    );
  }

  /// Factory cho trạng thái "Chưa chấm công" (chấm xám + chữ xám)
  factory StatusBadge.neutral({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.textMuted,
      backgroundColor: AppColors.borderLight,
      textColor: AppColors.textSecondary,
    );
  }

  /// Factory cho thông tin Chi nhánh (chấm xanh lá + nền xanh nhạt)
  factory StatusBadge.branch({required String text}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.success,
      backgroundColor: AppColors.primaryLight,
      textColor: AppColors.primaryDark,
    );
  }

  /// Factory cho trạng thái "Chờ duyệt / Pending" (màu cam vàng)
  factory StatusBadge.pending({String text = 'Pending'}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.warning,
      backgroundColor: AppColors.warningBg,
      textColor: AppColors.warning,
    );
  }

  /// Factory cho trạng thái "Đã duyệt / Approved" (màu xanh lá)
  factory StatusBadge.approved({String text = 'Approved'}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.success,
      backgroundColor: AppColors.successBg,
      textColor: const Color(0xFF059669),
    );
  }

  /// Factory cho trạng thái "Bị từ chối / Rejected" (màu đỏ)
  factory StatusBadge.rejected({String text = 'Rejected'}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.error,
      backgroundColor: AppColors.errorBg,
      textColor: AppColors.error,
    );
  }

  /// Factory cho trạng thái "Đã hủy / Cancelled" (màu xám)
  factory StatusBadge.cancelled({String text = 'Cancelled'}) {
    return StatusBadge(
      text: text,
      hasDot: true,
      dotColor: AppColors.textMuted,
      backgroundColor: AppColors.borderLight,
      textColor: AppColors.textSecondary,
    );
  }

  /// Factory động theo mã trạng thái của Request từ Backend
  factory StatusBadge.forRequestStatus(String status) {
    switch (status.toUpperCase()) {
      case 'PENDING':
        return StatusBadge.pending(text: 'Pending');
      case 'APPROVED':
        return StatusBadge.approved(text: 'Approved');
      case 'REJECTED':
        return StatusBadge.rejected(text: 'Rejected');
      case 'CANCELLED':
        return StatusBadge.cancelled(text: 'Cancelled');
      default:
        return StatusBadge.neutral(text: status);
    }
  }

  /// Factory động theo trạng thái của Hợp đồng lao động
  factory StatusBadge.forContractStatus(String status) {
    switch (status.toUpperCase()) {
      case 'PENDING_STAFF':
        return StatusBadge(
          text: 'Chờ ký',
          hasDot: true,
          dotColor: AppColors.warning,
          backgroundColor: AppColors.warningBg,
          textColor: AppColors.warning,
        );
      case 'ACTIVE':
        return StatusBadge(
          text: 'Có hiệu lực',
          hasDot: true,
          dotColor: AppColors.success,
          backgroundColor: AppColors.successBg,
          textColor: const Color(0xFF059669),
        );
      case 'SCHEDULED':
        return StatusBadge(
          text: 'Sắp bắt đầu',
          hasDot: true,
          dotColor: AppColors.info,
          backgroundColor: AppColors.infoBg,
          textColor: AppColors.info,
        );
      case 'EXPIRED':
        return StatusBadge(
          text: 'Đã hết hạn',
          hasDot: true,
          dotColor: AppColors.textMuted,
          backgroundColor: AppColors.borderLight,
          textColor: AppColors.textSecondary,
        );
      case 'CANCELLED':
        return StatusBadge(
          text: 'Đã hủy',
          hasDot: true,
          dotColor: AppColors.error,
          backgroundColor: AppColors.errorBg,
          textColor: AppColors.error,
        );
      case 'TERMINATED':
        return StatusBadge(
          text: 'Đã chấm dứt',
          hasDot: true,
          dotColor: AppColors.error,
          backgroundColor: AppColors.errorBg,
          textColor: AppColors.error,
        );
      default:
        return StatusBadge.neutral(text: status);
    }
  }

  /// Factory cho trạng thái của Yêu cầu thay đổi hợp đồng (Contract Change Request)
  factory StatusBadge.forChangeRequestStatus(String status) {
    switch (status.toUpperCase()) {
      case 'SUBMITTED':
        return StatusBadge(
          text: 'Đang xem xét',
          hasDot: true,
          dotColor: AppColors.warning,
          backgroundColor: AppColors.warningBg,
          textColor: AppColors.warning,
        );
      case 'APPROVED':
        return StatusBadge(
          text: 'Đã cập nhật',
          hasDot: true,
          dotColor: AppColors.success,
          backgroundColor: AppColors.successBg,
          textColor: const Color(0xFF059669),
        );
      case 'REJECTED':
        return StatusBadge(
          text: 'Từ chối sửa',
          hasDot: true,
          dotColor: AppColors.error,
          backgroundColor: AppColors.errorBg,
          textColor: AppColors.error,
        );
      default:
        return StatusBadge.neutral(text: status);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 13, color: textColor),
            const SizedBox(width: 4),
          ],
          if (hasDot) ...[
            Container(
              width: 6,
              height: 6,
              decoration: BoxDecoration(
                color: dotColor ?? textColor,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 5),
          ],
          Text(
            text,
            style: TextStyle(
              color: textColor,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}
