# Payroll workflow APIs

Luồng backend hiện hành: tạo kỳ DRAFT → Publish OPEN → Manager liên kết và review công → Timesheet REVIEWED → tính toàn kỳ → Accountant/Staff review Payroll Preview → sửa nguồn hoặc giải trình → Submit toàn kỳ → Admin duyệt, khóa công và phát Official Payslip → quyết toán → CLOSED.

Các đường dẫn dưới đây có tiền tố `/api`. Accountant và Manager chỉ thao tác chi nhánh được phân công; Staff chỉ được đọc/trả lời lượt gửi cho chính mình. Backend kiểm tra lại tài khoản/role đang hoạt động.

## 1. Kỳ lương và tính lương

| Method | Endpoint | Actor | Ý nghĩa |
|---|---|---|---|
| GET/POST | `/accountant/payroll-periods` | Accountant | Tra cứu/tạo kỳ DRAFT |
| GET/PATCH/DELETE | `/accountant/payroll-periods/{id}` | Accountant | Xem; chỉ sửa/xóa nháp nếu không có dữ liệu phụ thuộc |
| POST | `/accountant/payroll-periods/{id}/publish` | Accountant | DRAFT → OPEN; `/open` giữ làm alias tương thích |
| GET | `/payroll-periods/{id}/calculation-readiness` | Accountant | Điều kiện tính của từng Staff |
| GET | `/payroll-periods/{id}/employees/{userId}/calculation-inputs` | Accountant | Xem dữ liệu đã có, cả khi chưa đủ điều kiện tính |
| GET | `/payroll-periods/{id}/employees/{userId}/calculation-preview` | Accountant | Xem phép tính dự kiến, không lưu |
| GET | `/payroll-periods/{id}/calculation-preview` | Accountant | Xem trước toàn kỳ, không lưu |
| POST | `/payroll-periods/{id}/calculate` | Accountant | Lần đầu tính toàn kỳ trong một transaction; thiếu bất kỳ Staff nào thì không lưu một phần |
| GET | `/payroll-periods/{id}/payrolls` | Admin/Accountant | Danh sách/Search/Filter Payroll hiện hành |
| GET | `/payrolls/{id}` | Admin/Accountant | Snapshot và kết quả đã lưu |
| GET | `/payrolls/{id}/details` | Admin/Accountant | Các dòng tính lương |
| GET | `/payrolls/{id}/versions` | Admin/Accountant | Lịch sử version của cùng Staff/kỳ |

Tính lương dùng Timesheet REVIEWED có ReviewedAt và tổng công khớp dữ liệu ca/chấm công; không có yêu cầu chỉnh công PENDING. Chọn lịch sử lương theo ngày làm, bao gồm bản INACTIVE nếu ngày làm nằm trong khoảng hiệu lực. Chỉ tính thưởng/phạt APPROVED. Carry Forward cũ áp dụng theo FIFO trước giảm trừ mới. Không ghi số dư Carry Forward ở bước Calculate.

Thưởng/phạt PENDING không được tính vào tiền; vẫn được xem Preview và thấy riêng trong `pendingSourceAdjustments`, nhưng chặn Submit/Approve.

Điểm kết thúc review công của Manager chuyển sang REVIEWED, không khóa sớm. Endpoint `/manager/timesheets/{id}/review` và alias `/lock` dùng cơ chế review hiện có. Không sửa schema Timesheet. Chỉ bước Admin duyệt Payroll mới đặt Timesheet LOCKED.

## 2. Gửi và kiểm tra Payroll Preview

| Method | Endpoint | Actor | Ý nghĩa |
|---|---|---|---|
| POST | `/payroll-periods/{id}/send-previews` | Accountant | Gửi toàn bộ Payroll hiện hành cho Staff; retry không tạo lượt trùng |
| GET | `/payroll-previews` | Accountant/Staff | Danh sách/Search/Filter các lượt review |
| GET | `/payroll-previews/{id}` | Accountant/Staff | Snapshot Payroll của lượt đó, kết quả xử lý và correction liên quan |
| POST | `/payroll-previews/{id}/confirm` | Staff | Đồng ý lượt review hiện hành |
| POST | `/payroll-previews/{id}/feedback` | Staff | Gửi `{ "description": "Nội dung sai sót" }`; không có category |
| POST | `/payroll-feedback-requests/{id}/approve` | Accountant | Chấp nhận feedback, tự tạo Correction Request OPEN gửi Manager |
| POST | `/payroll-feedback-requests/{id}/reject` | Accountant | Từ chối với lý do và gửi một lượt review mới cho Staff |

