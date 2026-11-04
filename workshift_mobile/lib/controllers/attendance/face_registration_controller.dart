import 'dart:async';
import 'dart:io';
import 'dart:math' as math;
import 'package:camera/camera.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_mlkit_face_detection/google_mlkit_face_detection.dart';
import '../../models/attendance/attendance_model.dart';
import '../../models/attendance/face_registration_model.dart';
import '../../services/attendance/face_registration_service.dart';
import '../../utils/test_face_helper.dart';

class FaceRegistrationController extends ChangeNotifier {
  final FaceRegistrationService _service = FaceRegistrationService();

  bool isSubmitting = false;
  String? errorMessage;

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
  FaceRegistrationSessionModel? registrationSession;
  int remainingSeconds = 90;
  Timer? _countdownTimer;

  // Biometrics & Live Challenge Tracking
  int currentChallengeIndex = 0;
  bool isDetecting = false;
  bool isFaceFramedProperly = false;
  DateTime? _challengeHoldStartTime;
  double challengeHoldProgress = 0.0;
  double remainingHoldSeconds = 2.0;
  String detectionStatus = 'Align face in the oval frame...';
  Uint8List? capturedImageBytes;

  // Results
  FaceRegistrationVerifyResult? verifyResult;
  bool isSuccess = false;

  List<FaceChallengeItem> get challenges =>
      registrationSession?.challenges ?? [];

  FaceChallengeItem? get currentChallenge {
    if (challenges.isEmpty) return null;
    if (currentChallengeIndex < challenges.length) {
      return challenges[currentChallengeIndex];
    }
    return challenges.last;
  }

  String? get sessionId => registrationSession?.sessionId;

  /// Initialize Face Registration flow
  Future<void> initialize() async {
    errorMessage = null;
    notifyListeners();

    if (!kIsWeb) {
      _initFaceDetector();
    }

    try {
      await createSession();
    } catch (e) {
      errorMessage = e.toString();
      notifyListeners();
      return;
    }

    if (!kIsWeb) {
      await initCamera();

      if (isCameraInitialized) {
        await startChallengeDetection();
      }
    } else {
      detectionStatus = 'Web Test Mode: Sẵn sàng đăng ký 1 chạm';
      notifyListeners();
    }
  }

  void _initFaceDetector() {
    _faceDetector?.close();
    _faceDetector = FaceDetector(
      options: FaceDetectorOptions(
        enableClassification: true,
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
    if (cameraController == null || !cameraController!.value.isInitialized) {
      return;
    }
    if (_isStreaming) return;

    isDetecting = true;
    currentChallengeIndex = 0;
    detectionStatus =
        currentChallenge?.instruction ?? 'Align face inside oval frame...';
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

      // 1. Validate Face Distance
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

      // 2. Validate Face Centering
      if (centerXRatio < 0.15 ||
          centerXRatio > 0.85 ||
          centerYRatio < 0.15 ||
          centerYRatio > 0.85) {
        isFaceFramedProperly = false;
        detectionStatus = 'Please align your face inside the oval frame';
        notifyListeners();
        return;
      }

      // 3. Face framed properly
      isFaceFramedProperly = true;
      _evaluateChallenge(face);
    } catch (_) {
    } finally {
      _isProcessingFrame = false;
    }
  }

  /// Evaluate user's action against current challenge
  void _evaluateChallenge(Face face) {
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
        if (leftEye != null &&
            rightEye != null &&
            (leftEye < 0.35 || rightEye < 0.35)) {
          isPassed = true;
        } else {
          detectionStatus = 'Please blink your eyes';
        }
        break;

      case 'SMILE':
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
      if (action == 'BLINK') {
        _challengeHoldStartTime = null;
        challengeHoldProgress = 1.0;
        remainingHoldSeconds = 0.0;
        _handleChallengePassed(challenge.action);
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
        _handleChallengePassed(challenge.action);
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

  void _handleChallengePassed(String action) {
    if (_stepCooldown) return;
    _stepCooldown = true;

    _challengeHoldStartTime = null;
    challengeHoldProgress = 1.0;
    remainingHoldSeconds = 0.0;

    detectionStatus = '✓ $action Completed!';
    notifyListeners();

    Timer(const Duration(milliseconds: 500), () async {
      _stepCooldown = false;
      challengeHoldProgress = 0.0;
      remainingHoldSeconds = 2.0;

      if (currentChallengeIndex < challenges.length - 1) {
        currentChallengeIndex++;
        detectionStatus = currentChallenge?.instruction ?? 'Next challenge...';
        notifyListeners();
      } else {
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

  /// Create Real Backend Session
  Future<void> createSession() async {
    errorMessage = null;
    _countdownTimer?.cancel();

    try {
      registrationSession = await _service.createSession();
      remainingSeconds = registrationSession!.expiresInSeconds;
      currentChallengeIndex = 0;
      _startTimer();
      notifyListeners();
    } catch (e) {
      errorMessage = e.toString();
      notifyListeners();
      rethrow;
    }
  }

  void _startTimer() {
    _countdownTimer?.cancel();
    _countdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (remainingSeconds > 0) {
        remainingSeconds--;
        notifyListeners();
      } else {
        timer.cancel();
        errorMessage = 'Registration session expired. Please reopen.';
        notifyListeners();
      }
    });
  }

  /// Complete Face Registration: Snap from Camera & Submit to Backend
  Future<bool> snapAndSubmit() async {
    if (sessionId == null) {
      errorMessage = 'No active face registration session found.';
      notifyListeners();
      return false;
    }

    isSubmitting = true;
    errorMessage = null;
    notifyListeners();

    try {
      await _stopStream();

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

      verifyResult = await _service.verifyAndRegister(
        sessionId: sessionId!,
        imageBytes: capturedImageBytes!,
        filename: 'registered_face.jpg',
      );

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

  /// One-Click Test Registration for Web (using embedded sample face)
  Future<bool> testSubmitWithSampleFace() async {
    if (sessionId == null) {
      try {
        await createSession();
      } catch (e) {
        errorMessage = 'Failed to create registration session: $e';
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

      verifyResult = await _service.verifyAndRegister(
        sessionId: sessionId!,
        imageBytes: sampleBytes,
        filename: 'sample_face.jpg',
      );

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
    if (sessionId != null && !isSuccess) {
      _service.cancelSession(sessionId!);
    }
    _stopStream();
    _faceDetector?.close();
    cameraController?.dispose();
    super.dispose();
  }
}
