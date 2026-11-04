import 'package:flutter/material.dart';
import '../../models/attendance/attendance_history_model.dart';
import '../../services/home/home_service.dart';
import '../../theme/app_colors.dart';

class AttendanceDetailView extends StatefulWidget {
  final int assignmentId;

  const AttendanceDetailView({super.key, required this.assignmentId});

  @override
  State<AttendanceDetailView> createState() => _AttendanceDetailViewState();
}

class _AttendanceDetailViewState extends State<AttendanceDetailView> {
  final HomeService _homeService = HomeService();
  bool _isLoading = true;
  String? _errorMessage;
  StaffAttendanceDetailModel? _detail;

  @override
  void initState() {
    super.initState();
    _loadDetail();
  }

  Future<void> _loadDetail() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final res = await _homeService.getStaffAttendanceDetail(
        widget.assignmentId,
      );
      if (mounted) {
        setState(() {
          _detail = res;
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

  String _formatDuration(int minutes) {
    if (minutes <= 0) return '0m';
    final h = minutes ~/ 60;
    final m = minutes % 60;
    if (h > 0) {
      return '${h}h ${m}m';
    }
    return '${m}m';
  }

  String _formatCurrency(double amount) {
    final formatted = amount
        .toStringAsFixed(0)
        .replaceAllMapped(
          RegExp(r'(\d{1,3})(?=(\d{3})+(?!\d))'),
          (Match m) => '${m[1]},',
        );
    return '$formatted đ';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF8FAFC),
      appBar: AppBar(
        title: const Text(
          'Attendance Details',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: AppColors.textPrimary,
          ),
        ),
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(
            Icons.arrow_back_ios_new_rounded,
            color: AppColors.textPrimary,
            size: 20,
          ),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: _buildContent(),
    );
  }

  Widget _buildContent() {
    if (_isLoading && _detail == null) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null && _detail == null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.error_outline_rounded,
                color: AppColors.error,
                size: 48,
              ),
              const SizedBox(height: 12),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary),
              ),
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _loadDetail,
                child: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    final detail = _detail;
    if (detail == null) {
      return const Center(child: Text('Shift attendance details not found.'));
    }

    return RefreshIndicator(
      onRefresh: _loadDetail,
      child: ListView(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        children: [
          // 1. Shift & Branch Header Card
          _buildHeroHeader(detail),
          const SizedBox(height: 14),

          // 2. Metrics 3-Col KPI Summary
          _buildKpiGrid(detail),
          const SizedBox(height: 14),

          // 3. Check-In & Check-Out Timeline Cards
          _buildAttendanceTimeline(detail),
          const SizedBox(height: 14),

          // 4. Timesheet Adjustment / Explanation Request (if any)
          if (detail.timesheetAdjustment != null) ...[
            _buildTimesheetAdjustmentCard(detail.timesheetAdjustment!),
            const SizedBox(height: 14),
          ],

          // 5. Penalties History (if any)
          if (detail.penalties.isNotEmpty) ...[
            _buildPenaltiesCard(detail),
            const SizedBox(height: 14),
          ],

          // 6. Associated Requests Card (if any)
          if (detail.lateArrivalRequest != null ||
              detail.earlyLeaveRequest != null ||
              detail.leaveRequest != null) ...[
            _buildRequestsCard(detail),
            const SizedBox(height: 14),
          ],
        ],
      ),
    );
  }

  Widget _buildHeroHeader(StaffAttendanceDetailModel detail) {
    Color badgeBg;
    Color badgeText;
    String badgeTitle;
    IconData badgeIcon;

    switch (detail.status.toUpperCase()) {
      case 'COMPLETED':
        badgeBg = const Color(0xFFDCFCE7);
        badgeText = const Color(0xFF15803D);
        badgeTitle = 'Completed';
        badgeIcon = Icons.check_circle_rounded;
        break;
      case 'IN_PROGRESS':
        badgeBg = const Color(0xFFFFEDD5);
        badgeText = const Color(0xFFC2410C);
        badgeTitle = 'In Progress';
        badgeIcon = Icons.timelapse_rounded;
        break;
      case 'INCOMPLETE':
        badgeBg = const Color(0xFFFEE2E2);
        badgeText = const Color(0xFFB91C1C);
        badgeTitle = 'Incomplete';
        badgeIcon = Icons.warning_amber_rounded;
        break;
      case 'ABSENT':
        badgeBg = const Color(0xFFF1F5F9);
        badgeText = const Color(0xFF64748B);
        badgeTitle = 'Absent';
        badgeIcon = Icons.cancel_outlined;
        break;
      case 'LEAVE':
      case 'ON_LEAVE':
        badgeBg = const Color(0xFFF3E8FF);
        badgeText = const Color(0xFF7E22CE);
        badgeTitle = 'On Leave';
        badgeIcon = Icons.event_busy_rounded;
        break;
      case 'LATE':
        badgeBg = const Color(0xFFFEF3C7);
        badgeText = const Color(0xFFB45309);
        badgeTitle = 'Late Check-In';
        badgeIcon = Icons.alarm_off_rounded;
        break;
      default:
        badgeBg = const Color(0xFFE0F2FE);
        badgeText = const Color(0xFF0369A1);
        badgeTitle = detail.status;
        badgeIcon = Icons.info_outline_rounded;
    }

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: AppColors.border),
        boxShadow: const [
          BoxShadow(
            color: Color(0x08000000),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      detail.shiftName,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      _formatDate(detail.scheduledStartAt),
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 5,
                ),
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
          const Divider(height: 24, color: AppColors.borderLight),
          Row(
            children: [
              const Icon(
                Icons.schedule_rounded,
                size: 16,
                color: AppColors.primary,
              ),
              const SizedBox(width: 8),
              Text(
                'Scheduled: ${_formatTime(detail.scheduledStartAt)} – ${_formatTime(detail.scheduledEndAt)}',
                style: const TextStyle(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w600,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(
                Icons.location_on_outlined,
                size: 16,
                color: AppColors.textMuted,
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      detail.branchName,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    if (detail.branchAddress != null &&
                        detail.branchAddress!.isNotEmpty) ...[
                      const SizedBox(height: 2),
                      Text(
                        detail.branchAddress!,
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.textMuted,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildKpiGrid(StaffAttendanceDetailModel detail) {
    return Row(
      children: [
        Expanded(
          child: _buildKpiCard(
            title: 'Work Hours',
            value: _formatDuration(detail.actualWorkingMinutes),
            icon: Icons.timer_outlined,
            iconColor: const Color(0xFF2563EB),
            bg: const Color(0xFFEFF6FF),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _buildKpiCard(
            title: 'Late Arrival',
            value: detail.totalLateMinutes > 0
                ? '${detail.totalLateMinutes}m'
                : '0m',
            icon: Icons.alarm_off_rounded,
            iconColor: detail.totalLateMinutes > 0
                ? const Color(0xFFD97706)
                : AppColors.textMuted,
            bg: detail.totalLateMinutes > 0
                ? const Color(0xFFFFFBEB)
                : Colors.white,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: _buildKpiCard(
            title: 'Early Leave',
            value: detail.totalEarlyLeaveMinutes > 0
                ? '${detail.totalEarlyLeaveMinutes}m'
                : '0m',
            icon: Icons.directions_run_rounded,
            iconColor: detail.totalEarlyLeaveMinutes > 0
                ? const Color(0xFFEA580C)
                : AppColors.textMuted,
            bg: detail.totalEarlyLeaveMinutes > 0
                ? const Color(0xFFFFF7ED)
                : Colors.white,
          ),
        ),
      ],
    );
  }

  Widget _buildKpiCard({
    required String title,
    required String value,
    required IconData icon,
    required Color iconColor,
    required Color bg,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: iconColor),
          const SizedBox(height: 8),
          Text(
            value,
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            title,
            style: const TextStyle(
              fontSize: 11,
              color: AppColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAttendanceTimeline(StaffAttendanceDetailModel detail) {
    final adj = detail.timesheetAdjustment;
    final isCheckInAdjusted =
        adj != null && adj.isApproved && adj.adjustCheckIn;
    final isCheckOutAdjusted =
        adj != null && adj.isApproved && adj.adjustCheckOut;

    return Column(
      children: [
        _buildEventCard(
          title: 'Check-In',
          time: detail.checkInTime,
          isCompleted: detail.checkInTime != null,
          icon: Icons.login_rounded,
          iconBg: const Color(0xFFEFF6FF),
          iconColor: const Color(0xFF2563EB),
          distanceMeters: detail.checkInDistanceMeters,
          maxDistanceMeters: detail.maxAttendanceDistanceMeters,
          accuracyMeters: detail.checkInAccuracyMeters,
          isFaceVerified: detail.checkInFaceVerified,
          faceConfidence: detail.checkInFaceConfidence,
          violationMinutes: detail.totalLateMinutes,
          violationType: 'Late Arrival',
          excusedMinutes: detail.excusedLateMinutes,
          isAdjusted: isCheckInAdjusted,
          originalTime: adj?.originalCheckInTime,
        ),
        const SizedBox(height: 12),
        _buildEventCard(
          title: 'Check-Out',
          time: detail.checkOutTime,
          isCompleted: detail.checkOutTime != null,
          icon: Icons.logout_rounded,
          iconBg: const Color(0xFFFFF7ED),
          iconColor: const Color(0xFFEA580C),
          distanceMeters: detail.checkOutDistanceMeters,
          maxDistanceMeters: detail.maxAttendanceDistanceMeters,
          accuracyMeters: detail.checkOutAccuracyMeters,
          isFaceVerified: detail.checkOutFaceVerified,
          faceConfidence: detail.checkOutFaceConfidence,
          violationMinutes: detail.totalEarlyLeaveMinutes,
          violationType: 'Early Leave',
          excusedMinutes: detail.excusedEarlyLeaveMinutes,
          isAdjusted: isCheckOutAdjusted,
          originalTime: adj?.originalCheckOutTime,
        ),
      ],
    );
  }

  Widget _buildTimesheetAdjustmentCard(StaffTimesheetAdjustmentModel adj) {
    Color badgeBg;
    Color badgeText;
    String badgeTitle;
    IconData badgeIcon;

    if (adj.isApproved) {
      badgeBg = const Color(0xFFDCFCE7);
      badgeText = const Color(0xFF15803D);
      badgeTitle = 'Approved';
      badgeIcon = Icons.check_circle_rounded;
    } else if (adj.isRejected) {
      badgeBg = const Color(0xFFFEE2E2);
      badgeText = const Color(0xFFB91C1C);
      badgeTitle = 'Rejected';
      badgeIcon = Icons.cancel_rounded;
    } else {
      badgeBg = const Color(0xFFFEF3C7);
      badgeText = const Color(0xFFB45309);
      badgeTitle = 'Pending Review';
      badgeIcon = Icons.hourglass_top_rounded;
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(
                Icons.published_with_changes_rounded,
                size: 18,
                color: AppColors.primary,
              ),
              const SizedBox(width: 8),
              const Text(
                'Timesheet Adjustment Request',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: badgeBg,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(badgeIcon, size: 12, color: badgeText),
                    const SizedBox(width: 4),
                    Text(
                      badgeTitle,
                      style: TextStyle(
                        fontSize: 11,
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

          // Reason Box
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(
                      Icons.chat_bubble_outline_rounded,
                      size: 13,
                      color: AppColors.textSecondary,
                    ),
                    SizedBox(width: 5),
                    Text(
                      'Staff Explanation / Reason:',
                      style: TextStyle(
                        fontSize: 11.5,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  adj.reason.isNotEmpty
                      ? '"${adj.reason}"'
                      : 'No detailed explanation provided',
                  style: const TextStyle(
                    fontSize: 13,
                    color: AppColors.textPrimary,
                    fontStyle: FontStyle.italic,
                  ),
                ),
              ],
            ),
          ),

          // Manager Note (if provided)
          if (adj.managerNote != null &&
              adj.managerNote!.trim().isNotEmpty) ...[
            const SizedBox(height: 8),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: adj.isApproved
                    ? const Color(0xFFF0FDF4)
                    : const Color(0xFFFEF2F2),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: adj.isApproved
                      ? const Color(0xFFBBF7D0)
                      : const Color(0xFFFECACA),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        Icons.admin_panel_settings_outlined,
                        size: 13,
                        color: adj.isApproved
                            ? const Color(0xFF15803D)
                            : const Color(0xFFB91C1C),
                      ),
                      const SizedBox(width: 5),
                      Text(
                        'Manager Feedback / Review Note:',
                        style: TextStyle(
                          fontSize: 11.5,
                          fontWeight: FontWeight.bold,
                          color: adj.isApproved
                              ? const Color(0xFF15803D)
                              : const Color(0xFFB91C1C),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '"${adj.managerNote!}"',
                    style: TextStyle(
                      fontSize: 12.5,
                      color: adj.isApproved
                          ? const Color(0xFF166534)
                          : const Color(0xFF991B1B),
                    ),
                  ),
                ],
              ),
            ),
          ],

          const SizedBox(height: 10),
          // Time adjustments comparison table
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: const Color(0xFFE2E8F0)),
            ),
            child: Column(
              children: [
                if (adj.adjustCheckIn)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 3),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.login_rounded,
                          size: 14,
                          color: Color(0xFF2563EB),
                        ),
                        const SizedBox(width: 6),
                        const Text(
                          'Check-In:',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const Spacer(),
                        Text(
                          _formatTime(adj.originalCheckInTime),
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textMuted,
                            decoration: TextDecoration.lineThrough,
                          ),
                        ),
                        const SizedBox(width: 6),
                        const Icon(
                          Icons.arrow_forward_rounded,
                          size: 12,
                          color: AppColors.textMuted,
                        ),
                        const SizedBox(width: 6),
                        Text(
                          _formatTime(adj.requestedCheckInTime),
                          style: const TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF15803D),
                          ),
                        ),
                      ],
                    ),
                  ),
                if (adj.adjustCheckIn && adj.adjustCheckOut)
                  const Divider(height: 12, color: Color(0xFFE2E8F0)),
                if (adj.adjustCheckOut)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 3),
                    child: Row(
                      children: [
                        const Icon(
                          Icons.logout_rounded,
                          size: 14,
                          color: Color(0xFFEA580C),
                        ),
                        const SizedBox(width: 6),
                        const Text(
                          'Check-Out:',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const Spacer(),
                        Text(
                          _formatTime(adj.originalCheckOutTime),
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.textMuted,
                            decoration: TextDecoration.lineThrough,
                          ),
                        ),
                        const SizedBox(width: 6),
                        const Icon(
                          Icons.arrow_forward_rounded,
                          size: 12,
                          color: AppColors.textMuted,
                        ),
                        const SizedBox(width: 6),
                        Text(
                          _formatTime(adj.requestedCheckOutTime),
                          style: const TextStyle(
                            fontSize: 12.5,
                            fontWeight: FontWeight.bold,
                            color: Color(0xFF15803D),
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),

          const SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Submitted: ${_formatDate(adj.createdAt)} ${_formatTime(adj.createdAt)}',
                style: const TextStyle(
                  fontSize: 11,
                  color: AppColors.textMuted,
                ),
              ),
              if (adj.reviewedAt != null)
                Text(
                  'Reviewed: ${_formatDate(adj.reviewedAt!)} ${_formatTime(adj.reviewedAt!)}',
                  style: const TextStyle(
                    fontSize: 11,
                    color: AppColors.textMuted,
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildEventCard({
    required String title,
    required DateTime? time,
    required bool isCompleted,
    required IconData icon,
    required Color iconBg,
    required Color iconColor,
    required double? distanceMeters,
    required int maxDistanceMeters,
    double? accuracyMeters,
    required bool? isFaceVerified,
    required double? faceConfidence,
    required int violationMinutes,
    required String violationType,
    required int excusedMinutes,
    bool isAdjusted = false,
    DateTime? originalTime,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  color: iconBg,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, color: iconColor, size: 20),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    Text(
                      isCompleted
                          ? 'Actual at ${_formatTime(time)}'
                          : 'Not Recorded',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: isCompleted
                            ? AppColors.textPrimary
                            : AppColors.textMuted,
                      ),
                    ),
                  ],
                ),
              ),
              if (isCompleted)
                const Icon(
                  Icons.check_circle_rounded,
                  color: Color(0xFF16A34A),
                  size: 20,
                )
              else
                const Icon(
                  Icons.radio_button_unchecked_rounded,
                  color: AppColors.border,
                  size: 20,
                ),
            ],
          ),
          if (isCompleted) ...[
            const Divider(height: 20, color: AppColors.borderLight),
            // GPS Location with Accuracy
            if (distanceMeters != null) ...[
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Padding(
                    padding: EdgeInsets.only(top: 2),
                    child: Icon(
                      Icons.my_location_rounded,
                      size: 14,
                      color: AppColors.textSecondary,
                    ),
                  ),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Text(
                              'Distance: ${distanceMeters.toStringAsFixed(1)}m (Max: ${maxDistanceMeters}m)',
                              style: const TextStyle(
                                fontSize: 12,
                                color: AppColors.textSecondary,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                            const Spacer(),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 6,
                                vertical: 2,
                              ),
                              decoration: BoxDecoration(
                                color: distanceMeters <= maxDistanceMeters
                                    ? const Color(0xFFDCFCE7)
                                    : const Color(0xFFFEE2E2),
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: Text(
                                distanceMeters <= maxDistanceMeters
                                    ? 'Valid'
                                    : 'Out of Range',
                                style: TextStyle(
                                  fontSize: 10,
                                  fontWeight: FontWeight.bold,
                                  color: distanceMeters <= maxDistanceMeters
                                      ? const Color(0xFF15803D)
                                      : const Color(0xFFB91C1C),
                                ),
                              ),
                            ),
                          ],
                        ),
                        if (accuracyMeters != null) ...[
                          const SizedBox(height: 2),
                          Text(
                            'GPS Accuracy: ±${accuracyMeters.toStringAsFixed(1)}m',
                            style: TextStyle(
                              fontSize: 11,
                              color: accuracyMeters <= 25
                                  ? const Color(0xFF15803D)
                                  : const Color(0xFFB45309),
                              fontWeight: FontWeight.w500,
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
            ],

            // Biometric Face Verification
            Row(
              children: [
                const Icon(
                  Icons.face_rounded,
                  size: 14,
                  color: AppColors.textSecondary,
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    isFaceVerified == true
                        ? 'Facial Recognition: Verified ${faceConfidence != null ? '(${(faceConfidence <= 1.0 ? faceConfidence * 100 : faceConfidence).toStringAsFixed(1)}%)' : ''}'
                        : 'Facial Recognition: Not Verified',
                    style: const TextStyle(
                      fontSize: 12,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ),
              ],
            ),

            // Adjusted Notice (if attendance time was adjusted by manager approval)
            if (isAdjusted && originalTime != null) ...[
              const SizedBox(height: 6),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: const Color(0xFFF0FDF4),
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(color: const Color(0xFFBBF7D0)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(
                      Icons.history_rounded,
                      size: 13,
                      color: Color(0xFF16A34A),
                    ),
                    const SizedBox(width: 5),
                    Text(
                      'Adjusted via approved request (Original: ${_formatTime(originalTime)})',
                      style: const TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF15803D),
                      ),
                    ),
                  ],
                ),
              ),
            ],

            // Violation Notice
            if (violationMinutes > 0) ...[
              const SizedBox(height: 6),
              Row(
                children: [
                  const Icon(
                    Icons.warning_amber_rounded,
                    size: 14,
                    color: Color(0xFFD97706),
                  ),
                  const SizedBox(width: 6),
                  Text(
                    '$violationType: ${violationMinutes}m',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFB45309),
                    ),
                  ),
                  if (excusedMinutes > 0) ...[
                    const SizedBox(width: 6),
                    Text(
                      '(Excused: ${excusedMinutes}m)',
                      style: const TextStyle(
                        fontSize: 11,
                        color: Color(0xFF15803D),
                      ),
                    ),
                  ],
                ],
              ),
            ],
          ],
        ],
      ),
    );
  }

  Widget _buildPenaltiesCard(StaffAttendanceDetailModel detail) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(
                Icons.gavel_rounded,
                size: 18,
                color: AppColors.textPrimary,
              ),
              const SizedBox(width: 8),
              const Text(
                'Violations & Penalties',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
              const Spacer(),
              if (detail.activePenalties.isNotEmpty)
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEE2E2),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    'Total: -${_formatCurrency(detail.totalActivePenaltyAmount)}',
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFB91C1C),
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 12),
          ...detail.penalties.map((p) {
            final isApplied = p.isApplied;
            return Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: isApplied
                    ? const Color(0xFFFEF2F2)
                    : const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: isApplied
                      ? const Color(0xFFFECACA)
                      : const Color(0xFFE2E8F0),
                ),
              ),
              child: Row(
                children: [
                  Icon(
                    isApplied
                        ? Icons.warning_rounded
                        : Icons.check_circle_outline_rounded,
                    size: 18,
                    color: isApplied
                        ? const Color(0xFFDC2626)
                        : AppColors.textMuted,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${p.violationTypeDisplay} (${p.violationMinutes}m)',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                            color: isApplied
                                ? const Color(0xFF991B1B)
                                : AppColors.textMuted,
                            decoration: isApplied
                                ? null
                                : TextDecoration.lineThrough,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          'Penalty Amount: -${p.formattedPenaltyAmount}',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                            color: isApplied
                                ? const Color(0xFFDC2626)
                                : AppColors.textMuted,
                            decoration: isApplied
                                ? null
                                : TextDecoration.lineThrough,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: isApplied
                          ? const Color(0xFFFEE2E2)
                          : const Color(0xFFE2E8F0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      isApplied ? 'Applied' : 'Excused',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: isApplied
                            ? const Color(0xFF991B1B)
                            : AppColors.textMuted,
                      ),
                    ),
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  Widget _buildRequestsCard(StaffAttendanceDetailModel detail) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.border),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Row(
            children: [
              Icon(
                Icons.assignment_outlined,
                size: 18,
                color: AppColors.textPrimary,
              ),
              SizedBox(width: 8),
              Text(
                'Associated Shift Requests',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.textPrimary,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          if (detail.lateArrivalRequest != null)
            _buildRequestItem(
              title: 'Late Arrival Request',
              status: detail.lateArrivalRequest!.status,
              reason: detail.lateArrivalRequest!.reason,
              time: detail.lateArrivalRequest!.requestedTime,
            ),
          if (detail.earlyLeaveRequest != null)
            _buildRequestItem(
              title: 'Early Leave Request',
              status: detail.earlyLeaveRequest!.status,
              reason: detail.earlyLeaveRequest!.reason,
              time: detail.earlyLeaveRequest!.requestedTime,
            ),
          if (detail.leaveRequest != null)
            _buildRequestItem(
              title: 'Leave Request',
              status: detail.leaveRequest!.status,
              reason: detail.leaveRequest!.reason,
            ),
        ],
      ),
    );
  }

  Widget _buildRequestItem({
    required String title,
    required String status,
    String? reason,
    DateTime? time,
  }) {
    final isApproved = status.toUpperCase() == 'APPROVED';

    return Container(
      margin: const EdgeInsets.only(top: 6),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AppColors.borderLight),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(
            isApproved
                ? Icons.check_circle_rounded
                : Icons.info_outline_rounded,
            size: 16,
            color: isApproved
                ? const Color(0xFF16A34A)
                : AppColors.textSecondary,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    Text(
                      status,
                      style: TextStyle(
                        fontSize: 10.5,
                        fontWeight: FontWeight.bold,
                        color: isApproved
                            ? const Color(0xFF15803D)
                            : AppColors.textSecondary,
                      ),
                    ),
                  ],
                ),
                if (reason != null && reason.isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(
                    'Reason: "$reason"',
                    style: const TextStyle(
                      fontSize: 11,
                      color: AppColors.textSecondary,
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
