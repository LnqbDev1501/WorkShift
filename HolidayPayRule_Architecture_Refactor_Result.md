# BÁO CÁO KẾT QUẢ REFACTOR KIẾN TRÚC 3-LAYER CHO HOLIDAY PAY RULE — WORKSHIFT

> **Ngày thực hiện:** 03/11/2026  
> **Dự án:** WorkShift ASP.NET Core Backend (`WorkShiftServer`)  
> **Mục tiêu:** Chuẩn hóa kiến trúc 4 chức năng Holiday Pay Rule theo chuẩn 3-Layer:  
> `AdminHolidayPayRulesController` → `IHolidayPayRuleService` → `HolidayPayRuleService` → `IHolidayPayRuleRepo` → `HolidayPayRuleRepo` → `AppDbContext`.

---

## 1. DANH SÁCH FILE TẠO MỚI

1. `WorkShiftServer/WorkShiftManagement.BLL/DTOs/Admin/HolidayPayRule/Request/HolidayPayRuleRequest.cs`:
   - DTO nhận dữ liệu tạo và cập nhật quy tắc tính lương ngày lễ.
   - Chứa validation DataAnnotations (`[Required]`, `[StringLength(150)]`, `[Range]`) và `[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]`.
2. `WorkShiftServer/WorkShiftManagement.BLL/DTOs/Admin/HolidayPayRule/Response/HolidayPayRuleResponse.cs`:
   - Record response trả về thông tin chi tiết của một quy tắc lương ngày lễ, bao gồm các cờ hành động UI (`CanEdit`, `CanPublish`, `CanCancel`).
3. `WorkShiftServer/WorkShiftManagement.BLL/Interfaces/Admin/IHolidayPayRuleService.cs`:
   - Interface hợp đồng nghiệp vụ tầng BLL cho các chức năng quản trị quy tắc tính lương ngày lễ.
4. `WorkShiftServer/WorkShiftManagement.DAL/Interfaces/Admin/IHolidayPayRuleRepo.cs`:
   - Interface truy cập dữ liệu tầng DAL cho Holiday Pay Rule, bao gồm các truy vấn chuyên biệt, kiểm tra tham chiếu lương, và quản lý giao dịch có lock bảng.
5. `WorkShiftServer/WorkShiftManagement.DAL/Repositories/Admin/HolidayPayRuleRepo.cs`:
   - Repository triển khai `IHolidayPayRuleRepo`, đóng gói toàn bộ câu truy vấn LINQ/EF Core, quản lý `AppDbContext` và ghi AuditLog.

---

## 2. DANH SÁCH FILE CHỈNH SỬA

1. `WorkShiftServer/WorkShiftManagement.BLL/Services/Admin/HolidayPayRuleService.cs`:
   - Bỏ các DTO viết inline; import DTO từ namespace mới.
   - Kế thừa và triển khai `IHolidayPayRuleService`.
   - Đổi dependency từ concrete class `HolidayConfigurationRepo` sang interface `IHolidayPayRuleRepo`.
   - Tách phương thức Create và Update: Cung cấp `CreateAsync(...)` và `UpdateAsync(...)` độc lập bên cạnh `SaveAsync(...)`.
   - Loại bỏ hoàn toàn việc truy cập trực tiếp `IQueryable` hay các hàm EF Core (`AsNoTracking`, `ToListAsync`, `SingleOrDefaultAsync`, `AnyAsync`). Mọi thao tác đọc/ghi đều thông qua `repo`.
2. `WorkShiftServer/WorkShiftServer.API/Controllers/Admin/AdminHolidayPayRulesController.cs`:
   - Đổi Dependency Injection từ concrete `HolidayPayRuleService` sang `IHolidayPayRuleService`.
   - Gọi riêng biệt `service.CreateAsync(request, Actor, ct)` ở endpoint `POST` và `service.UpdateAsync(id, request, Actor, ct)` ở endpoint `PUT`.
   - Bổ sung endpoint `[HttpGet("{id:int}")]` để lấy chi tiết một rule theo ID (`GetByIdAsync`).
   - Bổ sung route alias `api/admin/holiday-pay-rules` song song với `api/holiday-pay-rules` để tương thích 100% với frontend hiện tại và chuẩn convention phân quyền admin.
3. `WorkShiftServer/WorkShiftServer.API/Services/BackgroundJobs/Rules/HolidayRuleActivationWorker.cs`:
   - Đổi resolve dependency trong scoped service provider từ concrete `HolidayPayRuleService` sang interface `IHolidayPayRuleService`.
