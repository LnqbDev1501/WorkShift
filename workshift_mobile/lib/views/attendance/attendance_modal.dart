import 'package:camera/camera.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import '../../controllers/attendance/attendance_controller.dart';
import '../../models/attendance/attendance_shift_model.dart';
import '../../theme/app_colors.dart';
import '../face_registration/face_registration_modal.dart';

/// Cờ kiểm tra xem BottomSheet chấm công có đang hiển thị không (tránh mở nhiều cái đè nhau)
bool _isAttendanceModalOpen = false;

/// Function to show Attendance Popup directly from Homepage
Future<bool?> showAttendanceModal({
  required BuildContext context,
  required EligibleShiftModel shift,
  required bool isCheckIn,
}) async {
  if (_isAttendanceModalOpen) return null;
  _isAttendanceModalOpen = true;

  try {
    return await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      isDismissible: false,
      enableDrag: false,
      backgroundColor: Colors.transparent,
      builder: (ctx) =>
          AttendanceModalPopup(shift: shift, isCheckIn: isCheckIn),
    );
  } finally {
    _isAttendanceModalOpen = false;
  }
}

class AttendanceModalPopup extends StatefulWidget {
  final EligibleShiftModel shift;
  final bool isCheckIn;

  const AttendanceModalPopup({
    super.key,
    required this.shift,
    this.isCheckIn = true,
  });

  @override
  State<AttendanceModalPopup> createState() => _AttendanceModalPopupState();
}

