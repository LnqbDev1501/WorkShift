import 'dart:async';
import 'dart:io';
import 'dart:math' as math;
import 'package:camera/camera.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:geolocator/geolocator.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';
import '../../models/attendance/attendance_model.dart';
import '../../models/attendance/attendance_shift_model.dart';
import '../../services/attendance/attendance_service.dart';
import '../../utils/test_face_helper.dart';

class AttendanceController extends ChangeNotifier {
  final AttendanceService _service = AttendanceService();

  final EligibleShiftModel shift;
  final bool isCheckIn;

  AttendanceController({required this.shift, this.isCheckIn = true});

  bool isSubmitting = false;
  String? errorMessage;
  bool isInitializing = true;
  bool isSessionExpired = false;
  bool isShiftWindowClosed = false;
  bool isFaceProfileRequired = false;
  bool hasSession = false;

  // Camera Controller & Devices
  CameraController? cameraController;
  CameraDescription? selectedCamera;
  bool isCameraInitialized = false;

  // ML Kit Face Detector
  FaceDetector? _faceDetector;
  bool _isProcessingFrame = false;
  bool _isStreaming = false;
  bool _stepCooldown = false;

  // Session
  CheckInSessionResponse? checkInSession;
  CheckOutSessionResponse? checkOutSession;
  int remainingSeconds = 90;
  Timer? _countdownTimer;

  // Location
  double? currentLat;
  double? currentLng;
  double? currentAccuracy;
  double distanceMeters = 0.0;
  bool isLocating = false;
  bool isTestGpsMode = false;
  String? locationMessage;
  StreamSubscription<Position>? _positionStreamSubscription;

  // Biometrics & Live Challenge Tracking
  int currentChallengeIndex = 0;
  bool isDetecting = false;
  bool isFaceFramedProperly = false;
  DateTime? _challengeHoldStartTime;
  double challengeHoldProgress = 0.0;
  double remainingHoldSeconds = 2.0;
  String detectionStatus = 'Align face in the oval frame...';
  Uint8List? capturedImageBytes;
  String? capturedImageName;

  // Liveness score tracking: accumulate ML Kit confidence per challenge
  final List<double> _challengeScores = [];
  double _computedLivenessScore = 0.0;
  double get computedLivenessScore => _computedLivenessScore;

  double _calculateLivenessScore(List<double> scores) {
    if (scores.isEmpty) return 0.0;
    final avg = scores.reduce((a, b) => a + b) / scores.length;
    return double.parse(avg.toStringAsFixed(4));
  }

  // Results
  CheckInResponse? checkInResult;
  CheckOutResponse? checkOutResult;
  bool isSuccess = false;

  List<FaceChallengeItem> get challenges {
    if (isCheckIn) {
      return checkInSession?.challenges ?? [];
    } else {
      return checkOutSession?.challenges ?? [];
    }
  }

  FaceChallengeItem? get currentChallenge {
    if (challenges.isEmpty) return null;
    if (currentChallengeIndex < challenges.length) {
      return challenges[currentChallengeIndex];
    }
    return challenges.last;
  }

  bool get isLastChallenge {
    return challenges.isEmpty || currentChallengeIndex >= challenges.length - 1;
  }

  String? get sessionId {
    return isCheckIn ? checkInSession?.sessionId : checkOutSession?.sessionId;
  }

  double get effectiveAccuracy => math.min(currentAccuracy ?? 0.0, 50.0);
  double get effectiveDistanceMeters =>
      math.max(0.0, distanceMeters - effectiveAccuracy);

  bool get isWithinGeofence {
    if (currentLat == null || currentLng == null) return false;
    return effectiveDistanceMeters <= shift.maxAttendanceDistanceMeters;
  }

