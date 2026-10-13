import 'package:camera/camera.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import '../../controllers/face_registration_controller.dart';
import '../../theme/app_colors.dart';

/// Function to open Face Registration Modal from anywhere in the app
Future<bool?> showFaceRegistrationModal({
  required BuildContext context,
}) {
  return showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    isDismissible: false,
    enableDrag: false,
    backgroundColor: Colors.transparent,
    builder: (ctx) => const FaceRegistrationModalPopup(),
  );
}

class FaceRegistrationModalPopup extends StatefulWidget {
  const FaceRegistrationModalPopup({super.key});

  @override
  State<FaceRegistrationModalPopup> createState() => _FaceRegistrationModalPopupState();
}

class _FaceRegistrationModalPopupState extends State<FaceRegistrationModalPopup>
    with SingleTickerProviderStateMixin {
  late final FaceRegistrationController _controller;
  late final AnimationController _scanAnimController;
  late final Animation<double> _scanAnimation;

  @override
  void initState() {
    super.initState();
    _controller = FaceRegistrationController();
    _controller.initialize();

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

              return Column(
                children: [
                  _buildHeader(),
                  Expanded(
                    child: SingleChildScrollView(
                      padding: const EdgeInsets.symmetric(horizontal: 20),
                      child: Column(
                        children: [
                          const SizedBox(height: 12),
                          if (_controller.errorMessage != null) ...[
                            _buildErrorBanner(_controller.errorMessage!),
                            const SizedBox(height: 12),
                          ],
                          _buildInstructionPill(),
                          const SizedBox(height: 14),
                          _buildLiveCameraView(),
                          const SizedBox(height: 14),
                          _buildChallengeCard(),
                          const SizedBox(height: 16),
                        ],
                      ),
                    ),
                  ),
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
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 10, 12, 14),
      decoration: const BoxDecoration(
        border: Border(bottom: BorderSide(color: AppColors.borderLight)),
      ),
      child: Column(
        children: [
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
                  color: const Color(0xFFF3E8FF),
                  shape: BoxShape.circle,
                  border: Border.all(color: const Color(0xFF8B5CF6).withValues(alpha: 0.3)),
                ),
                child: const Icon(
                  Icons.face_retouching_natural_rounded,
                  color: Color(0xFF7C3AED),
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Register Face Profile',
                      style: TextStyle(
                        fontSize: 17,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                        letterSpacing: -0.3,
                      ),
                    ),
                    SizedBox(height: 2),
                    Text(
                      'Official Biometric Face Enrollment',
                      style: TextStyle(
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
                icon: const Icon(Icons.close_rounded, color: AppColors.textSecondary, size: 20),
                onPressed: () => Navigator.of(context).pop(_controller.isSuccess),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildInstructionPill() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xFFFAF5FF),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE9D5FF)),
      ),
      child: const Row(
        children: [
          Icon(Icons.security_rounded, size: 18, color: Color(0xFF7C3AED)),
          SizedBox(width: 10),
          Expanded(
            child: Text(
              'Position your face inside the frame and complete the 2 quick verification gestures.',
              style: TextStyle(
                fontSize: 12.5,
                fontWeight: FontWeight.w600,
                color: Color(0xFF581C87),
              ),
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
    final frameColor = isFramed ? const Color(0xFF10B981) : const Color(0xFFEF4444);

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
          if (kIsWeb)
            Image.asset('assets/sample_face.jpg', fit: BoxFit.cover)
          else if (_controller.isCameraInitialized && _controller.cameraController != null)
            CameraPreview(_controller.cameraController!)
          else
            _buildCameraLoadingPlaceholder(),

          // Laser Scanning Beam
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
                      colors: [Colors.transparent, Color(0xFF8B5CF6), Color(0xFFA78BFA), Colors.transparent],
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFF8B5CF6).withValues(alpha: 0.8),
                        blurRadius: 8,
                        spreadRadius: 2,
                      ),
                    ],
                  ),
                ),
              );
            },
          ),

          // Corner Target Reticles
          Center(
            child: SizedBox(
              width: 236,
              height: 286,
              child: Stack(
                children: [
                  _buildReticleCorner(isTop: true, isLeft: true, color: frameColor),
                  _buildReticleCorner(isTop: true, isLeft: false, color: frameColor),
                  _buildReticleCorner(isTop: false, isLeft: true, color: frameColor),
                  _buildReticleCorner(isTop: false, isLeft: false, color: frameColor),
                ],
              ),
            ),
          ),

          // Face Scanner Oval Guide
          Center(
            child: Container(
              width: 210,
              height: 260,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(105),
                border: Border.all(
                  color: frameColor,
                  width: 2.5,
                ),
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

          // Top Centering Badge
          Positioned(
            top: 48,
            left: 20,
            right: 20,
            child: Center(
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 250),
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 5.5),
                decoration: BoxDecoration(
                  color: isFramed
                      ? const Color(0xFF065F46).withValues(alpha: 0.9)
                      : const Color(0xFF991B1B).withValues(alpha: 0.9),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(
                    color: isFramed ? const Color(0xFF34D399) : const Color(0xFFF87171),
                    width: 1,
                  ),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      isFramed ? Icons.check_circle_rounded : Icons.warning_amber_rounded,
                      size: 14,
                      color: Colors.white,
                    ),
                    const SizedBox(width: 6),
                    Text(
                      isFramed ? 'Face Centered ✓' : 'Align face inside oval',
                      style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // Top Right Session Timer Badge
          Positioned(
            top: 12,
            right: 12,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.65),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: remaining < 20 ? const Color(0xFFEF4444) : Colors.white30,
                ),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.timer_outlined,
                    size: 14,
                    color: remaining < 20 ? const Color(0xFFEF4444) : Colors.white,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    '$minutes:$seconds',
                    style: TextStyle(
                      color: remaining < 20 ? const Color(0xFFEF4444) : Colors.white,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
          ),

          // Bottom Live AI Status inside camera with Progress Bar
          Positioned(
            bottom: 12,
            left: 16,
            right: 16,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8.5),
              decoration: BoxDecoration(
                color: Colors.black.withValues(alpha: 0.85),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: _controller.challengeHoldProgress > 0
                      ? const Color(0xFF8B5CF6)
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
                              ? const Color(0xFFA78BFA)
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
                        valueColor: const AlwaysStoppedAnimation<Color>(Color(0xFF8B5CF6)),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildReticleCorner({required bool isTop, required bool isLeft, required Color color}) {
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
            bottom: !isTop ? BorderSide(color: color, width: 3) : BorderSide.none,
            left: isLeft ? BorderSide(color: color, width: 3) : BorderSide.none,
            right: !isLeft ? BorderSide(color: color, width: 3) : BorderSide.none,
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
              color: const Color(0xFF8B5CF6).withValues(alpha: 0.15),
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.face_retouching_natural_rounded,
              color: Color(0xFFA78BFA),
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
            'Look directly into the camera',
            style: TextStyle(color: Colors.white70, fontSize: 12),
          ),
        ],
      ),
    );
  }

  Widget _buildChallengeCard() {
    final current = _controller.currentChallenge;
    final challenges = _controller.challenges;
    final currentIndex = _controller.currentChallengeIndex;
    final total = challenges.length;

    if (current == null) {
      return const SizedBox.shrink();
    }

    return Container(
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
                decoration: const BoxDecoration(
                  color: Color(0xFF7C3AED),
                  shape: BoxShape.circle,
                ),
                child: Icon(_getChallengeIcon(current.action), color: Colors.white, size: 20),
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
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: Color(0xFF7C3AED),
                          ),
                        ),
                        Text(
                          current.action,
                          style: const TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w800,
                            color: Color(0xFF7C3AED),
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
                        ? const Color(0xFF10B981)
                        : (isCurrent ? const Color(0xFF7C3AED) : const Color(0xFFCBD5E1)),
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  Widget _buildBottomBar() {
    final isSubmitting = _controller.isSubmitting;

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
              color: isSubmitting ? const Color(0xFF7C3AED) : const Color(0xFFFAF5FF),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: isSubmitting ? const Color(0xFF7C3AED) : const Color(0xFFE9D5FF),
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
                    'Verifying & Registering Face Profile...',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13.5,
                      color: Colors.white,
                    ),
                  ),
                ] else ...[
                  const Icon(Icons.verified_user_rounded, size: 18, color: Color(0xFF7C3AED)),
                  const SizedBox(width: 8),
                  const Text(
                    'Google ML Kit Face Biometrics Active',
                    style: TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 13.5,
                      color: Color(0xFF581C87),
                    ),
                  ),
                ],
              ],
            ),
          ),
          if (kIsWeb || !_controller.isCameraInitialized) ...[
            const SizedBox(height: 10),
            ElevatedButton.icon(
              onPressed: isSubmitting ? null : () => _controller.testSubmitWithSampleFace(),
              icon: const Icon(Icons.flash_on_rounded, color: Colors.white, size: 20),
              label: const Text(
                'Test 1 Chạm: Đăng Ký Khuôn Mặt Ngay (Web)',
                style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14, color: Colors.white),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF7C3AED),
                minimumSize: const Size(double.infinity, 48),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                elevation: 2,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSuccessContent() {
    final result = _controller.verifyResult;

    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            padding: const EdgeInsets.all(22),
            decoration: BoxDecoration(
              color: const Color(0xFFFAF5FF),
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: const Color(0xFF7C3AED).withValues(alpha: 0.18),
                  blurRadius: 20,
                  spreadRadius: 4,
                ),
              ],
            ),
            child: const Icon(
              Icons.check_circle_rounded,
              size: 58,
              color: Color(0xFF7C3AED),
            ),
          ),
          const SizedBox(height: 18),
          const Text(
            'Face Profile Registered!',
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
              letterSpacing: -0.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            result?.message ?? 'Your biometric face profile has been registered and activated successfully.',
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
          ),
          const SizedBox(height: 22),
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: const Color(0xFFF8FAFC),
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: AppColors.borderLight),
            ),
            child: Column(
              children: [
                _buildRow('Biometric Status', 'Active ✓', valueColor: const Color(0xFF059669)),
                const Divider(height: 18),
                _buildRow('Face Quality', '${((result?.faceQualityScore ?? 0.95) * 100).toInt()}%'),
                const Divider(height: 18),
                _buildRow('Liveness Score', '${((result?.livenessScore ?? 0.96) * 100).toInt()}%'),
              ],
            ),
          ),
          const SizedBox(height: 28),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton(
              onPressed: () => Navigator.of(context).pop(true),
              style: ElevatedButton.styleFrom(
                backgroundColor: const Color(0xFF7C3AED),
                foregroundColor: Colors.white,
                padding: const EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                elevation: 2,
              ),
              child: const Text('Done', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 16)),
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
        Text(label, style: const TextStyle(fontSize: 13, color: AppColors.textSecondary)),
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
          const Icon(Icons.error_outline_rounded, color: Color(0xFFDC2626), size: 18),
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
