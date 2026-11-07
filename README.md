# WorkShift

Hệ thống quản lý ca làm việc, chấm công và bảng lương.

## Cấu trúc

- `WorkShiftServer/WorkShiftServer.API`: API ASP.NET Core, SignalR và tác vụ Hangfire.
- `WorkShiftServer/WorkShiftManagement.BLL`: nghiệp vụ, DTO và service.
- `WorkShiftServer/WorkShiftManagement.DAL`: Entity Framework Core, entity, repository và migration.
- `UI_Web/UI_Admin`: web ASP.NET Core MVC cho các vai trò trong hệ thống.
- `workshift_mobile`: ứng dụng Flutter dành cho nhân viên.

`WorkShiftServer/WorkShiftServer.sln` bao gồm cả API, BLL, DAL và web MVC.

## Cấu hình và chạy

Cần .NET SDK hỗ trợ `net8.0`, Flutter có Dart `>=3.11.0 <4.0.0` và PostgreSQL/Supabase.

1. Sao chép `WorkShiftServer/WorkShiftServer.API/appsettings.example.json` thành `appsettings.json` trong cùng thư mục rồi điền cấu hình dịch vụ. DbContext nhận cấu hình kết nối từ API.
2. Sao chép `UI_Web/UI_Admin/appsettings.example.json` thành `appsettings.json` trong cùng thư mục. Đặt `WorkShiftApi:BaseUrl` trỏ tới API đang chạy. URL phải là HTTP/HTTPS, không có query hoặc fragment.
3. Chạy API và web ở hai terminal:

```powershell
dotnet run --project WorkShiftServer/WorkShiftServer.API --launch-profile http
dotnet run --project UI_Web/UI_Admin --launch-profile http
```

API mặc định: `http://localhost:5267`; web: `http://localhost:5079`.

Chạy mobile:

```powershell
cd workshift_mobile
flutter pub get
flutter run
```

Android emulator dùng `http://10.0.2.2:5267`; các nền tảng khác dùng `http://localhost:5267`. Với điện thoại thật hoặc tunnel, truyền địa chỉ API truy cập được từ thiết bị:

```powershell
flutter run --dart-define=API_BASE_URL=https://your-api.example.com
```

HTTP và SignalR dùng chung URL đang hoạt động. Sau khi đổi URL bằng `ApiClient.setBaseUrl`, ngắt và kết nối lại SignalR để áp dụng cho kết nối đang mở.

Không commit `appsettings.json`, token hoặc khóa dịch vụ. Có thể dùng biến môi trường .NET với dấu `__`, ví dụ `ConnectionStrings__MyCnn` và `WorkShiftApi__BaseUrl`.

## Kiểm tra

Từ thư mục gốc:

```powershell
dotnet restore WorkShiftServer/WorkShiftServer.sln
dotnet build WorkShiftServer/WorkShiftServer.sln --no-restore
```

Từ `workshift_mobile`:

```powershell
flutter pub get
flutter analyze --no-pub
flutter test --no-pub
dart format --output=none --set-exit-if-changed lib test
```

Các thư mục `bin`, `obj`, `build`, `.dart_tool`, `.vs` và Flutter `ephemeral` là sản phẩm sinh tự động. Sau khi dọn, chạy restore/pub get để tạo lại. Giữ các file lock, migration, mã nguồn nền tảng và tài nguyên đang được tham chiếu.