Hai GET `/payroll-feedback-requests` và `/{id}` giữ làm alias đọc. ID ở đây là `feedback_id` của **lượt review**, không phải Payroll ID/Payslip ID.

Bộ lọc: `keyword`, `userId`, `payrollPeriodId`, `status`, `reviewStatus`, `page`, `pageSize`. Response danh sách dùng `items`, `pageNumber`, `pageSize`, `totalItems`.

Approve/Reject body: `{ "resolutionNote": "Kết quả kiểm tra / lý do" }`. Cả hai cần nội dung xử lý. Approve không đóng case: `review_status=FEEDBACK_PENDING`, `status=APPROVED` cho đến khi xử lý nguồn và gửi lượt tiếp theo.

### Deadline và trạng thái

- Khi gửi Preview lần đầu, kỳ có `initial_feedback_deadline_at` mặc định là **24 giờ kể từ thời điểm gửi thực tế** (Now + 24h, lưu UTC, hiển thị UTC+7), hoặc theo deadline hợp lệ do Accountant chỉ định (> Now). Không dùng approval_deadline_at cho việc tự kết thúc case.
- Sau hạn này không cho Accountant mở vấn đề mới, không cho Staff trả lời lượt đầu quá hạn.
- Feedback đã gửi hợp lệ vẫn được xử lý sau hạn. Khi gửi kết quả/bản sửa qua Resend Preview: nếu gửi trong hạn thì giữ deadline cũ, nếu gửi sau hạn thì cấp chu kỳ 24 giờ mới từ lúc gửi lại. Staff có thể tiếp tục phản hồi trong lượt mới cho đến khi case hoàn tất.
- Một lượt chỉ được Confirm hoặc Feedback một lần; không sửa/rút feedback.
- Hết hạn không phản hồi: EXPIRED; không ghi CONFIRMED thay Staff. Job mỗi phút cập nhật; API vẫn chặn theo timestamp ngay cả trước khi job chạy.
- `round_no` tăng theo lượt review trong một case. Nó khác `payroll.version_no`: giải trình/từ chối có thể tạo lượt review mới nhưng giữ Payroll V1; sửa nguồn tạo Payroll V2.
- Trạng thái review: PENDING, CONFIRMED, FEEDBACK_PENDING, SUPERSEDED, EXPIRED. Quyết định feedback riêng: NULL, SUBMITTED, APPROVED, REJECTED.
- Metadata đóng case đặt trên lượt gốc (previous_feedback_id NULL), với STAFF_CONFIRMED hoặc REVIEW_EXPIRED.

## 3. Accountant yêu cầu Manager kiểm tra nguồn

| Method | Endpoint | Actor | Ý nghĩa |
|---|---|---|---|
| POST | `/payrolls/{id}/correction-requests` | Accountant | Mở vấn đề trong hạn: `{ "reason": "Lý do" }` |
| GET | `/payroll-periods/{id}/correction-requests` | Accountant/Manager | Lịch sử yêu cầu và phản hồi trong kỳ |
| POST | `/payroll-correction-requests/{id}/respond` | Manager | `{ "action": "CORRECT hoặc EXPLAIN", "note": "Kết quả" }` |
| POST | `/payroll-correction-requests/{id}/review` | Accountant | `{ "decision": "RETURN hoặc ACCEPT_EXPLANATION", "note": "Nội dung" }` |
| POST | `/payrolls/{id}/recalculate` | Accountant | `{ "correctionRequestId": 123 }`; chỉ tính lại Payroll bị ảnh hưởng |