class _AttendanceModalPopupState extends State<AttendanceModalPopup>
    with SingleTickerProviderStateMixin {
  late final AttendanceController _controller;
  late final AnimationController _scanAnimController;
  late final Animation<double> _scanAnimation;

  @override
  void initState() {
    super.initState();
    _controller = AttendanceController(
      shift: widget.shift,
      isCheckIn: widget.isCheckIn,
    );
    _controller.initialize();

    // Laser scanning beam animation
    _scanAnimController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1800),
    )..repeat(reverse: true);

    _scanAnimation = Tween<double>(begin: 0.12, end: 0.88).animate(
      CurvedAnimation(parent: _scanAnimController, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _scanAnimController.dispose();
    _controller.dispose();
    super.dispose();
  }

  IconData _getChallengeIcon(String action) {
    switch (action.toUpperCase()) {
      case 'BLINK':
        return Icons.visibility_rounded;
      case 'SMILE':
        return Icons.sentiment_very_satisfied_rounded;
      case 'TURN_LEFT':
        return Icons.arrow_back_rounded;
      case 'TURN_RIGHT':
        return Icons.arrow_forward_rounded;
      case 'NOD':
        return Icons.swap_vert_rounded;
      case 'LOOK_STRAIGHT':
      default:
        return Icons.center_focus_strong_rounded;
    }
  }

  @override
  Widget build(BuildContext context) {
    final mediaQuery = MediaQuery.of(context);
    final maxHeight = mediaQuery.size.height * 0.92;

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 500),
        child: Container(
          height: maxHeight,
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.vertical(top: Radius.circular(32)),
            boxShadow: [
              BoxShadow(
                color: Colors.black26,
                blurRadius: 24,
                offset: Offset(0, -6),
              ),
            ],
          ),
          child: ListenableBuilder(
            listenable: _controller,
            builder: (context, _) {
              if (_controller.isSuccess) {
                return _buildSuccessContent();
              }

              // Guard 3: Fail-Fast during initialization
              if (_controller.isInitializing) {
                return _buildInitializingContent();
              }

              // Guard 3: Fail-Fast if session failed to initialize or shift expired/closed
              if (!_controller.hasSession && _controller.errorMessage != null) {
                return _buildInitializationErrorContent();
              }

              return Column(
                children: [
                  // Top Drag Handle & Header
                  _buildHeader(),

                  // Scrollable Body
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.symmetric(horizontal: 20),
                      child: Column(
                        children: [
                          const SizedBox(height: 12),

                          // Error Banner
                          if (_controller.errorMessage != null) ...[
                            _buildErrorBanner(_controller.errorMessage!),
                            const SizedBox(height: 12),
                          ],

                          // Geofence & Shift Card
                          _buildShiftPill(),
                          const SizedBox(height: 14),

                          // Live Camera Preview Box with Reticles & Laser
                          _buildLiveCameraView(),
                          const SizedBox(height: 14),

                          // Liveness Challenge Step Card
                          _buildChallengeCard(),
                          const SizedBox(height: 16),
                        ],
                      ),
                    ),
                  ),

                  // Bottom Auto-Detection Status Bar
                  _buildBottomBar(),
                ],
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    final isCheckIn = widget.isCheckIn;
    final title = isCheckIn ? 'Attendance Check-In' : 'Attendance Check-Out';
    final themeColor = isCheckIn
        ? const Color(0xFF059669)
        : const Color(0xFFEA580C);
    final iconColor = isCheckIn
        ? const Color(0xFF047857)
        : const Color(0xFFC2410C);
    final bgLight = isCheckIn
        ? const Color(0xFFECFDF5)
        : const Color(0xFFFFF7ED);

    return Container(
      padding: const EdgeInsets.fromLTRB(20, 10, 12, 14),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: AppColors.borderLight)),
      ),
      child: Column(
        children: [
          // Drag handle pill
          Container(
            width: 44,
            height: 4.5,
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: const Color(0xFFCBD5E1),
              borderRadius: BorderRadius.circular(4),
            ),
          ),
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(9),
                decoration: BoxDecoration(
                  color: bgLight,
                  shape: BoxShape.circle,
                  border: Border.all(color: themeColor.withValues(alpha: 0.2)),
                ),
                child: Icon(
                  isCheckIn ? Icons.login_rounded : Icons.logout_rounded,
                  color: iconColor,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                        letterSpacing: -0.3,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${widget.shift.shiftName} • ${widget.shift.formattedTimeRange}',
                      style: const TextStyle(
                        fontSize: 12.5,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                style: IconButton.styleFrom(
                  backgroundColor: const Color(0xFFF1F5F9),
                  padding: const EdgeInsets.all(8),
                ),
                icon: const Icon(
                  Icons.close_rounded,
                  color: AppColors.textSecondary,
                  size: 20,
                ),
                onPressed: () =>
                    Navigator.of(context).pop(_controller.isSuccess),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildShiftPill() {
    final isWithin = _controller.isWithinGeofence;
    final maxDist = widget.shift.maxAttendanceDistanceMeters;
    final distanceText = '${_controller.distanceMeters.toStringAsFixed(1)}m';

    final accuracyText = _controller.currentAccuracy != null
        ? '±${_controller.currentAccuracy!.toStringAsFixed(1)}m'
        : 'N/A';

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderLight),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.02),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Row 1: Branch Name & Mock GPS / Real GPS Toggle Button
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(
                  Icons.storefront_rounded,
                  size: 16,
                  color: AppColors.primary,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  widget.shift.branchName,
                  style: const TextStyle(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              const SizedBox(width: 8),
              GestureDetector(
                onTap: () => _controller.toggleTestGps(),
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 10,
                    vertical: 6,
                  ),
                  decoration: BoxDecoration(
                    color: _controller.isTestGpsMode
                        ? const Color(0xFFEFF6FF)
                        : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                      color: _controller.isTestGpsMode
                          ? const Color(0xFF3B82F6)
                          : const Color(0xFFCBD5E1),
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      if (_controller.isLocating) ...[
                        const SizedBox(
                          width: 12,
                          height: 12,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Color(0xFF475569),
                          ),
                        ),
                        const SizedBox(width: 5),
                        const Text(
                          'Locating...',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF475569),
                          ),
                        ),
                      ] else ...[
                        Icon(
                          _controller.isTestGpsMode
                              ? Icons.bug_report_rounded
                              : Icons.my_location_rounded,
                          size: 13,
                          color: _controller.isTestGpsMode
                              ? const Color(0xFF2563EB)
                              : const Color(0xFF475569),
                        ),
                        const SizedBox(width: 4),
                        Text(
                          _controller.isTestGpsMode
                              ? 'Mock GPS (0m)'
                              : 'Real GPS',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: _controller.isTestGpsMode
                                ? const Color(0xFF2563EB)
                                : const Color(0xFF475569),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          // Row 2: Distance, Accuracy & Geofence Badge
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
            decoration: BoxDecoration(
              color: isWithin
                  ? const Color(0xFFECFDF5)
                  : const Color(0xFFFEF2F2),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: isWithin
                    ? const Color(0xFFA7F3D0)
                    : const Color(0xFFFECACA),
              ),
            ),
            child: Row(
              children: [
                Icon(
                  Icons.location_on_rounded,
                  size: 15,
                  color: isWithin
                      ? const Color(0xFF059669)
                      : const Color(0xFFDC2626),
                ),
                const SizedBox(width: 6),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Row(
                        children: [
                          Flexible(
                            child: Text(
                              'Distance: $distanceText',
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                                color: isWithin
                                    ? const Color(0xFF047857)
                                    : const Color(0xFFB91C1C),
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                          const SizedBox(width: 4),
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 4,
                              vertical: 1,
                            ),
                            decoration: BoxDecoration(
                              color: isWithin
                                  ? const Color(0xFFD1FAE5)
                                  : const Color(0xFFFEE2E2),
                              borderRadius: BorderRadius.circular(4),
                            ),
                            child: Text(
                              accuracyText,
                              style: TextStyle(
                                fontSize: 10,
                                fontWeight: FontWeight.w700,
                                color: isWithin
                                    ? const Color(0xFF065F46)
                                    : const Color(0xFF991B1B),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Effective: ${_controller.effectiveDistanceMeters.toStringAsFixed(1)}m • Max: ${maxDist}m',
                        style: TextStyle(
                          fontSize: 10.5,
                          fontWeight: FontWeight.w500,
                          color: isWithin
                              ? const Color(0xFF059669)
                              : const Color(0xFFDC2626),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 6),
                Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 7,
                    vertical: 3,
                  ),
                  decoration: BoxDecoration(
                    color: isWithin
                        ? const Color(0xFF059669)
                        : const Color(0xFFDC2626),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(
                    isWithin ? 'Within Geofence ✓' : 'Out of Range',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLiveCameraView() {
    final remaining = _controller.remainingSeconds;
    final minutes = (remaining ~/ 60).toString().padLeft(2, '0');
    final seconds = (remaining % 60).toString().padLeft(2, '0');
    final isFramed = _controller.isFaceFramedProperly;
    final frameColor = isFramed
        ? const Color(0xFF10B981)
        : const Color(0xFFEF4444);

    return Container(
      height: 315,
      width: double.infinity,
      decoration: BoxDecoration(
        color: const Color(0xFF0B132B),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.18),
            blurRadius: 18,
            offset: const Offset(0, 6),
          ),
        ],
      ),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        fit: StackFit.expand,
        children: [
          // 1. Live Camera or Simulated AI Face Mesh
          if (kIsWeb)
            Image.asset('assets/sample_face.jpg', fit: BoxFit.cover)
          else if (_controller.isCameraInitialized &&
              _controller.cameraController != null)
            CameraPreview(_controller.cameraController!)
          else
            _buildCameraLoadingPlaceholder(),

          // 2. Animated Laser Scanning Beam
          AnimatedBuilder(
            animation: _scanAnimation,
            builder: (context, _) {
              return Positioned(
                top: 315 * _scanAnimation.value,
                left: 36,
                right: 36,
                child: Container(
                  height: 3,
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [
                        Colors.transparent,
                        Color(0xFF10B981),
                        Color(0xFF34D399),
                        Colors.transparent,
                      ],
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFF10B981).withValues(alpha: 0.8),
                        blurRadius: 8,
                        spreadRadius: 2,
                      ),
                    ],
                  ),
                ),
              );
            },
          ),

          // 3. Modern Corner Target Reticles around Face Center
          Center(
            child: SizedBox(
              width: 236,
              height: 286,
              child: Stack(
                children: [
                  _buildReticleCorner(
                    isTop: true,
                    isLeft: true,
                    color: frameColor,
                  ),
                  _buildReticleCorner(
                    isTop: true,
                    isLeft: false,
                    color: frameColor,
                  ),
                  _buildReticleCorner(
                    isTop: false,
                    isLeft: true,
                    color: frameColor,
                  ),
                  _buildReticleCorner(
                    isTop: false,
                    isLeft: false,
                    color: frameColor,
                  ),
                ],
              ),
            ),
          ),

          // 4. Face Scanner Oval Overlay Guide with Dynamic Color
          Center(
            child: Container(
              width: 210,
              height: 260,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(105),
                border: Border.all(color: frameColor, width: 2.5),
                boxShadow: [
                  BoxShadow(
                    color: frameColor.withValues(alpha: 0.3),
                    blurRadius: 14,
                    spreadRadius: 2,
                  ),
                ],
              ),
            ),
          ),

          // 5. Top Centering Badge
          Positioned(
            top: 48,
            left: 20,
            right: 20,
            child: Center(
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 250),
                padding: const EdgeInsets.symmetric(
                  horizontal: 14,
                  vertical: 5.5,
                ),
                decoration: BoxDecoration(
                  color: isFramed
                      ? const Color(0xFF065F46).withValues(alpha: 0.9)
                      : const Color(0xFF991B1B).withValues(alpha: 0.9),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: isFramed
                        ? const Color(0xFF34D399)
                        : const Color(0xFFF87171),
                    width: 1,
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      isFramed
                          ? Icons.check_circle_rounded
                          : Icons.warning_amber_rounded,
                      size: 14,
                      color: Colors.white,
                    ),
                    const SizedBox(width: 6),
                    Text(
                      isFramed ? 'Face Centered ✓' : 'Align face inside oval',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 11.5,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // 6. Top Right Session Countdown Badge
          Positioned(
            top: 12,
            right: 12,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.65),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color:
                      (remaining < 20 ||
                          _controller.isSessionExpired ||
                          _controller.isShiftWindowClosed)
                      ? const Color(0xFFEF4444)
                      : Colors.white30,
                ),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    (_controller.isSessionExpired ||
                            _controller.isShiftWindowClosed)
                        ? Icons.timer_off_rounded
                        : Icons.timer_outlined,
                    size: 14,
                    color:
                        (remaining < 20 ||
                            _controller.isSessionExpired ||
                            _controller.isShiftWindowClosed)
                        ? const Color(0xFFEF4444)
                        : Colors.white,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    (_controller.isSessionExpired ||
                            _controller.isShiftWindowClosed)
                        ? '00:00'
                        : '$minutes:$seconds',
                    style: TextStyle(
                      color:
                          (remaining < 20 ||
                              _controller.isSessionExpired ||
                              _controller.isShiftWindowClosed)
                          ? const Color(0xFFEF4444)
                          : Colors.white,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),

          // 8. Bottom Live AI Status inside camera with Hold Progress Bar
          Positioned(
            bottom: 12,
            left: 16,
            right: 16,
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 14,
                vertical: 8.5,
              ),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.85),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: _controller.challengeHoldProgress > 0
                      ? const Color(0xFF10B981)
                      : Colors.white24,
                ),
              ),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      SizedBox(
                        width: 12,
                        height: 12,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: _controller.challengeHoldProgress > 0
                              ? const Color(0xFF10B981)
                              : Colors.white70,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Flexible(
                        child: Text(
                          _controller.detectionStatus,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 12,
                            fontWeight: FontWeight.w600,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                  if (_controller.challengeHoldProgress > 0) ...[
                    const SizedBox(height: 6),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: _controller.challengeHoldProgress,
                        minHeight: 4,
                        backgroundColor: Colors.white24,
                        valueColor: const AlwaysStoppedAnimation<Color>(
                          Color(0xFF10B981),
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),

          // 9. Guard 1 & 2: Session Expired / Cutoff Lock Overlay
          if (_controller.isSessionExpired || _controller.isShiftWindowClosed)
            _buildCameraExpiredOverlay(),
        ],
      ),
    );
  }

  Widget _buildReticleCorner({
    required bool isTop,
    required bool isLeft,
    required Color color,
  }) {
    return Positioned(
      top: isTop ? 0 : null,
      bottom: !isTop ? 0 : null,
      left: isLeft ? 0 : null,
      right: !isLeft ? 0 : null,
      child: Container(
        width: 18,
        height: 18,
        decoration: BoxDecoration(
          border: Border(
            top: isTop ? BorderSide(color: color, width: 3) : BorderSide.none,
            bottom: !isTop
                ? BorderSide(color: color, width: 3)
                : BorderSide.none,
            left: isLeft ? BorderSide(color: color, width: 3) : BorderSide.none,
            right: !isLeft
                ? BorderSide(color: color, width: 3)
                : BorderSide.none,
          ),
        ),
      ),
    );
  }

  Widget _buildCameraLoadingPlaceholder() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(18),
            decoration: BoxDecoration(
              color: const Color(0xFF10B981).withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.face_retouching_natural_rounded,
              color: Color(0xFF34D399),
              size: 52,
            ),
          ),
          const SizedBox(height: 14),
          const Text(
            'Initializing Biometric Camera...',
            style: TextStyle(
              color: Colors.white,
              fontSize: 14,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.3,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'Keep your face in front of the lens',
            style: TextStyle(color: Colors.white70, fontSize: 12),
          ),
        ],
      ),
    );
  }

  Widget _buildChallengeCard() {
    final isLocked =
        _controller.isSessionExpired || _controller.isShiftWindowClosed;
    final current = _controller.currentChallenge;
    final challenges = _controller.challenges;
    final currentIndex = _controller.currentChallengeIndex;
    final total = challenges.length;

    if (current == null) {
      return const SizedBox.shrink();
    }

    return Opacity(
      opacity: isLocked ? 0.55 : 1.0,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: const Color(0xFFF8FAFC),
          borderRadius: BorderRadius.circular(18),
          border: Border.all(color: AppColors.borderLight),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.02),
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
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: isLocked
                        ? const Color(0xFF94A3B8)
                        : AppColors.primary,
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    isLocked
                        ? Icons.lock_rounded
                        : _getChallengeIcon(current.action),
                    color: Colors.white,
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
                          Text(
                            'Step ${currentIndex + 1} of $total: ',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: isLocked
                                  ? const Color(0xFF64748B)
                                  : AppColors.primary,
                            ),
                          ),
                          Text(
                            current.action,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w800,
                              color: isLocked
                                  ? const Color(0xFF64748B)
                                  : AppColors.primary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 3),
                      Text(
                        current.instruction,
                        style: const TextStyle(
                          fontSize: 14.5,
                          fontWeight: FontWeight.w700,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),

            // Real-time Steps Progress Dots
            Row(
              children: List.generate(total, (i) {
                final isDone = i < currentIndex;
                final isCurrent = i == currentIndex;

                return Expanded(
                  child: Container(
                    height: 4,
                    margin: EdgeInsets.only(right: i == total - 1 ? 0 : 6),
                    decoration: BoxDecoration(
                      color: isDone
                          ? const Color(0xFF059669)
                          : (isCurrent
                                ? (isLocked
                                      ? const Color(0xFF94A3B8)
                                      : AppColors.primary)
                                : const Color(0xFFCBD5E1)),
                      borderRadius: BorderRadius.circular(2),
                    ),
                  ),
                );
              }),
            ),

            if (isLocked) ...[
              const SizedBox(height: 10),
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 10,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEF2F2),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFFFECACA)),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.lock_clock_rounded,
                      size: 14,
                      color: Color(0xFFDC2626),
                    ),
                    SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        'Challenge detection paused due to expired session.',
                        style: TextStyle(
                          color: Color(0xFFB91C1C),
                          fontSize: 11.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildBottomBar() {
    final isSubmitting = _controller.isSubmitting;
    final isExpired =
        _controller.isSessionExpired || _controller.isShiftWindowClosed;

    if (isExpired) {
      final isShiftClosed = _controller.isShiftWindowClosed;
      return Container(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
        decoration: const BoxDecoration(
          color: Colors.white,
          border: Border(top: BorderSide(color: AppColors.borderLight)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (!isShiftClosed) ...[
              SizedBox(
                width: double.infinity,
                child: ElevatedButton.icon(
                  onPressed: () => _controller.retrySession(),
                  icon: const Icon(Icons.refresh_rounded, size: 18),
                  label: const Text(
                    'Restart Verification Session',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 14.5,
                    ),
                  ),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                    elevation: 2,
                  ),
                ),
              ),
              const SizedBox(height: 6),
              SizedBox(
                width: double.infinity,
                child: TextButton(
                  onPressed: () => Navigator.of(context).pop(false),
                  child: const Text(
                    'Close',
                    style: TextStyle(
                      color: AppColors.textSecondary,
                      fontWeight: FontWeight.w600,
                      fontSize: 14,
                    ),
                  ),
                ),
              ),
            ] else ...[
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () => Navigator.of(context).pop(false),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFFDC2626),
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 14),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                  child: const Text(
                    'Shift Closed - Dismiss',
                    style: TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                  ),
                ),
              ),
            ],
          ],
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 20),
      decoration: const BoxDecoration(
        color: Colors.white,
        border: Border(top: BorderSide(color: AppColors.borderLight)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
            decoration: BoxDecoration(
              color: isSubmitting ? AppColors.primary : const Color(0xFFECFDF5),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: isSubmitting
                    ? AppColors.primary
                    : const Color(0xFFA7F3D0),
              ),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (isSubmitting) ...[
                  const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: Colors.white,
                    ),
                  ),
                  const SizedBox(width: 10),
                  const Text(
                    'Verifying Biometrics & Submitting...',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13.5,
                      color: Colors.white,
                    ),
                  ),
                ] else ...[
                  const Icon(
                    Icons.verified_user_rounded,
                    size: 18,
                    color: Color(0xFF059669),
                  ),
                  const SizedBox(width: 8),
                  const Text(
                    'Google ML Kit Face Biometrics Active',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13.5,
                      color: Color(0xFF065F46),
                    ),
                  ),
                ],
              ],
            ),
          ),
          if (kIsWeb || !_controller.isCameraInitialized) ...[
            const SizedBox(height: 10),
            ElevatedButton.icon(
              onPressed: isSubmitting
                  ? null
                  : () => _controller.testSubmitWithSampleFace(),
              icon: const Icon(
                Icons.flash_on_rounded,
                color: Colors.white,
                size: 20,
              ),
              label: Text(
                widget.isCheckIn
                    ? '1-Tap Test: Instant Check-In (Web)'
                    : '1-Tap Test: Instant Check-Out (Web)',
                style: const TextStyle(
                  fontWeight: FontWeight.w700,
                  fontSize: 14,
                  color: Colors.white,
                ),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: widget.isCheckIn
                    ? const Color(0xFF059669)
                    : const Color(0xFFEA580C),
                minimumSize: const Size(double.infinity, 48),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
                elevation: 2,
              ),
            ),
          ],
        ],
      ),
    );
  }

  /// Overlay displayed directly on top of camera preview when timeout or cut-off is hit
  Widget _buildCameraExpiredOverlay() {
    final isShiftClosed = _controller.isShiftWindowClosed;
    final title = isShiftClosed ? 'SHIFT WINDOW CLOSED' : 'SESSION EXPIRED';
    final desc = isShiftClosed
        ? 'Cut-off deadline reached. Attendance verification is locked.'
        : 'The verification window timed out. Live camera & challenges are locked.';
    final icon = isShiftClosed
        ? Icons.event_busy_rounded
        : Icons.timer_off_rounded;
    final iconColor = isShiftClosed
        ? const Color(0xFFEF4444)
        : const Color(0xFFF59E0B);
    final bgIcon = isShiftClosed
        ? const Color(0xFF7F1D1D)
        : const Color(0xFF78350F);

    return Container(
      decoration: BoxDecoration(
        color: Colors.black.withValues(alpha: 0.90),
        borderRadius: BorderRadius.circular(24),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
      child: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: bgIcon.withValues(alpha: 0.7),
                shape: BoxShape.circle,
                border: Border.all(
                  color: iconColor.withValues(alpha: 0.5),
                  width: 2,
                ),
                boxShadow: [
                  BoxShadow(
                    color: iconColor.withValues(alpha: 0.35),
                    blurRadius: 18,
                  ),
                ],
              ),
              child: Icon(icon, color: iconColor, size: 36),
            ),
            const SizedBox(height: 14),
            Text(
              title,
              style: const TextStyle(
                color: Colors.white,
                fontSize: 16,
                fontWeight: FontWeight.w800,
                letterSpacing: 0.8,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              desc,
              textAlign: TextAlign.center,
              style: const TextStyle(
                color: Color(0xFFCBD5E1),
                fontSize: 12.5,
                height: 1.35,
              ),
            ),
            if (!isShiftClosed) ...[
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: () => _controller.retrySession(),
                icon: const Icon(Icons.refresh_rounded, size: 16),
                label: const Text(
                  'Restart Verification',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 13),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 20,
                    vertical: 10,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  /// Guard 3: Sleek loading state while generating dynamic challenges & session
  Widget _buildInitializingContent() {
    return Column(
      children: [
        _buildHeader(),
        Expanded(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    width: 90,
                    height: 90,
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.1),
                      shape: BoxShape.circle,
                    ),
                    child: const Center(
                      child: SizedBox(
                        width: 48,
                        height: 48,
                        child: CircularProgressIndicator(
                          strokeWidth: 3.5,
                          color: AppColors.primary,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  const Text(
                    'Preparing Verification Session',
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Validating shift attendance window and generating randomized security challenges...',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSecondary,
                      height: 1.4,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  /// Guard 3: Fail-Fast Error State when session fails, face registration required, or shift closed
  Widget _buildInitializationErrorContent() {
    final isFaceReq = _controller.isFaceProfileRequired;
    final isShiftClosed = _controller.isShiftWindowClosed;

    final Color themeColor;
    final Color bgLight;
    final IconData iconData;
    final String title;
    final String desc;

    if (isFaceReq) {
      themeColor = const Color(0xFF7C3AED);
      bgLight = const Color(0xFFF5F3FF);
      iconData = Icons.face_retouching_off_rounded;
      title = 'Face Profile Required';
      desc =
          _controller.errorMessage ??
          'You have not registered your biometric face profile yet. Please complete face registration to enable automated attendance verification.';
    } else if (isShiftClosed) {
      themeColor = const Color(0xFFDC2626);
      bgLight = const Color(0xFFFEF2F2);
      iconData = Icons.event_busy_rounded;
      title = 'Attendance Window Closed';
      desc =
          _controller.errorMessage ??
          'The check-in/check-out time window for this shift has closed. Please contact your manager or HR.';
    } else {
      themeColor = const Color(0xFFEA580C);
      bgLight = const Color(0xFFFFF7ED);
      iconData = Icons.cloud_off_rounded;
      title = 'Session Initialization Failed';
      desc =
          _controller.errorMessage ??
          'Unable to connect to biometric verification service. Please try again.';
    }

    return Column(
      children: [
        _buildHeader(),
        Expanded(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 20),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    padding: const EdgeInsets.all(22),
                    decoration: BoxDecoration(
                      color: bgLight,
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: themeColor.withValues(alpha: 0.25),
                        width: 2,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: themeColor.withValues(alpha: 0.12),
                          blurRadius: 18,
                          spreadRadius: 4,
                        ),
                      ],
                    ),
                    child: Icon(iconData, size: 54, color: themeColor),
                  ),
                  const SizedBox(height: 20),
                  Text(
                    title,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      letterSpacing: -0.3,
                    ),
                  ),
                  const SizedBox(height: 10),
                  Text(
                    desc,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 13.5,
                      color: AppColors.textSecondary,
                      height: 1.45,
                    ),
                  ),
                  const SizedBox(height: 28),
                  if (isFaceReq) ...[
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: () async {
                          final registered = await showFaceRegistrationModal(
                            context: context,
                          );
                          if (registered == true && mounted) {
                            _controller.retrySession();
                          }
                        },
                        icon: const Icon(Icons.face_unlock_rounded, size: 20),
                        label: const Text(
                          'Register Face Profile Now',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                          ),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF7C3AED),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                          elevation: 2,
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    SizedBox(
                      width: double.infinity,
                      child: TextButton(
                        onPressed: () => Navigator.of(context).pop(false),
                        child: const Text(
                          'Cancel',
                          style: TextStyle(
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ),
                  ] else if (!isShiftClosed) ...[
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: () => _controller.retrySession(),
                        icon: const Icon(Icons.refresh_rounded, size: 20),
                        label: const Text(
                          'Try Again',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                          ),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                          elevation: 2,
                        ),
                      ),
                    ),
                    const SizedBox(height: 10),
                    SizedBox(
                      width: double.infinity,
                      child: TextButton(
                        onPressed: () => Navigator.of(context).pop(false),
                        child: const Text(
                          'Close',
                          style: TextStyle(
                            color: AppColors.textSecondary,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ),
                  ] else ...[
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton(
                        onPressed: () => Navigator.of(context).pop(false),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFFDC2626),
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(16),
                          ),
                        ),
                        child: const Text(
                          'Understood & Close',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 15,
                          ),
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildSuccessContent() {
    final checkInRes = _controller.checkInResult;
    final checkOutRes = _controller.checkOutResult;
    final isCheckIn = widget.isCheckIn;

    final localTime =
        (isCheckIn ? checkInRes?.checkInTime : checkOutRes?.checkOutTime)
            ?.toLocal() ??
        DateTime.now();
    final timeStr =
        '${localTime.hour.toString().padLeft(2, '0')}:${localTime.minute.toString().padLeft(2, '0')}';

    final themeColor = isCheckIn
        ? const Color(0xFF059669)
        : const Color(0xFFEA580C);
    final bgCircleColor = isCheckIn
        ? const Color(0xFFECFDF5)
        : const Color(0xFFFFF7ED);

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(22),
            decoration: BoxDecoration(
              color: bgCircleColor,
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: themeColor.withValues(alpha: 0.15),
                  blurRadius: 20,
                  spreadRadius: 4,
                ),
              ],
            ),
            child: Icon(
              Icons.check_circle_rounded,
              size: 58,
              color: themeColor,
            ),
          ),
          const SizedBox(height: 18),
          Text(
            isCheckIn ? 'Check-In Verified!' : 'Check-Out Verified!',
            style: const TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
              letterSpacing: -0.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            isCheckIn
                ? (checkInRes?.message ??
                      'Your attendance check-in is complete.')
                : (checkOutRes?.message ??
                      'Your shift attendance has been completed.'),
            textAlign: TextAlign.center,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.textSecondary,
            ),
          ),
          const SizedBox(height: 22),

          // Detail Summary Ticket Box
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: AppColors.borderLight),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withValues(alpha: 0.03),
                  blurRadius: 10,
                  offset: const Offset(0, 3),
                ),
              ],
            ),
            child: Column(
              children: [
                _buildRow(
                  isCheckIn ? 'Check-In Time' : 'Check-Out Time',
                  timeStr,
                ),
                const Divider(height: 18),
                _buildRow('Branch', widget.shift.branchName),
                const Divider(height: 18),
                if (isCheckIn) ...[
                  _buildRow(
                    'Punctuality',
                    checkInRes?.isLate == true
                        ? '${checkInRes?.totalLateMinutes ?? 0}m late'
                        : 'On Time ✓',
                    valueColor: checkInRes?.isLate == true
                        ? const Color(0xFFDC2626)
                        : const Color(0xFF059669),
                  ),
                ] else ...[
                  _buildRow(
                    'Working Hours',
                    '${checkOutRes?.workingHours.toStringAsFixed(1) ?? '0.0'} hrs',
                  ),
                  const Divider(height: 18),
                  _buildRow(
                    'Early Leave',
                    checkOutRes?.isEarlyLeave == true
                        ? '${checkOutRes?.totalEarlyLeaveMinutes ?? 0}m early'
                        : 'Completed ✓',
                    valueColor: checkOutRes?.isEarlyLeave == true
                        ? const Color(0xFFDC2626)
                        : const Color(0xFF059669),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 28),

          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: () => Navigator.of(context).pop(true),
              style: ElevatedButton.styleFrom(
                backgroundColor: isCheckIn
                    ? AppColors.primary
                    : const Color(0xFFEA580C),
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
                elevation: 2,
              ),
              child: const Text(
                'Done',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRow(String label, String value, {Color? valueColor}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
        Text(
          value,
          style: TextStyle(
            fontSize: 13.5,
            fontWeight: FontWeight.w700,
            color: valueColor ?? AppColors.textPrimary,
          ),
        ),
      ],
    );
  }

  Widget _buildErrorBanner(String message) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFFECACA)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(
            Icons.error_outline_rounded,
            color: Color(0xFFDC2626),
            size: 18,
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(
                color: Color(0xFF991B1B),
                fontSize: 12.5,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
