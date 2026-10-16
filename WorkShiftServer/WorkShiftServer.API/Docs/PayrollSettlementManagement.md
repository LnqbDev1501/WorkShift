# Quyết toán toàn kỳ và tra cứu Carry Forward

Tất cả API dưới đây yêu cầu Bearer token của **ACCOUNTANT đang ACTIVE**, có chi nhánh. Chỉ truy cập kỳ/Carry Forward thuộc chi nhánh nguồn; không thực hiện chuyển tiền.

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/payroll-periods/{id}/settlement-summary` | Tổng tiền version hiện hành, deadline và lý do chặn quyết toán |
| POST | `/api/payroll-periods/{id}/confirm-settlement` | Xác nhận đã quyết toán bên ngoài và tự đóng kỳ |
| GET | `/api/carry-forward-deductions` | Danh sách, search/filter, phân trang |
| GET | `/api/carry-forward-deductions/{id}` | Nguồn phát sinh, original/remaining/total applied |
| GET | `/api/carry-forward-deductions/{id}/applications?page=1&pageSize=20` | Lịch sử trừ: Staff, Payroll version, kỳ, số tiền, thời điểm |

## Xác nhận quyết toán

```http
POST /api/payroll-periods/12/confirm-settlement
Content-Type: application/json
```
```json
{
  "confirmedExternalSettlement": true,
  "paymentReference": "BANK-BATCH-20261016",
  "paymentNote": "Đã chi trả toàn bộ nhân viên bên ngoài hệ thống"
}
```

`confirmedExternalSettlement` bắt buộc true. Reference/note tùy chọn, tối đa 100/2000 ký tự. Không nhận tổng tiền/số dư từ frontend. Phản hồi có `alreadySettled` và `summary`; gọi lại cùng kỳ hợp lệ đã CLOSED trả thành công, không ghi đè metadata và không trừ/tạo Carry Forward lần nữa.

GET summary trả `periodStatus`, `feedbackDeadlineAt`, `isSettled`, `canConfirmSettlement`, danh sách `blockingReasons`, nhân viên/Payroll/Payslip hiện hành và tổng gross/deduction/net/Carry Forward áp dụng/khoản mới chuyển kỳ. GET không thay đổi dữ liệu.

Điều kiện POST được kiểm tra lại **trong transaction**, không dựa vào GET trước đó:

1. Kỳ APPROVED, đủ Payroll hiện hành cho các nhân viên cần tính.
2. Đã hết thứ Năm: **00:00 thứ Sáu, UTC+7**. Deadline cố định theo tuần, phiếu thay thế không kéo dài deadline. Không coi im lặng là Staff xác nhận.
3. Không còn feedback SUBMITTED hoặc APPROVED với correction chưa COMPLETED; không còn version sửa CALCULATED/SUBMITTED/REJECTED đang chờ xử lý.
4. Mỗi Payroll hiện hành APPROVED và có Payslip PUBLISHED đúng kỳ đã phát. Kiểm tra tổng tiền với các dòng tính và snapshot công; không đọc lại Attendance để đổi công.
5. Các kỳ trước của nhân viên đã quyết toán. Carry Forward đúng user, source Payroll/detail, lịch sử, số dư; kế hoạch trong Payroll đã phát khớp số dư hiện tại và FIFO. Nếu không khớp, trả 409 để correction; không tự đổi tiền trên phiếu đã phát.

Khi hợp lệ: Payroll hiện hành PAID và lưu người/thời điểm/reference/note → ghi application, giảm số dư Carry Forward cũ → tạo OPEN cho phần giảm trừ mới chưa trừ hết → kỳ PAID → tự CLOSED. Hai audit `CONFIRM_PAYROLL_PERIOD_SETTLEMENT` và `AUTO_CLOSE_PAYROLL_PERIOD` ghi nhận các bước. `closed_by` là Accountant kích hoạt quá trình, audit tự đóng có actor hệ thống. Tất cả thay đổi tài chính/đóng kỳ trong cùng transaction; lỗi thì rollback toàn bộ.

Không cập nhật version Payroll/Payslip cũ. Carry Forward cũ áp dụng FIFO (`created_at`, rồi ID), trước giảm trừ mới. Chỉ application **> 0** được ghi; phần còn dư của Carry Forward cũ ở chính bản ghi đó, không tạo nợ trùng. Phần chưa trừ của mỗi dòng PENALTY/ATTENDANCE_DEDUCTION mới tạo một khoản nguồn riêng. Không làm tròn; DB cũ cố định scale không chứa được kết quả sẽ bị từ chối thay vì tự làm tròn.

Ví dụ: gross 250.000; nợ cũ #1=200.000, #2=200.000; phạt mới=100.000. Khi PAID: ghi applications #1=200.000 và #2=50.000; #1 SETTLED, #2 còn 150.000/PARTIALLY_APPLIED; phạt mới sinh OPEN 100.000. Net=0. Nếu gross=0, không ghi application=0; nợ cũ giữ nguyên, chỉ phần giảm trừ mới chuyển kỳ.

## Tra cứu Carry Forward

```http
GET /api/carry-forward-deductions?keyword=Nguyễn&userId=...&status=OPEN&sourcePayrollId=100&sourcePayrollPeriodId=8&createdFrom=2026-10-01&createdTo=2026-10-31&page=1&pageSize=20
```

Keyword tìm tên nhân viên snapshot/lý do hoặc ID Carry Forward; không phân biệt hoa thường, bỏ khoảng trắng hai đầu. `%`/`_` được tìm như ký tự thông thường. Filter kết hợp AND. Status chỉ OPEN/PARTIALLY_APPLIED/SETTLED; bỏ status để xem cả ba. Ngày tạo là ngày UTC+7, bao gồm cả ngày bắt đầu/kết thúc. Page >=1, PageSize 1–100. Danh sách xếp FIFO; lịch sử applications mới nhất trước.

Chi tiết trả nguồn Payroll/version/kỳ/detail, Staff, số tiền khoản nguồn, phần đã trừ ở nguồn, `originalAmount`, `remainingAmount`, `totalAppliedAmount` và số lần áp dụng. Tra cứu cũng kiểm tra tính nhất quán nguồn/lịch sử, trả 409 nếu dữ liệu bị sai; không sửa số dư tự động.

## Thông báo đóng kỳ

Sau commit, Hangfire gửi sự kiện SignalR `PayrollPeriodClosed` trên hub **`/hubs/payroll-notifications`**, chỉ ADMIN được kết nối. Payload: `eventId` audit ổn định, `payrollPeriodId`, `periodCode`, `branchId`, `closedAt`, `status`. Hỗ trợ Bearer header hoặc `access_token` cho SignalR browser transports trên đúng hub này.

Không sử dụng bảng Notification của thông báo Admin tự tạo, không thêm Publication/Outbox/table mới. Audit tự đóng là dấu vết bền vững; job `payroll-closure-notification-recovery` mỗi phút tìm kỳ chưa có audit `PAYROLL_CLOSURE_NOTIFIED` và thử gửi lại. Lỗi enqueue không làm API báo thất bại khi tài chính đã commit. Lỗi gửi giữ sự kiện chờ retry.

Realtime có thể phát lặp nếu process dừng sau khi gửi nhưng trước khi ghi nhận, phía nhận cần deduplicate `eventId`. Đây là broadcast cho Admin đang kết nối, không phải bảo đảm Admin offline đã đọc; trạng thái CLOSED và audit vẫn lưu trong DB để tra cứu.

Sau PAID/CLOSED, Accountant/Admin xem lịch sử và xuất Excel bằng API sẵn có `/api/payroll-periods/{id}/report`. Không thêm API đóng thủ công, Cancel, tạo/sửa/hủy Carry Forward thủ công. Không thêm migration hoặc chạy SQL trên DB.

Mã lỗi: 400 request không hợp lệ; 401 chưa đăng nhập; 403 sai role/tài khoản không hoạt động; 404 không tìm thấy hoặc ngoài chi nhánh; 409 điều kiện nghiệp vụ chưa đáp ứng/dữ liệu không nhất quán.
