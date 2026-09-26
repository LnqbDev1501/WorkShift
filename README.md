# WorkShift

Ứng dụng quản lý ca làm việc, nhân sự và bảng lương, sử dụng .NET 8.

## Cấu trúc

- `WorkShiftServer/WorkShiftServer.API`: API, xác thực, SignalR và tác vụ nền.
- `WorkShiftServer/WorkShiftManagement.BLL`: nghiệp vụ, DTO và interface service.
- `WorkShiftServer/WorkShiftManagement.DAL`: Entity Framework, repository và migration PostgreSQL.
- `UI_Admin/UI_Admin`: giao diện ASP.NET Core MVC.

`WorkShiftServer/WorkShiftServer.sln` chứa cả backend và UI; dùng solution này để kiểm tra toàn bộ project.

## Cấu hình và chạy

1. Cài .NET SDK hỗ trợ target `net8.0` và chuẩn bị PostgreSQL/Supabase.
2. Tạo `WorkShiftServer/WorkShiftServer.API/appsettings.json` từ `appsettings.example.json`, thay placeholder bằng cấu hình môi trường của bạn. Hangfire dùng connection string `Hangfire` nếu có, nếu không dùng `MyCnn`.
3. Cấu hình `WorkShiftApi:BaseUrl` cho UI bằng user secrets, biến môi trường `WorkShiftApi__BaseUrl` hoặc file `UI_Admin/UI_Admin/appsettings.json`. Các tính năng bản đồ cần cấu hình VietMap tương ứng.
4. Chuẩn bị schema database phù hợp trước khi khởi động API. Cấu hình Supabase, Resend và Cloudinary theo các tính năng sử dụng.

Không commit mật khẩu, API key hoặc cấu hình riêng. File `appsettings.json` và `appsettings.Development.json` đã được bỏ qua bởi Git.

```powershell
dotnet restore WorkShiftServer/WorkShiftServer.sln
dotnet build WorkShiftServer/WorkShiftServer.sln --no-restore
dotnet run --project WorkShiftServer/WorkShiftServer.API
# Chạy trong terminal thứ hai:
dotnet run --project UI_Admin/UI_Admin
```

## Kiểm tra code

```powershell
dotnet format WorkShiftServer/WorkShiftServer.sln whitespace --no-restore --verify-no-changes --exclude WorkShiftServer/WorkShiftManagement.DAL/Migrations
dotnet format WorkShiftServer/WorkShiftServer.sln analyzers --no-restore --verify-no-changes --severity warn
```

Build và kiểm tra tĩnh không thay thế kiểm thử các luồng nghiệp vụ với database và dịch vụ bên ngoài.
