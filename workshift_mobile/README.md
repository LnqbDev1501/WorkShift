# workshift_mobile

Ứng dụng nhân viên WorkShift: lịch làm việc, chấm công, hợp đồng, yêu cầu và bảng lương.

## Getting Started

Xem [hướng dẫn cấu hình và kiểm tra của project](../README.md).

```powershell
flutter pub get
flutter run --dart-define=API_BASE_URL=https://your-api.example.com
```

Không truyền `API_BASE_URL` thì Android emulator dùng `http://10.0.2.2:5267`, các nền tảng khác dùng `http://localhost:5267`.

```powershell
flutter analyze --no-pub
flutter test --no-pub
dart format --output=none --set-exit-if-changed lib test
```