Mở Correction Request: Payroll → REJECTED, Period → REVISION_REQUIRED. Manager không có action REJECT.

CORRECT không tự cấp quyền sửa nguồn. Manager phải xử lý qua nghiệp vụ nguồn hợp lệ; backend kiểm tra dữ liệu thực sự thay đổi và đủ điều kiện tính trước khi nhận CORRECT. Accountant không được gửi đơn giá/tiền giảm trừ nhập đè qua API Recalculate.

- OPEN → RESPONDED khi Manager trả lời.
- RESPONDED + EXPLAIN → Accountant chấp nhận: giữ Payroll, request COMPLETED; gửi Staff lượt tiếp theo nếu liên quan review Staff.
- RESPONDED → RETURNED: giữ lịch sử và tạo request OPEN kế tiếp qua previous_request_id.
- RESPONDED + CORRECT → Accountant Recalculate: lưu snapshot nguồn mới, Payroll cũ REPLACED, bản mới CALCULATED; request vẫn RESPONDED. Accountant kiểm tra rồi gọi complete để hoàn tất và gửi Staff lượt tiếp theo nếu có case Staff.
- Chỉ COMPLETED có resolved_payroll_id/completed_at. Không tạo version nếu dữ liệu/kết quả không đổi.

Nếu Staff đã xác nhận một case nhưng Accountant phát hiện vấn đề **mới trước hạn chung**, có thể mở case riêng; không sửa lại case đã đóng. Sau hạn không được mở case mới.

## 4. Submit, Admin duyệt và phát phiếu

| Method | Endpoint | Actor | Ý nghĩa |
|---|---|---|---|
| POST | `/accountant/payroll-periods/{id}/submit` | Accountant | Submit toàn kỳ sau khi tất cả review/correction hoàn tất |
| GET | `/admin/payroll-periods` | Admin | Tra cứu/Search/Filter kỳ |
| GET | `/admin/payroll-periods/{id}` | Admin | Chi tiết kỳ và Payroll |
| POST | `/admin/payroll-periods/{id}/approve` | Admin | Duyệt toàn kỳ, khóa công và phát Official Payslip trong cùng transaction |
| GET | `/payslips` | Admin/Accountant/Staff | Tra cứu phiếu chính thức theo phạm vi |
| GET | `/payslips/{id}` | Admin/Accountant/Staff | Chi tiết phiếu từ dữ liệu đã đóng băng |
| GET | `/payslips/{id}/versions` | Admin/Accountant/Staff | Lịch sử phiếu chính thức trong phạm vi |

Điều kiện Submit: đủ Payroll hiện hành cho mọi Staff, tất cả CALCULATED; không có feedback/correction mở; lượt cuối của bản hiện hành CONFIRMED hoặc EXPIRED; không còn thưởng/phạt PENDING; dữ liệu nguồn không lệch kết quả đã lưu.

Submit: Period/current Payrolls → SUBMITTED. Nguồn đóng băng trong thời gian chờ duyệt. Admin approve: Timesheets REVIEWED → LOCKED, Payrolls/Period → APPROVED; Official Payslips PUBLISHED ngay, không đợi thứ Năm.

Không có Admin reject riêng Payroll, submit riêng Payroll, correction sau phát hành hoặc feedback trên Official Payslip. Các route của luồng cũ đã gỡ khỏi routing/Swagger.

`payslip_work_details` được giữ nguyên, tiếp tục ghi từ payroll.work_input_snapshot. Không đọc lại Attendance để thay thế snapshot khi Staff xem phiếu chính thức.

## 5. Quyết toán và Carry Forward

| Method | Endpoint | Actor |
|---|---|---|
| GET | `/payroll-periods/{id}/settlement-summary` | Accountant |
| POST | `/payroll-periods/{id}/confirm-settlement` | Accountant |
| GET | `/carry-forward-deductions` | Accountant |
| GET | `/carry-forward-deductions/{id}` | Accountant |
| GET | `/carry-forward-deductions/{id}/applications` | Accountant |
| GET | `/payroll-periods/{id}/report` | Admin/Accountant |