4. `WorkShiftServer/WorkShiftServer.API/Program.cs`:
   - Đăng ký DI theo cặp Interface - Implementation chuẩn:
     - `builder.Services.AddScoped<IHolidayPayRuleRepo, HolidayPayRuleRepo>();`
     - `builder.Services.AddScoped<IHolidayPayRuleService, HolidayPayRuleService>();`
     - Vẫn duy trì đăng ký `HolidayConfigurationRepo` để phục vụ `PublicHolidayService`.
     - Vẫn duy trì đăng ký alias `HolidayPayRuleService` để bảo đảm an toàn cho các thành phần legacy.

---

## 3. CẤU TRÚC KIẾN TRÚC TRƯỚC VÀ SAU REFACTOR

### Trước Refactor (As-Is):
```text
[AdminHolidayPayRulesController]
       │ (inject concrete class)
       ▼
[HolidayPayRuleService] (tự chứa DTO nội tuyến)
       │ (inject concrete repo, gọi trực tiếp repo.Rules.ToListAsync()...)
       ▼
[HolidayConfigurationRepo] (phơi bày IQueryable<T> ra ngoài)
       │
       ▼
  [AppDbContext]
```

### Sau Refactor (To-Be chuẩn 3-Layer):
```text
[AdminHolidayPayRulesController]
       │
       ▼ (injects)
[<<interface>> IHolidayPayRuleService]
       ▲
       │ (implements)
[HolidayPayRuleService] ──> [DTOs: HolidayPayRuleRequest, HolidayPayRuleResponse]
       │
       ▼ (injects)
[<<interface>> IHolidayPayRuleRepo]
       ▲
       │ (implements)
[HolidayPayRuleRepo] (đóng gói 100% EF Core, Lock bảng, AuditLog)
       │
       ▼
  [AppDbContext]
```

---

## 4. CÁC METHOD ĐƯỢC DI CHUYỂN VÀ TÁCH BIỆT

### A. Tách Create và Update:
- **Trước đây:** Controller gọi chung `SaveAsync(null, ...)` cho Create và `SaveAsync(id, ...)` cho Update.
- **Hiện tại:**
  - Interface `IHolidayPayRuleService` khai báo tường minh:
    ```csharp
    Task<HolidayPayRuleResponse> CreateAsync(HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default);
    Task<HolidayPayRuleResponse> UpdateAsync(int id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default);
    ```
  - Trong Service, `CreateAsync` và `UpdateAsync` ủy thác xử lý qua phương thức dùng chung an toàn `SaveInternalAsync`, đảm bảo không trùng lặp code kiểm tra validation, locking hay snapshot audit.
  - Controller gọi đúng method ngữ nghĩa: `CreateAsync` cho HTTP POST và `UpdateAsync` cho HTTP PUT.

### B. Chuyển toàn bộ EF Core Queries xuống DAL:
- Query kiểm tra rule draft chưa dùng:
  - Trước đây (ở Service): `await repo.Details.AnyAsync(...)` và `await repo.Rules.AnyAsync(...)`.
  - Hiện tại (ở Repository): `repo.IsReferencedInPayrollAsync(...)` và `repo.IsReferencedAsReplacedRuleAsync(...)`.
- Query lấy lịch sử danh sách:
  - Trước đây (ở Service): `repo.Rules.AsNoTracking().OrderByDescending(...).ToListAsync(...)`.
  - Hiện tại (ở Repository): `repo.GetAllOrderedAsync(ct)`.
- Query kiểm tra có rule đang chờ lịch Publish:
  - Trước đây (ở Service): `await repo.Rules.AnyAsync(r => r.Status == "PUBLISH", ct)`.
  - Hiện tại (ở Repository): `repo.HasScheduledPublishRuleAsync(ct)`.
- Query lấy phiên bản trước đó:
  - Trước đây (ở Service): `repo.Rules.Where(r => r.Status == "ACTIVE" || r.Status == "INACTIVE").OrderByDescending(...).FirstOrDefaultAsync(...)`.
  - Hiện tại (ở Repository): `repo.GetLatestActiveOrInactiveRuleAsync(ct)`.
- Query lấy rule Publish đến ngày kích hoạt:
  - Trước đây (ở Service): `repo.Rules.Where(r => r.Status == "PUBLISH" && r.EffectiveFrom <= Today)...`.
  - Hiện tại (ở Repository): `repo.GetDuePublishedRulesAsync(today, ct)`.
