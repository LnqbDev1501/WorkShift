# Payroll Feedback and Correction

No new table/field/migration is required. Uses `payroll_feedback_requests`, `payslips`, `payrolls`, `payroll_details` and `audit_logs`. Work input remains frozen; no Timesheet/Attendance update is performed. The Admin-created `notifications` table is not used to store feedback/system notices.

## Routes

| Method | Route | Actor |
| --- | --- | --- |
| POST | `/api/payslips/{id}/feedback-requests` | Staff |
| GET | `/api/payroll-feedback-requests` | Accountant / Staff |
| GET | `/api/payroll-feedback-requests/{id}` | Accountant / Staff |
| POST | `/api/payroll-feedback-requests/{id}/approve` | Accountant |
| POST | `/api/payroll-feedback-requests/{id}/reject` | Accountant |
| POST | `/api/payroll-feedback-requests/{id}/correction` | Accountant |

Active database roles are checked in addition to JWT authorization. Staff only sees their own feedback; Accountant only sees feedback for their assigned branch. Admin/Manager do not have access to these six routes. Cross-scope IDs return 404. Closed/paid period feedback remains readable, but cannot be submitted/reviewed/corrected.

## Submit

```http
POST /api/payslips/100/feedback-requests
```
```json
{
  "reasonCode": "DEDUCTION",
  "description": "Khoản giảm trừ đi trễ đang được tính 40.000, trong khi mức áp dụng là 20.000."
}
```

Reason codes: `CALCULATION`, `RATE`, `REWARD`, `DEDUCTION`, `OTHER`. There is no Timesheet correction category. Description is required (not whitespace), maximum 2000 characters. IDs, employee, dates, status and processing metadata come from the server, not the request body.

Only the Staff owner's current `PUBLISHED` payslip linked to an `APPROVED` Payroll/Period is eligible. Feedback is admitted after actual publication during Thursday, strictly before Friday 00:00 UTC+7. Replacement does not reset the deadline. There may be only one unresolved feedback per employee/period: `SUBMITTED`, or `APPROVED` with correction not yet completed. The creation/check runs under the same transaction locks as payroll revision/publication. Concurrent duplicates are blocked. A rejected feedback or completed correction allows another submission only if the deadline has not passed, and only on the current slip. No withdraw, edit or delete route is provided.

## List/Search/Filter

```http
GET /api/payroll-feedback-requests?keyword=Nguyễn&userId=...&payrollPeriodId=12&status=APPROVED&correctionStatus=PENDING&reasonCode=DEDUCTION&createdFrom=2026-10-15&createdTo=2026-10-15&page=1&pageSize=20
```

- `keyword`: case-insensitive literal substring of employee snapshot name, feedback description, period snapshot code/name; a numeric keyword also matches the exact feedback ID. Whitespace is trimmed. Empty removes search; `%`/`_` are literal characters.
- `userId`, `payrollPeriodId`, `payslipId`: exact match within the actor scope. Staff cannot override their owner ID.
- `status`: `SUBMITTED`, `APPROVED`, `REJECTED`.
- `correctionStatus`: `PENDING`, `IN_PROGRESS`, `COMPLETED`. Null means no correction workflow, such as submitted/rejected feedback; it is not treated as `PENDING`.
- `reasonCode`: one of the five codes above.
- `createdFrom`/`createdTo`: inclusive calendar dates in UTC+7, implemented as >= start of first day and < start of the day after the last.
- `page`: >=1, default 1. `pageSize`: 1–100, default 20. Latest submission first, then feedback ID descending.

All search/filter conditions are combined. No automatic restriction to pending items. List returns source IDs/version, status, correction status, processing metadata, resolution and correction reference. Details also returns original Payroll and correction Payroll calculation lines, plus the correction payslip ID when publication has completed.

## Review

```http
POST /api/payroll-feedback-requests/15/approve
```
```json
{ "resolutionNote": "Khoản giảm trừ bị tính sai; cần điều chỉnh tiền phạt." }
```

Approve may use `{}` if no note is needed. Only `SUBMITTED` can be reviewed. The accepted feedback becomes `APPROVED`, `correction_status = PENDING`, and stores Accountant ID/time/note. This acknowledges a valid feedback; it does not itself change Payroll or create a new version.

```http
POST /api/payroll-feedback-requests/15/reject
```
```json
{ "resolutionNote": "Khoản giảm trừ khớp với dữ liệu và mức áp dụng đã được duyệt." }
```

Reject requires a nonblank note (maximum 2000 characters). Sets `REJECTED`, processing metadata and reason, with no correction status/version. Processing a timely feedback may continue after Thursday ends, while the period is still `APPROVED`. Review cannot overwrite an already processed feedback.

## Correction and remaining workflow

```http
POST /api/payroll-feedback-requests/15/correction
```
```json
{
  "correctionReason": "Điều chỉnh khoản giảm trừ theo feedback #15",
  "attendanceDeductions": [
    { "attendancePenaltyId": 20, "amount": 20000, "reason": "Áp dụng đúng mức giảm trừ đã kiểm tra" }
  ]
}
```

The body uses existing `PayrollCorrectionRequest`: optional `salaryHistoryId`, `holidayPayRuleId`, `attendanceDeductions`; amounts are monetary deduction corrections only. `feedbackId` is optional because the URL identifies it; if supplied it must match the URL. No official gross/net/work minutes are accepted from the frontend.

The feedback must be `APPROVED` with `PENDING`/`IN_PROGRESS`. The source is its original Payroll or latest rejected correction pointed to by `resolved_payroll_id`. The existing revision service revalidates the latest version and feedback after transaction locks, blocks parallel/no-op correction, preserves `timesheet_id` and exact `work_input_snapshot`, and creates a `CALCULATED` Payroll version. Feedback becomes `IN_PROGRESS` and points to that candidate. Repeated correction while a calculated/submitted candidate exists returns 409; if Admin rejects the candidate, a new corrected version can be created through the same feedback endpoint.

Then continue with existing routes:

1. Accountant `POST /api/accountant/payroll-periods/{periodId}/payrolls/{newPayrollId}/submit`.
2. Admin reviews the candidate and `POST /api/payrolls/{newPayrollId}/approve-correction` (or rejects it through the existing Admin route).
3. Approval atomically publishes the replacement, replaces the old slip and marks feedback correction `COMPLETED`, with `resolved_at`. The feedback decision status remains `APPROVED`.

An expired deadline does not prevent completing timely feedback/correction. It prevents new submissions, including on replacement slips. `PAID`/`CLOSED` must wait until unresolved feedback/corrections are finished; these endpoints do not mark payment or close the period.

HTTP errors: invalid input 400; missing/expired authentication 401 from middleware; role/scope override 403; inaccessible ID 404; invalid business state/deadline/duplicate/no-op 409. Successful requests return 200, following existing controller conventions.
