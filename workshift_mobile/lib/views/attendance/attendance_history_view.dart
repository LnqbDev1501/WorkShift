import 'package:flutter/material.dart';
import '../../controllers/home_controller.dart';
import '../../models/attendance_history_model.dart';
import '../../models/attendance_shift_model.dart';
import '../../services/home_service.dart';
import '../../theme/app_colors.dart';
import '../home/handlers/attendance_flow_handler.dart';

class AttendanceHistoryView extends StatefulWidget {
  final HomeController homeController;

  const AttendanceHistoryView({
    super.key,
    required this.homeController,
  });

  @override
  State<AttendanceHistoryView> createState() => _AttendanceHistoryViewState();
}

class _AttendanceHistoryViewState extends State<AttendanceHistoryView> {
  final HomeService _homeService = HomeService();
  bool _isLoading = true;
  String? _errorMessage;
  List<StaffAttendanceHistoryModel> _history = [];

  @override
  void initState() {
    super.initState();
    _loadHistory();
  }

  Future<void> _loadHistory() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final results = await _homeService.getAttendanceHistory();
      if (mounted) {
        setState(() {
          _history = results;
          _isLoading = false;
        });
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

  String _formatTime(DateTime? dt) {
    if (dt == null) return '--:--';
    final local = dt.toLocal();
    final hour = local.hour.toString().padLeft(2, '0');
    final minute = local.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }

  String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    final day = local.day.toString().padLeft(2, '0');
    final month = local.month.toString().padLeft(2, '0');
    final year = local.year.toString();
    return '$day/$month/$year';
  }