- Query lấy các rule đang Active để chuyển Inactive:
  - Trước đây (ở Service): `repo.Rules.Where(r => r.Status == "ACTIVE").ToListAsync(...)`.
  - Hiện tại (ở Repository): `repo.GetActiveRulesAsync(ct)`.

---

## 5. THÔNG TIN CHÍNH XÁC ĐỂ CẬP NHẬT CLASS DIAGRAM

Dưới đây là danh sách chi tiết các class, interface, method signature để cập nhật cho 4 Use Cases:

### 1. Presentation Layer (API)
- **Class:** `WorkShiftServer.API.Controllers.Admin.AdminHolidayPayRulesController`
  - **Constructor:** `AdminHolidayPayRulesController(IHolidayPayRuleService service)`
  - **Methods:**
    - `List(CancellationToken ct)` → `Task<IActionResult>` *(UC1 - View History)*
    - `GetById(int id, CancellationToken ct)` → `Task<IActionResult>`
    - `Create(HolidayPayRuleRequest request, CancellationToken ct)` → `Task<IActionResult>` *(UC2 - Create)*
    - `Update(int id, HolidayPayRuleRequest request, CancellationToken ct)` → `Task<IActionResult>` *(UC3 - Update)*
    - `Delete(int id, CancellationToken ct)` → `Task<IActionResult>` *(UC4 - Delete)*
    - `Publish(int id, CancellationToken ct)` → `Task<IActionResult>`
    - `Cancel(int id, CancellationToken ct)` → `Task<IActionResult>`

### 2. Business Logic Layer (BLL)
- **Interface:** `WorkShiftManagement.BLL.Interfaces.Admin.IHolidayPayRuleService`
  - **Property:** `DateOnly Today { get; }`
  - **Methods:**
    - `ListAsync(CancellationToken ct = default)` → `Task<List<HolidayPayRuleResponse>>` *(UC1)*
    - `GetByIdAsync(int id, CancellationToken ct = default)` → `Task<HolidayPayRuleResponse>`
    - `CreateAsync(HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default)` → `Task<HolidayPayRuleResponse>` *(UC2)*
    - `UpdateAsync(int id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default)` → `Task<HolidayPayRuleResponse>` *(UC3)*
    - `DeleteAsync(int id, Guid actor, CancellationToken ct = default)` → `Task<bool>` *(UC4)*
    - `PublishAsync(int id, Guid actor, CancellationToken ct = default)` → `Task<HolidayPayRuleResponse>`
    - `CancelAsync(int id, Guid actor, CancellationToken ct = default)` → `Task<HolidayPayRuleResponse>`
    - `SynchronizeAsync(CancellationToken ct = default)` → `Task<bool>`
- **Implementation:** `WorkShiftManagement.BLL.Services.Admin.HolidayPayRuleService`
  - **Constructor:** `HolidayPayRuleService(IHolidayPayRuleRepo repo, IAdminRealtimePublisher? realtimePublisher = null) : IHolidayPayRuleService`
  - Triển khai đầy đủ tất cả các method của `IHolidayPayRuleService`.
- **DTOs:**
  - `WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Request.HolidayPayRuleRequest`:
    - `string RuleName { get; set; }`
    - `decimal? PayMultiplier { get; set; }`
    - `DateOnly? EffectiveFrom { get; set; }`
  - `WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Response.HolidayPayRuleResponse`:
    - `int HolidayPayRuleId`
    - `string RuleName`
    - `decimal PayMultiplier`
    - `DateOnly? EffectiveFrom`
    - `DateOnly? EffectiveTo`
    - `int? ReplacedRuleId`
    - `string Status`
    - `bool CanEdit`
    - `bool CanPublish`
    - `bool CanCancel`
    - `Guid CreatedBy`
    - `DateTime CreatedAt`