  /// Initialize attendance flow with Guard 3 (Fail-Fast)
  Future<void> initialize() async {
    isInitializing = true;
    errorMessage = null;
    isSessionExpired = false;
    isShiftWindowClosed = false;
    isFaceProfileRequired = false;
    hasSession = false;
    _challengeScores.clear();
    _computedLivenessScore = 0.0;
    notifyListeners();

    // 1. Guard 3: Pre-flight check on shift window expiration before touching hardware
    if (isCheckIn && shift.isCheckInExpired) {
      isShiftWindowClosed = true;
      isInitializing = false;
      errorMessage = 'The check-in window for this shift has closed.';
      notifyListeners();
      return;
    } else if (!isCheckIn && shift.isCheckoutExpired) {
      isShiftWindowClosed = true;
      isInitializing = false;
      errorMessage = 'The check-out window for this shift has closed.';
      notifyListeners();
      return;
    }

    // 2. Fetch real session from Backend (Guard 2 dynamic TTL applied inside)
    try {
      await createSession();
      hasSession = true;
    } catch (e) {
      hasSession = false;
      isInitializing = false;
      notifyListeners();
      return;
    }

    // 3. Setup ML Kit & Hardware safely (skipped on Web)
    if (!kIsWeb) {
      _initFaceDetector();
      await Future.wait([determinePosition(), initCamera()]);
    } else {
      _useBranchFallback();
      detectionStatus = 'Web Test Mode: Ready for 1-tap attendance';
    }

    isInitializing = false;

    // 5. Start real-time face detection stream if camera is ready and session active
    if (!kIsWeb &&
        isCameraInitialized &&
        !isSessionExpired &&
        !isShiftWindowClosed) {
      await startChallengeDetection();
    } else {
      notifyListeners();
    }
  }

  void _initFaceDetector() {
    _faceDetector?.close();
    _faceDetector = FaceDetector(
      options: FaceDetectorOptions(
        enableClassification: true, // Necessary for blink and smile detection
        enableLandmarks: true,
        minFaceSize: 0.1,
        performanceMode: FaceDetectorMode.accurate,
      ),
    );
  }

  /// Initialize Camera: defaults to FRONT camera for selfie biometric
  Future<void> initCamera() async {
    if (kIsWeb) return;

    isCameraInitialized = false;
    notifyListeners();

    try {
      final cameras = await availableCameras();
      if (cameras.isEmpty) {
        errorMessage = 'No camera found on this device.';
        notifyListeners();
        return;
      }

      // Find Front camera first
      CameraDescription? targetCam;
      for (final cam in cameras) {
        if (cam.lensDirection == CameraLensDirection.front) {
          targetCam = cam;
          break;
        }
      }
      targetCam ??= cameras.first;
      selectedCamera = targetCam;

      if (cameraController != null) {
        await _stopStream();
        await cameraController!.dispose();
        cameraController = null;
      }

      final isAndroid =
          !kIsWeb && defaultTargetPlatform == TargetPlatform.android;
      final controller = CameraController(
        targetCam,
        ResolutionPreset.medium,
        enableAudio: false,
        imageFormatGroup: isAndroid
            ? ImageFormatGroup.nv21
            : ImageFormatGroup.bgra8888,
      );

      await controller.initialize();
      cameraController = controller;
      isCameraInitialized = true;
    } catch (e) {
      errorMessage = 'Camera initialization failed: $e';
    } finally {
      notifyListeners();
    }
  }

  /// Start Face Detection Stream with Google ML Kit
  Future<void> startChallengeDetection() async {
    if (isSessionExpired || isShiftWindowClosed || !hasSession) return;
    if (cameraController == null || !cameraController!.value.isInitialized) {
      return;
    }
    if (_isStreaming) return;

    isDetecting = true;
    currentChallengeIndex = 0;
    detectionStatus =
        currentChallenge?.instruction ?? 'Align face in the frame...';
    notifyListeners();

    try {
      _isStreaming = true;
      await cameraController!.startImageStream((CameraImage image) {
        if (!_isProcessingFrame && isDetecting && !isSubmitting && !isSuccess) {
          _processCameraFrame(image);
        }
      });
    } catch (e) {
      debugPrint('Error starting camera image stream: $e');
    }
  }

