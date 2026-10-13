import 'package:flutter/services.dart';

class TestFaceHelper {
  static Uint8List? _cachedBytes;

  /// Loads the embedded sample face image bytes (assets/sample_face.jpg)
  static Future<Uint8List> getSampleFaceBytes() async {
    if (_cachedBytes != null) return _cachedBytes!;
    final byteData = await rootBundle.load('assets/sample_face.jpg');
    _cachedBytes = byteData.buffer.asUint8List();
    return _cachedBytes!;
  }
}
