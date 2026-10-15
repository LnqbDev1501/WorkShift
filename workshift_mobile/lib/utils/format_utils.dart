import 'package:flutter/material.dart';
import '../theme/app_colors.dart';

String formatCurrency(double amount) {
  return '${amount.toStringAsFixed(0).replaceAllMapped(RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'), (m) => '${m[1]},')} VNĐ';
}

String formatDate(DateTime date) {
  return '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year} ${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
}

Color getStatusColor(String status) {
  switch (status.toUpperCase()) {
    case 'ACTIVE':
      return AppColors.primary;
    case 'COMPLETED':
      return AppColors.success;
    case 'CANCELLED':
      return AppColors.error;
    case 'INACTIVE':
      return AppColors.textMuted;
    default:
      return AppColors.textSecondary;
  }
}

Color getPaymentStatusColor(String status) {
  switch (status.toUpperCase()) {
    case 'PAID':
      return AppColors.success;
    case 'PARTIAL':
      return AppColors.warning;
    case 'UNPAID':
      return AppColors.error;
    default:
      return AppColors.textSecondary;
  }
}

class StatusChip extends StatelessWidget {
  final String label;
  final Color color;
  final IconData? icon;

  const StatusChip({super.key, required this.label, required this.color, this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withValues(alpha: 0.3)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 12, color: color),
            const SizedBox(width: 4),
          ],
          Text(
            label.toUpperCase(),
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: color,
              letterSpacing: 0.3,
            ),
          ),
        ],
      ),
    );
  }
}

Widget buildTotalRow(String label, String value, {bool isTotal = false, Color? color}) {
  return Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: isTotal ? 16 : 14,
            fontWeight: isTotal ? FontWeight.w700 : FontWeight.w500,
            color: isTotal ? AppColors.textPrimary : AppColors.textSecondary,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            fontSize: isTotal ? 18 : 14,
            fontWeight: isTotal ? FontWeight.w800 : FontWeight.w600,
            color: color ?? (isTotal ? AppColors.primary : AppColors.textPrimary),
          ),
        ),
      ],
    ),
  );
}