  /// Process camera frame through Google ML Kit
  Future<void> _processCameraFrame(CameraImage image) async {
    // Guard 1: Immediate lock if session expired or shift closed
    if (isSessionExpired ||
        isShiftWindowClosed ||
        remainingSeconds <= 0 ||
        !hasSession) {
      _stopStream();
      return;
    }
    if (_stepCooldown || _faceDetector == null || selectedCamera == null) {
      return;
    }
    _isProcessingFrame = true;

    try {
      final inputImage = _inputImageFromCameraImage(image);
      if (inputImage == null) return;

      final faces = await _faceDetector!.processImage(inputImage);
      if (faces.isEmpty) {
        isFaceFramedProperly = false;
        detectionStatus = 'No face detected. Please face the camera.';
        notifyListeners();
        return;
      }

      final face = faces.first;
      final rawWidth = inputImage.metadata?.size.width ?? 1.0;
      final rawHeight = inputImage.metadata?.size.height ?? 1.0;
      final isRotated =
          inputImage.metadata?.rotation == InputImageRotation.rotation90deg ||
          inputImage.metadata?.rotation == InputImageRotation.rotation270deg;
      final actualWidth = isRotated ? rawHeight : rawWidth;
      final actualHeight = isRotated ? rawWidth : rawHeight;

      final centerXRatio = face.boundingBox.center.dx / actualWidth;
      final centerYRatio = face.boundingBox.center.dy / actualHeight;
      final faceWidthRatio = face.boundingBox.width / actualWidth;

      // 1. Validate Face Distance (Relaxed natural range)
      if (faceWidthRatio < 0.15) {
        isFaceFramedProperly = false;
        detectionStatus = 'Please move closer to the camera';
        notifyListeners();
        return;
      }

      if (faceWidthRatio > 0.90) {
        isFaceFramedProperly = false;
        detectionStatus = 'Please step back slightly';
        notifyListeners();
        return;
      }

      // 2. Validate Face Centering (Natural, comfortable range)
      if (centerXRatio < 0.15 ||
          centerXRatio > 0.85 ||
          centerYRatio < 0.15 ||
          centerYRatio > 0.85) {
        isFaceFramedProperly = false;
        detectionStatus = 'Please align your face inside the oval frame';
        notifyListeners();
        return;
      }

      // 3. Face is properly centered and sized!
      isFaceFramedProperly = true;
      _evaluateChallenge(face);
    } catch (e) {
      // Ignore frame errors
    } finally {
      _isProcessingFrame = false;
    }
  }