  EligibleShiftModel _toEligibleShift(StaffAttendanceHistoryModel h) {
    return EligibleShiftModel(
      assignmentId: h.assignmentId,
      shiftId: h.shiftId,
      shiftName: h.shiftName,
      scheduledStartAt: h.scheduledStartAt,
      scheduledEndAt: h.scheduledEndAt,
      branchId: 0,
      branchName: h.branchName,
      branchAddress: h.branchAddress,
      branchLatitude: h.branchLatitude,
      branchLongitude: h.branchLongitude,
      maxAttendanceDistanceMeters: h.maxAttendanceDistanceMeters,
      canCheckInNow: h.canCheckInNow,
      canNotCheckInReason: h.cannotCheckInReason,
      checkOutCutoffAt: h.checkOutCutoffAt,
      attendanceStatus: h.status,
      hasApprovedLeave: false,
      hasCheckedIn: h.checkInTime != null,
      hasCheckedOut: h.checkOutTime != null,
      checkInTime: h.checkInTime,
      checkOutTime: h.checkOutTime,
      canCheckOutNow: h.canCheckOutNow,
      canNotCheckOutReason: h.cannotCheckOutReason,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Attendance History',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: AppColors.textPrimary, size: 20),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: RefreshIndicator(
        onRefresh: _loadHistory,
        child: _buildBody(),
      ),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline_rounded, color: AppColors.error, size: 48),
              const SizedBox(height: 12),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _loadHistory,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_history.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.history_rounded, size: 56, color: AppColors.textMuted),
              SizedBox(height: 14),
              Text(
                'No Attendance Records',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textSecondary,
                ),
              ),
              SizedBox(height: 6),
              Text(
                'Your chronological attendance history will appear here.',
                textAlign: TextAlign.center,
                style: TextStyle(color: AppColors.textMuted, fontSize: 13),
              ),
            ],
          ),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: _history.length,
      separatorBuilder: (_, index) => const SizedBox(height: 12),
      itemBuilder: (context, index) {
        final item = _history[index];
        return _buildHistoryCard(item);
      },
    );
  }

  Widget _buildHistoryCard(StaffAttendanceHistoryModel item) {
    Color badgeBg;
    Color badgeText;
    String badgeTitle;
    IconData badgeIcon;

    switch (item.status.toUpperCase()) {
      case 'READY_TO_CHECK_IN':
        badgeBg = const Color(0xFFE8F5E9);
        badgeText = const Color(0xFF2E7D32);
        badgeTitle = 'Ready to Check-In';
        badgeIcon = Icons.access_time_rounded;
        break;
      case 'UPCOMING':
        badgeBg = Colors.blue.shade50;
        badgeText = Colors.blue.shade700;
        badgeTitle = 'Upcoming';
        badgeIcon = Icons.schedule_rounded;
        break;
      case 'CHECKIN_EXPIRED':
        badgeBg = Colors.amber.shade50;
        badgeText = Colors.amber.shade800;
        badgeTitle = 'Check-in Expired';
        badgeIcon = Icons.alarm_off_rounded;
        break;
      case 'COMPLETED':
        badgeBg = Colors.green.shade50;
        badgeText = Colors.green.shade700;
        badgeTitle = 'Completed';
        badgeIcon = Icons.check_circle_rounded;
        break;
      case 'IN_PROGRESS':
        badgeBg = Colors.orange.shade50;
        badgeText = Colors.orange.shade800;
        badgeTitle = 'In Progress';
        badgeIcon = Icons.timelapse_rounded;
        break;
      case 'INCOMPLETE':
        badgeBg = Colors.red.shade50;
        badgeText = Colors.red.shade700;
        badgeTitle = 'Incomplete';
        badgeIcon = Icons.warning_amber_rounded;
        break;
      case 'ABSENT':
        badgeBg = Colors.grey.shade100;
        badgeText = Colors.grey.shade600;
        badgeTitle = 'Absent';
        badgeIcon = Icons.cancel_outlined;
        break;
      case 'ON_LEAVE':
        badgeBg = Colors.purple.shade50;
        badgeText = Colors.purple.shade700;
        badgeTitle = 'On Leave';
        badgeIcon = Icons.event_busy_rounded;
        break;
      default:
        badgeBg = Colors.blue.shade50;
        badgeText = Colors.blue.shade700;
        badgeTitle = item.status;
        badgeIcon = Icons.info_outline_rounded;
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: item.status == 'IN_PROGRESS' 
              ? Colors.orange.shade300 
              : item.canCheckInNow 
                  ? Colors.blue.shade300 
                  : AppColors.border,
          width: (item.status == 'IN_PROGRESS' || item.canCheckInNow) ? 1.5 : 1,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header: Shift Name & Status
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.shiftName,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 3),
                    Text(
                      _formatDate(item.scheduledStartAt),
                      style: const TextStyle(fontSize: 12.5, color: AppColors.textMuted),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
                decoration: BoxDecoration(
                  color: badgeBg,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(badgeIcon, size: 14, color: badgeText),
                    const SizedBox(width: 4),
                    Text(
                      badgeTitle,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: badgeText,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          const SizedBox(height: 12),

          // Shift Scheduled Time & Branch
          Row(
            children: [
              const Icon(Icons.schedule_rounded, size: 15, color: AppColors.textMuted),
              const SizedBox(width: 6),
              Text(
                '${_formatTime(item.scheduledStartAt)} – ${_formatTime(item.scheduledEndAt)}',
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(width: 14),
              const Icon(Icons.location_on_outlined, size: 15, color: AppColors.textMuted),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  item.branchName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 13, color: AppColors.textMuted),
                ),
              ),
            ],
          ),

          const SizedBox(height: 10),

          // Attendance Details Box
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: AppColors.background,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    const Text('In: ', style: TextStyle(fontSize: 12.5, color: AppColors.textMuted)),
                    Text(
                      _formatTime(item.checkInTime),
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.bold,
                        color: item.checkInTime != null ? AppColors.textPrimary : AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
                Row(
                  children: [
                    const Text('Out: ', style: TextStyle(fontSize: 12.5, color: AppColors.textMuted)),
                    Text(
                      _formatTime(item.checkOutTime),
                      style: TextStyle(
                        fontSize: 12.5,
                        fontWeight: FontWeight.bold,
                        color: item.checkOutTime != null ? AppColors.textPrimary : AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
                if (item.actualWorkingMinutes > 0) ...[
                  Text(
                    '${item.actualWorkingMinutes ~/ 60}h ${item.actualWorkingMinutes % 60}m',
                    style: const TextStyle(
                      fontSize: 12.5,
                      fontWeight: FontWeight.w600,
                      color: AppColors.primary,
                    ),
                  ),
                ],
              ],
            ),
          ),

          // Warning / Notice for INCOMPLETE
          if (item.status.toUpperCase() == 'INCOMPLETE') ...[
            const SizedBox(height: 8),
            const Row(
              children: [
                Icon(Icons.info_outline_rounded, size: 14, color: AppColors.error),
                SizedBox(width: 6),
                Expanded(
                  child: Text(
                    'Check-out cutoff has passed. Shift status recorded as Incomplete.',
                    style: TextStyle(fontSize: 11.5, color: AppColors.error),
                  ),
                ),
              ],
            ),
          ],

          // Action: Check-In if ready
          if (item.canCheckInNow) ...[
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 10),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  elevation: 0,
                ),
                icon: const Icon(Icons.login_rounded, size: 16),
                label: const Text(
                  'Check In',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                ),
                onPressed: () async {
                  await AttendanceFlowHandler.handleCheckIn(
                    context: context,
                    homeController: widget.homeController,
                    targetShift: _toEligibleShift(item),
                  );
                  _loadHistory();
                },
              ),
            ),
          ],

          // Action: Check-Out if still within window
          if (item.canCheckOutNow) ...[
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.deepOrange,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 10),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                  elevation: 0,
                ),
                icon: const Icon(Icons.logout_rounded, size: 16),
                label: const Text(
                  'Check Out Now',
                  style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                ),
                onPressed: () async {
                  await AttendanceFlowHandler.handleCheckOut(
                    context: context,
                    homeController: widget.homeController,
                    targetShift: _toEligibleShift(item),
                  );
                  _loadHistory();
                },
              ),
            ),
          ],
        ],
      ),
    );
  }
}