### 3. Data Access Layer (DAL)
- **Interface:** `WorkShiftManagement.DAL.Interfaces.Admin.IHolidayPayRuleRepo`
  - **Methods:**
    - `GetByIdAsync(int id, CancellationToken ct = default)` → `Task<HolidayPayRule?>`
    - `GetAllOrderedAsync(CancellationToken ct = default)` → `Task<List<HolidayPayRule>>` *(UC1)*
    - `IsReferencedInPayrollAsync(int holidayPayRuleId, CancellationToken ct = default)` → `Task<bool>` *(UC3, UC4)*
    - `IsReferencedAsReplacedRuleAsync(int holidayPayRuleId, CancellationToken ct = default)` → `Task<bool>` *(UC3, UC4)*
    - `HasScheduledPublishRuleAsync(CancellationToken ct = default)` → `Task<bool>`
    - `GetLatestActiveOrInactiveRuleAsync(CancellationToken ct = default)` → `Task<HolidayPayRule?>`
    - `GetDuePublishedRulesAsync(DateOnly today, CancellationToken ct = default)` → `Task<List<HolidayPayRule>>`
    - `GetActiveRulesAsync(CancellationToken ct = default)` → `Task<List<HolidayPayRule>>`
    - `GetActiveHolidayDatesFromAsync(DateOnly fromDate, CancellationToken ct = default)` → `Task<List<DateOnly>>`
    - `Add(HolidayPayRule rule)` → `void` *(UC2)*
    - `Remove(HolidayPayRule rule)` → `void` *(UC4)*
    - `SaveChangesAsync(CancellationToken ct = default)` → `Task<int>`
    - `ExecuteTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)` → `Task<T>`
    - `WriteAudit(Guid? actor, string action, int id, object? before, object? after)` → `void`
- **Implementation:** `WorkShiftManagement.DAL.Repositories.Admin.HolidayPayRuleRepo`
  - **Constructor:** `HolidayPayRuleRepo(AppDbContext db) : IHolidayPayRuleRepo`
  - Triển khai toàn bộ phương thức, trực tiếp truy vấn qua `db.HolidayPayRules`, `db.PayrollDetails`, `db.AuditLogs`.

---

## 6. ĐÁNH GIÁ TÁC ĐỘNG VÀ TƯƠNG THÍCH

1. **HolidayRuleActivationWorker:**
   - Worker chạy ngầm định kỳ 1 phút/lần, đã chuyển sang inject `IHolidayPayRuleService` và gọi `SynchronizeAsync(stoppingToken)`.
   - Cơ chế tự động kích hoạt rule khi đến ngày hiệu lực tiếp tục hoạt động 100% không đổi.
2. **Payroll Calculation:**
   - Hoàn toàn KHÔNG bị ảnh hưởng: `PayrollCalculationRepo.cs` truy vấn độc lập từ `db.HolidayPayRules.AsNoTracking()`; `PayrollCalculationPolicy.cs` tính toán dựa trên Entity `HolidayPayRule`.
   - Các Guard bảo vệ tính bất biến của bảng lương trong `AppDbContext.cs` giữ nguyên.
3. **PublicHolidayService:**
   - `HolidayConfigurationRepo` vẫn tồn tại và được giữ nguyên để phục vụ `PublicHolidayService`.
   - Thuộc tính `HolidayPayRuleService.Today` được duy trì dưới dạng `public static`, đảm bảo `PublicHolidayService` biên dịch và hoạt động bình thường.
4. **Giao diện người dùng (UI_Admin):**
   - Các API endpoints (`GET /api/holiday-pay-rules`, `POST`, `PUT`, `DELETE`, `/publish`, `/cancel`) giữ nguyên.
   - Định dạng payload JSON gửi lên và kết quả trả về của `HolidayPayRuleRequest` và `HolidayPayRuleResponse` giống 100% trước đây.
   - Các HTTP Status codes (`200 OK`, `201 Created`, `204 NoContent`, `400 BadRequest`, `404 NotFound`, `409 Conflict` với mã lỗi `RULE_READ_ONLY`, `RULE_DATE_ORDER`,...) được bảo toàn nguyên vẹn.

---

## 7. KẾT QUẢ BIÊN DỊCH VÀ KIỂM THỬ

- **Lệnh thực thi:** `dotnet build "D:\DOAN\WorkShift\WorkShiftServer\WorkShiftServer.sln"`
- **Kết quả:** **Build succeeded**
  - `WorkShiftManagement.DAL` → Succeeded (0 Errors)
  - `WorkShiftManagement.BLL` → Succeeded (0 Errors)
  - `WorkShiftServer.API` → Succeeded (0 Errors)
  - `UI_Admin` → Succeeded (0 Errors)
- **Tổng số lỗi:** **0 Error(s)**.
- **Git State:** Không tự động commit hay push Git theo đúng yêu cầu giới hạn của task.