  /// Evaluate user's action against current challenge
  void _evaluateChallenge(Face face) {
    if (isSessionExpired ||
        isShiftWindowClosed ||
        remainingSeconds <= 0 ||
        !hasSession) {
      return;
    }
    final challenge = currentChallenge;
    if (challenge == null) return;

    bool isPassed = false;
    final action = challenge.action.toUpperCase();

    final smile = face.smilingProbability;
    final leftEye = face.leftEyeOpenProbability;
    final rightEye = face.rightEyeOpenProbability;
    final eulerY = face.headEulerAngleY ?? 0.0;
    final eulerX = face.headEulerAngleX ?? 0.0;
    final eulerZ = face.headEulerAngleZ ?? 0.0;

    switch (action) {
      case 'BLINK':
        // Blink detected when both eyes are closed (< 0.20)
        if (leftEye != null &&
            rightEye != null &&
            (leftEye < 0.20 && rightEye < 0.20)) {
          isPassed = true;
        } else {
          detectionStatus = 'Please blink your eyes';
        }
        break;

      case 'SMILE':
        // Smile detected when smiling probability > 0.50
        if (smile != null && smile > 0.50) {
          isPassed = true;
        } else {
          detectionStatus = 'Please smile at the camera';
        }
        break;

      case 'TURN_LEFT':
        // Turning to user's LEFT (eulerY > 10 on front camera)
        if (eulerY > 10) {
          isPassed = true;
        } else {
          detectionStatus = 'Turn head to the left';
        }
        break;

      case 'TURN_RIGHT':
        // Turning to user's RIGHT (eulerY < -10 on front camera)
        if (eulerY < -10) {
          isPassed = true;
        } else {
          detectionStatus = 'Turn head to the right';
        }
        break;

      case 'NOD':
        if (eulerX < -10) {
          isPassed = true;
        } else {
          detectionStatus = 'Please nod your head slightly';
        }
        break;

      case 'LOOK_STRAIGHT':
      default:
        if (eulerY.abs() < 10 && eulerZ.abs() < 10) {
          isPassed = true;
        } else {
          detectionStatus = 'Look straight at the camera';
        }
        break;
    }

    if (isPassed) {
      double score = 0.95;
      if (action == 'BLINK') {
        final eyeVal = math.min(leftEye ?? 0.0, rightEye ?? 0.0);
        score = (1.0 - eyeVal).clamp(0.65, 1.0);
      } else if (action == 'SMILE' && smile != null) {
        score = smile.clamp(0.60, 1.0);
      }

      // Instant pass for blink challenge as requested
      if (action == 'BLINK') {
        _challengeHoldStartTime = null;
        challengeHoldProgress = 1.0;
        remainingHoldSeconds = 0.0;
        _handleChallengePassed(challenge.action, score);
        return;
      }

      _challengeHoldStartTime ??= DateTime.now();

      final elapsedMs = DateTime.now()
          .difference(_challengeHoldStartTime!)
          .inMilliseconds;
      const targetHoldMs = 2000;

      challengeHoldProgress = (elapsedMs / targetHoldMs).clamp(0.0, 1.0);
      final remainingSec = math.max(0.0, (targetHoldMs - elapsedMs) / 1000.0);
      remainingHoldSeconds = remainingSec;

      if (elapsedMs >= targetHoldMs) {
        _challengeHoldStartTime = null;
        challengeHoldProgress = 1.0;
        remainingHoldSeconds = 0.0;
        _handleChallengePassed(challenge.action, score);
      } else {
        detectionStatus =
            '✓ Position held! Keep steady: ${remainingSec.toStringAsFixed(1)}s remaining...';
        notifyListeners();
      }
    } else {
      if (_challengeHoldStartTime != null) {
        _challengeHoldStartTime = null;
        challengeHoldProgress = 0.0;
        remainingHoldSeconds = 2.0;
      }
      notifyListeners();
    }
  }

  /// Advance to next challenge or finish verification
  void _handleChallengePassed(String action, [double score = 0.95]) {
    if (isSessionExpired ||
        isShiftWindowClosed ||
        remainingSeconds <= 0 ||
        !hasSession) {
      return;
    }
    if (_stepCooldown) return;
    _stepCooldown = true;

    _challengeScores.add(score);
    _challengeHoldStartTime = null;
    challengeHoldProgress = 1.0;
    remainingHoldSeconds = 0.0;

    detectionStatus = '✓ $action Completed!';
    notifyListeners();

    Timer(const Duration(milliseconds: 500), () async {
      _stepCooldown = false;
      if (isSessionExpired ||
          isShiftWindowClosed ||
          remainingSeconds <= 0 ||
          !hasSession) {
        return;
      }
      challengeHoldProgress = 0.0;
      remainingHoldSeconds = 2.0;

      if (currentChallengeIndex < challenges.length - 1) {
        currentChallengeIndex++;
        detectionStatus = currentChallenge?.instruction ?? 'Next challenge...';
        notifyListeners();
      } else {
        // Completed all challenges! Compute liveness score and snap picture!
        _computedLivenessScore = _calculateLivenessScore(_challengeScores);
        isDetecting = false;
        detectionStatus = '✓ All steps completed! Capturing photo...';
        notifyListeners();

        await _stopStream();
        await snapAndSubmit();
      }
    });
  }