Confirm body: `{ "confirmedExternalSettlement": true, "paymentReference": "BANK-001", "paymentNote": "Đã chi trả" }`.

Không còn chờ cửa sổ feedback của Official Payslip. Khi kỳ APPROVED, review/correction đã xong và phiếu chính thức đã phát, Accountant xác nhận chi trả bên ngoài: current Payrolls PAID → ghi lịch sử trừ Carry Forward, cập nhật số dư và tạo khoản còn thiếu → Period PAID → tự CLOSED. Retry không trừ/tạo Carry Forward lần hai. Không có API Cancel hoặc đóng kỳ thủ công.

## 6. Thông báo hệ thống và nhắc việc

- GET `/payroll-system-notifications?page=1&pageSize=20`: feed sự kiện theo tài khoản/role/chi nhánh. Dữ liệu gồm EventId, CreatedAt và Payload JSON.
- SignalR `/hubs/payroll-notifications`: nhận `PayrollWorkflowChanged`; deduplicate bằng EventId. Sự kiện đóng kỳ `PayrollPeriodClosed` chỉ gửi nhóm Admin.
- Sự kiện lưu trong audit_logs trong cùng transaction nghiệp vụ; job Hangfire giao thông báo và lưu dấu đã giao. Không ghi vào bảng Notification dành cho Admin tạo nội dung.
- Nhắc theo trạng thái/người đang phải xử lý, các slot 09:00, 14:00, 20:00 UTC+7; không lặp cùng object/status/slot.
- Scanner thông báo Accountant khi kỳ OPEN đã có toàn bộ Timesheet REVIEWED. Job hết hạn review, khôi phục phát phiếu đã duyệt và gửi thông báo chạy mỗi phút.

## Kiểm tra

`dotnet build WorkShiftServer/WorkShiftServer.API/WorkShiftServer.API.csproj`

`dotnet run --project WorkShiftServer/WorkShiftManagement.PayrollChecks/WorkShiftManagement.PayrollChecks.csproj`

Checks dùng repository bộ nhớ có rollback và HTTP localhost với authentication giả; không kết nối Supabase. Các kiểm tra không thay thế chạy tích hợp PostgreSQL trên schema thật. Không chạy migration hoặc sửa dữ liệu production trong thay đổi này. UI cũ cần chuyển sang các route/response mới ở trên.

## Bổ sung: kiểm tra và chủ động gửi bản sửa

| Method | Endpoint | Actor | Ý nghĩa |
|---|---|---|---|
| GET | `/payrolls/{id}/correction-preview?correctionRequestId=123` | Accountant | Xem dự kiến từ nguồn mới, không lưu version |
| POST | `/payroll-correction-requests/{id}/complete` | Accountant | Body `{ "payrollId": 123, "note": "Đã kiểm tra bản sửa" }`; hoàn tất và gửi lượt Staff tiếp theo |
| GET | `/payroll-periods/{id}/source-impact` | Accountant | Toàn bộ Staff có nguồn lệch hoặc điều kiện chặn |
| POST | `/payroll-correction-requests/{id}/register-related-impact` | Accountant | Đăng ký những Staff bị ảnh hưởng bởi cùng thay đổi nguồn đã được Manager xác nhận |

Recalculate chỉ lưu version; không gửi Staff ngay. Response correction có `candidatePayrollId` và `awaitingAccountantCompletion`. Complete kiểm tra lại nguồn; nguồn đã đổi tiếp thì yêu cầu tính lại. Retry hoàn tất cùng Payroll không gửi trùng.

Nguồn chung thay đổi được đối chiếu theo giá trị cũ/mới đã ghi nhận tại Manager CORRECT. Những Staff khác cùng kỳ bị đúng thay đổi đó có correction riêng, từng người tính lại và hoàn tất riêng. Sau hạn không mở rộng sang thay đổi không liên quan; feedback đang chờ Accountant không bị tự duyệt. Dữ liệu chứng minh và liên kết được lưu trong audit_logs, không cần thêm schema. Submit/Approve tiếp tục chặn mọi Payroll có nguồn lệch, kể cả Staff khác; response trả `sourceImpact` đầy đủ.
