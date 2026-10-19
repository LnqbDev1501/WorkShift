import 'package:flutter/material.dart';
import '../../../models/attendance_shift_model.dart';

/// Banner cảnh báo khi nhân viên có ca làm việc trước đó chưa Checkout
class UnfinishedShiftWarningBanner extends StatelessWidget {
  final EligibleShiftModel shift;

  const UnfinishedShiftWarningBanner({super.key, required this.shift});

  String _formatTime(DateTime? dt) {
    if (dt == null) return '--:--';
    final local = dt.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(top: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFFF7ED), // Warm amber background
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: const Color(0xFFFDBA74),
          width: 1.2,
        ), // Amber border
        boxShadow: [
          BoxShadow(
            color: Colors.orange.withValues(alpha: 0.06),
            blurRadius: 10,
            offset: const Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: const Color(0xFFFFEDD5),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(
                  Icons.warning_amber_rounded,
                  color: Color(0xFFEA580C),
                  size: 20,
                ),
              ),
              const SizedBox(width: 10),
              const Expanded(
                child: Text(
                  'Unfinished Previous Shift',
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF9A3412),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.only(left: 4),
            child: Text(
              '${shift.shiftName} (${_formatTime(shift.scheduledStartAt)} – ${_formatTime(shift.scheduledEndAt)}) has not been checked out.',
              style: const TextStyle(
                fontSize: 13,
                color: Color(0xFF7C2D12),
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          const SizedBox(height: 4),
          const Padding(
            padding: EdgeInsets.only(left: 4),
            child: Text(
              'Please complete check-out in Attendance History before checking in for a new shift.',
              style: TextStyle(
                fontSize: 12,
                color: Color(0xFF9A3412),
                height: 1.3,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