  Future<void> _stopStream() async {
    if (_isStreaming && cameraController != null) {
      try {
        await cameraController!.stopImageStream();
      } catch (_) {}
      _isStreaming = false;
    }
  }

  /// Convert CameraImage into ML Kit InputImage
  InputImage? _inputImageFromCameraImage(CameraImage image) {
    if (selectedCamera == null || cameraController == null) return null;

    final sensorOrientation = selectedCamera!.sensorOrientation;
    InputImageRotation? rotation;

    if (Platform.isAndroid) {
      var rotationCompensation =
          _orientations[cameraController!.value.deviceOrientation];
      if (rotationCompensation == null) return null;
      if (selectedCamera!.lensDirection == CameraLensDirection.front) {
        rotationCompensation = (sensorOrientation + rotationCompensation) % 360;
      } else {
        rotationCompensation =
            (sensorOrientation - rotationCompensation + 360) % 360;
      }
      rotation = InputImageRotationValue.fromRawValue(rotationCompensation);
    } else {
      rotation = InputImageRotationValue.fromRawValue(sensorOrientation);
    }
    if (rotation == null) return null;

    final format = InputImageFormatValue.fromRawValue(image.format.raw);
    if (format == null) return null;

    Uint8List bytes;
    if (image.planes.length == 1) {
      bytes = image.planes.first.bytes;
    } else {
      final WriteBuffer allBytes = WriteBuffer();
      for (final Plane plane in image.planes) {
        allBytes.putUint8List(plane.bytes);
      }
      bytes = allBytes.done().buffer.asUint8List();
    }

    return InputImage.fromBytes(
      bytes: bytes,
      metadata: InputImageMetadata(
        size: Size(image.width.toDouble(), image.height.toDouble()),
        rotation: rotation,
        format: format,
        bytesPerRow: image.planes.first.bytesPerRow,
      ),
    );
  }

  static const Map<DeviceOrientation, int> _orientations = {
    DeviceOrientation.portraitUp: 0,
    DeviceOrientation.landscapeLeft: 90,
    DeviceOrientation.portraitDown: 180,
    DeviceOrientation.landscapeRight: 270,
  };

  /// Create Real Backend Session with Guard 2 (Dynamic TTL & Cut-off Integration)
  Future<void> createSession() async {
    errorMessage = null;
    _countdownTimer?.cancel();
    isSessionExpired = false;

    try {
      int serverTtl = 90;
      if (isCheckIn) {
        checkInSession = await _service.createCheckInSession(
          assignmentId: shift.assignmentId,
        );
        serverTtl = checkInSession!.expiresInSeconds;
      } else {
        checkOutSession = await _service.createCheckOutSession(
          shift.assignmentId,
        );
        serverTtl = checkOutSession!.expiresInSeconds;
      }

      // Guard 2: Compute cutoff time and dynamic TTL
      final cutoff = isCheckIn
          ? (shift.checkInCutoffAt ?? shift.scheduledEndAt)
          : (shift.checkOutCutoffAt ??
                shift.scheduledEndAt.add(const Duration(minutes: 15)));

      int effectiveTtl = serverTtl;
      final secondsUntilCutoff = cutoff
          .toLocal()
          .difference(DateTime.now())
          .inSeconds;
      if (secondsUntilCutoff <= 0) {
        isShiftWindowClosed = true;
        isSessionExpired = true;
        remainingSeconds = 0;
        errorMessage = isCheckIn
            ? 'The check-in window for this shift has closed.'
            : 'The check-out window for this shift has closed.';
        notifyListeners();
        throw Exception(errorMessage);
      }
      effectiveTtl = math.min(serverTtl, secondsUntilCutoff);

      remainingSeconds = effectiveTtl;
      currentChallengeIndex = 0;
      _startTimer(cutoff);
      notifyListeners();
    } catch (e) {
      final str = e.toString();
      final lower = str.toLowerCase();
      if (lower.contains('face profile') ||
          lower.contains('register your face')) {
        isFaceProfileRequired = true;
        errorMessage =
            'You must register your face profile before performing attendance verification.';
      } else if (lower.contains('cut-off') ||
          lower.contains('closed') ||
          lower.contains('window')) {
        isShiftWindowClosed = true;
        errorMessage = str.replaceAll('Exception:', '').trim();
      } else {
        errorMessage = str.replaceAll('Exception:', '').trim();
      }
      notifyListeners();
      rethrow;
    }
  }

  void _startTimer(DateTime cutoff) {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      // Guard 2: Real-time cut-off verification
      if (DateTime.now().isAfter(cutoff.toLocal())) {
        timer.cancel();
        _handleSessionExpired(shiftClosed: true);
        return;
      }

      // Guard 1: Countdown check
      if (remainingSeconds > 1) {
        remainingSeconds--;
        notifyListeners();
      } else {
        timer.cancel();
        remainingSeconds = 0;
        _handleSessionExpired(shiftClosed: false);
      }
    });
  }

  void _handleSessionExpired({bool shiftClosed = false}) {
    isSessionExpired = true;
    if (shiftClosed) isShiftWindowClosed = true;
    isDetecting = false;
    _challengeHoldStartTime = null;
    challengeHoldProgress = 0.0;
    remainingHoldSeconds = 0.0;

    _stopStream();

    if (shiftClosed) {
      detectionStatus = 'Shift attendance window closed.';
      errorMessage = isCheckIn
          ? 'Check-in cut-off deadline reached. Attendance window is closed.'
          : 'Check-out cut-off deadline reached. Attendance window is closed.';
    } else {
      detectionStatus = 'Verification session expired.';
      errorMessage =
          'Verification session expired (90s timeout). Please try again.';
    }
    notifyListeners();
  }

  /// Retry session after expiration or error without leaving modal
  Future<void> retrySession() async {
    _countdownTimer?.cancel();
    await _stopStream();
    checkInSession = null;
    checkOutSession = null;
    isSuccess = false;
    isSubmitting = false;
    isSessionExpired = false;
    isShiftWindowClosed = false;
    isFaceProfileRequired = false;
    hasSession = false;
    currentChallengeIndex = 0;
    challengeHoldProgress = 0.0;
    remainingHoldSeconds = 2.0;
    capturedImageBytes = null;
    capturedImageName = null;
    await initialize();
  }

  /// Determine GPS Location: Requests runtime permission and reads real GPS
  Future<void> determinePosition({bool requestNewPermission = true}) async {
    if (isTestGpsMode) {
      _useBranchFallback();
      notifyListeners();
      return;
    }

    isLocating = true;
    locationMessage = null;
    notifyListeners();

    try {
      // 1. Check if location services are enabled
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        locationMessage = 'Please enable GPS location on your device to check in';
        _stopPositionStream();
        currentLat = null;
        currentLng = null;
        currentAccuracy = null;
        distanceMeters = 0.0;
        isLocating = false;
        notifyListeners();
        return;
      }

      // 2. Check and Request Location Permission
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied && requestNewPermission) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        locationMessage = 'Location permission is denied. Please grant permission to check in';
        _stopPositionStream();
        currentLat = null;
        currentLng = null;
        currentAccuracy = null;
        distanceMeters = 0.0;
        isLocating = false;
        notifyListeners();
        return;
      }

      // 3. Get Real Satellite GPS
      final position = await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );

      currentLat = position.latitude;
      currentLng = position.longitude;
      currentAccuracy = position.accuracy;
      _calculateDistance();
      locationMessage = 'Real GPS: ${distanceMeters.toStringAsFixed(1)}m away';

      // 4. Start Live GPS Stream (auto-updates as staff moves)
      _startPositionStream();
    } catch (e) {
      _stopPositionStream();
      currentLat = null;
      currentLng = null;
      currentAccuracy = null;
      distanceMeters = 0.0;
      locationMessage = 'Unable to determine real GPS location: $e';
    } finally {
      isLocating = false;
      notifyListeners();
    }
  }

  void _startPositionStream() {
    _positionStreamSubscription?.cancel();
    const locationSettings = LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 1, // trigger update every 1 meter moved
    );
    _positionStreamSubscription =
        Geolocator.getPositionStream(locationSettings: locationSettings).listen(
          (Position position) {
            if (isTestGpsMode) return;
            currentLat = position.latitude;
            currentLng = position.longitude;
            currentAccuracy = position.accuracy;
            _calculateDistance();
            locationMessage =
                'Real GPS: ${distanceMeters.toStringAsFixed(1)}m (±${currentAccuracy?.toStringAsFixed(1) ?? "0"}m)';
            notifyListeners();
          },
          onError: (e) {
            // Stream connection error handled gracefully
          },
        );
  }

  void _stopPositionStream() {
    _positionStreamSubscription?.cancel();
    _positionStreamSubscription = null;
  }

  void _useBranchFallback() {
    _stopPositionStream();
    if (shift.branchLatitude != null && shift.branchLongitude != null) {
      currentLat = shift.branchLatitude;
      currentLng = shift.branchLongitude;
      currentAccuracy = 5.0;
      _calculateDistance();
    } else {
      currentLat = null;
      currentLng = null;
      currentAccuracy = null;
      distanceMeters = 0.0;
      locationMessage = 'Branch does not have GPS coordinates configured';
    }
  }

  /// Toggle between Real GPS and Mock GPS (0m) for testing
  void toggleTestGps() {
    isTestGpsMode = !isTestGpsMode;
    if (isTestGpsMode) {
      _stopPositionStream();
      _useBranchFallback();
      locationMessage = 'Test GPS mode active (0m)';
    } else {
      determinePosition(requestNewPermission: true);
    }
    notifyListeners();
  }

  void _calculateDistance() {
    if (currentLat != null &&
        currentLng != null &&
        shift.branchLatitude != null &&
        shift.branchLongitude != null) {
      distanceMeters = calculateHaversineDistance(
        currentLat!,
        currentLng!,
        shift.branchLatitude!,
        shift.branchLongitude!,
      );
    } else {
      distanceMeters = 0.0;
    }
  }

  static double calculateHaversineDistance(
    double lat1,
    double lon1,
    double lat2,
    double lon2,
  ) {
    const earthRadius = 6371000.0;
    final dLat = (lat2 - lat1) * (math.pi / 180.0);
    final dLon = (lon2 - lon1) * (math.pi / 180.0);
    final rLat1 = lat1 * (math.pi / 180.0);
    final rLat2 = lat2 * (math.pi / 180.0);

    final a =
        math.sin(dLat / 2) * math.sin(dLat / 2) +
        math.cos(rLat1) *
            math.cos(rLat2) *
            math.sin(dLon / 2) *
            math.sin(dLon / 2);
    final c = 2 * math.atan2(math.sqrt(a), math.sqrt(1 - a));
    return double.parse((earthRadius * c).toStringAsFixed(1));
  }

  /// Complete verification: Snap from Camera & Submit to Backend
  Future<bool> snapAndSubmit() async {
    if (isSessionExpired || isShiftWindowClosed || remainingSeconds <= 0) {
      errorMessage = 'Verification session has expired. Please retry.';
      notifyListeners();
      return false;
    }

    if (sessionId == null) {
      errorMessage = 'No active verification session found.';
      notifyListeners();
      return false;
    }

    if (isSubmitting) return false;
    isSubmitting = true;
    errorMessage = null;
    notifyListeners();

    try {
      await _stopStream();

      // 1. Capture frame directly from live camera
      if (cameraController == null || !cameraController!.value.isInitialized) {
        errorMessage =
            'Camera is not available. Please restart the app and try again.';
        isSubmitting = false;
        notifyListeners();
        return false;
      }

      try {
        final xfile = await cameraController!.takePicture();
        capturedImageBytes = await xfile.readAsBytes();
        capturedImageName = xfile.name;
      } catch (e) {
        errorMessage = 'Failed to capture photo from camera: $e';
        isSubmitting = false;
        notifyListeners();
        return false;
      }

      if (capturedImageBytes == null || capturedImageBytes!.isEmpty) {
        errorMessage = 'Captured photo is empty. Please try again.';
        isSubmitting = false;
        notifyListeners();
        return false;
      }

      if (currentLat == null || currentLng == null) {
        errorMessage =
            'GPS location unavailable. Please enable GPS and grant location permission to check in.';
        isSubmitting = false;
        notifyListeners();
        return false;
      }

      // 2. Submit to backend
      final liveness = _computedLivenessScore > 0 ? _computedLivenessScore : 1.0;
      if (isCheckIn) {
        checkInResult = await _service.checkIn(
          assignmentId: shift.assignmentId,
          sessionId: sessionId!,
          latitude: currentLat!,
          longitude: currentLng!,
          gpsAccuracy: currentAccuracy ?? 5.0,
          livenessScore: liveness,
          imageBytes: capturedImageBytes!,
          filename: capturedImageName ?? 'live_camera.jpg',
        );
      } else {
        checkOutResult = await _service.checkOut(
          assignmentId: shift.assignmentId,
          sessionId: sessionId!,
          latitude: currentLat!,
          longitude: currentLng!,
          gpsAccuracy: currentAccuracy ?? 5.0,
          livenessScore: liveness,
          imageBytes: capturedImageBytes!,
          filename: capturedImageName ?? 'live_camera.jpg',
        );
      }

      _countdownTimer?.cancel();
      isSuccess = true;
      isSubmitting = false;
      notifyListeners();
      return true;
    } catch (e) {
      errorMessage = e.toString();
      isSubmitting = false;
      notifyListeners();
      return false;
    }
  }

  /// One-Click Test Check-In / Check-Out for Web (using embedded sample face)
  Future<bool> testSubmitWithSampleFace() async {
    if (sessionId == null) {
      try {
        await createSession();
        hasSession = true;
      } catch (e) {
        errorMessage = 'Failed to create verification session: $e';
        notifyListeners();
        return false;
      }
    }

    isSubmitting = true;
    errorMessage = null;
    notifyListeners();

    try {
      final sampleBytes = await TestFaceHelper.getSampleFaceBytes();
      capturedImageBytes = sampleBytes;
      capturedImageName = 'sample_face.jpg';

      if (currentLat == null || currentLng == null) {
        _useBranchFallback();
      }

      if (isCheckIn) {
        checkInResult = await _service.checkIn(
          assignmentId: shift.assignmentId,
          sessionId: sessionId!,
          latitude: currentLat ?? shift.branchLatitude ?? 0.0,
          longitude: currentLng ?? shift.branchLongitude ?? 0.0,
          gpsAccuracy: currentAccuracy ?? 5.0,
          livenessScore: 1.0,
          imageBytes: sampleBytes,
          filename: 'sample_face.jpg',
        );
      } else {
        checkOutResult = await _service.checkOut(
          assignmentId: shift.assignmentId,
          sessionId: sessionId!,
          latitude: currentLat ?? shift.branchLatitude ?? 0.0,
          longitude: currentLng ?? shift.branchLongitude ?? 0.0,
          gpsAccuracy: currentAccuracy ?? 5.0,
          livenessScore: 1.0,
          imageBytes: sampleBytes,
          filename: 'sample_face.jpg',
        );
      }

      _countdownTimer?.cancel();
      isSuccess = true;
      isSubmitting = false;
      notifyListeners();
      return true;
    } catch (e) {
      errorMessage = e.toString();
      isSubmitting = false;
      notifyListeners();
      return false;
    }
  }

  @override
  void dispose() {
    _countdownTimer?.cancel();
    _stopPositionStream();
    _stopStream();
    _faceDetector?.close();
    cameraController?.dispose();
    super.dispose();
  }
}
