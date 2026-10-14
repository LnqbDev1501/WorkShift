# Payslip Management

Backend routes use JWT authentication. Roles are verified again against active users/roles in the database.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/payslips` | View/Search/Filter the visible payslip list |
| GET | `/api/payslips/{id}` | Full published payslip, frozen work rows and monetary calculation details |
| GET | `/api/payslips/{id}/versions` | All published/replaced versions for this employee and period, including the current ID |

Admin sees all branches; Accountant sees their assigned branch; Staff sees only their own payslips. Manager has no access. These scopes apply to every route, including version history, before filtering. Cross-scope detail/history IDs return 404. A request explicitly overriding Staff's user ID or Accountant's branch ID returns 403.

## List query

```http
GET /api/payslips?keyword=Nguyễn&payrollPeriodId=12&status=PUBLISHED&periodFrom=2026-10-05&periodTo=2026-10-11&page=1&pageSize=20
```

- `keyword`: case-insensitive literal substring of employee name snapshot, period name snapshot or period code snapshot. Leading/trailing spaces are removed; empty clears the search. `%` and `_` are literal characters.
- `userId`: employee filter within the actor's permitted scope.
- `branchId`: branch filter; only Admin can use it to access another branch.
- `payrollPeriodId`: exact period.
- `status`: `PUBLISHED` or `REPLACED`. Payroll's `PAID` is not a payslip status.
- `includeHistory`: default `false` (current `PUBLISHED` only). `true` includes replaced rows if no explicit status is supplied. Explicit status takes precedence.
- `periodFrom`, `periodTo`: inclusive overlap with the frozen period dates; either bound can be omitted.
- `page`: default 1. `pageSize`: default 20, maximum 100. Order: publication time descending, ID descending.

Search and all filters are combined. Staff may continue viewing their own published and replaced history after the feedback deadline or period closure.

## Details and feedback availability

The response contains `payslip` (employee/branch/period snapshots, totals and metadata), `workDetails` and `calculationDetails`. Work rows are read exclusively from `payslip_work_details`; the query does not join current Attendance/Shift to replace displayed data. Monetary rows and totals come from this payslip's immutable Payroll version. Amount is the full obligation; `appliedAmount` is the amount actually deducted in this period.

Initial publication is scheduled for Thursday 00:00 UTC+7 following the frozen period end (Sunday). `feedbackDeadlineAt` is the next Friday 00:00 UTC+7 (exclusive). This is a fixed 24-hour calendar window covering Thursday, not a fresh 24 hours from each actual publication. Replacement and technical retry do not reset it. UTC timestamps are returned: Thursday 00:00 UTC+7 = Wednesday 17:00Z; Friday 00:00 UTC+7 = Thursday 17:00Z.

`canSubmitFeedback` is true only for owning Staff while the slip is current `PUBLISHED`, its Payroll/Period are `APPROVED`, current time is before Friday 00:00 and there is no unresolved submitted feedback or accepted unfinished correction for the same employee/period. `feedbackBlockedReason` explains a false result. The Feedback API revalidates these conditions transactionally when accepting a POST; see `PayrollFeedbackManagement.md`. Payment/settlement is a separate module.

## Automatic publication

No publication table or new fields are required. The backend no longer references `payroll_publications`; its previous entity/mapping and creation migration have been removed. If that table was already created, it can remain unused; this code change does not execute SQL to drop it. Existing `payslips.payroll_id UNIQUE`, `published_at`, status and links to Payroll/Period provide the required publication evidence.

Hangfire recurring job `initial-payslip-publication` keeps its existing ID and is now scheduled for Wednesday 17:00 UTC = Thursday 00:00 UTC+7. Restarting the API updates this job's schedule; no separate new job is created. There is no manual publication HTTP endpoint. The API and Hangfire storage/server must be running at that time; a scheduler cannot guarantee execution during an outage. Technical retries are idempotent and retain the fixed cutoff; there is no publication flow after Thursday ends.

Only that previous week's approved periods are candidates. Publication checks that approval was before `approval_deadline_at` (fallback Thursday 00:00 UTC+7), every expected employee has a current approved Payroll, totals are consistent and every work snapshot is valid. No period is partially published. All payslips, their work details and an audit entry recording scheduled/actual times and count are committed in one transaction. The period remains `APPROVED`; publishing does not mark payment or close the period.

Publication completion is checked against ALL current Payrolls: each must have a Payslip. Replaced historical slips still count as publication evidence; older rejected Payrolls without slips do not make a period unpublished. Retry after successful commit returns without inserting duplicates. The transaction lock prevents two jobs publishing concurrently, with `UNIQUE(payroll_id)` as the database safeguard. If some slips exist but other current Payrolls have none, the job reports inconsistent partial data rather than treating the whole period as complete or automatically republishing it. Different branches are processed independently. Failures are logged and recorded by Hangfire for retries/inspection.

Post-publication correction continues using the existing `/api/payrolls/{id}/approve-correction`: Admin approval immediately creates a replacement via the same snapshot factory, replaces the old payslip and completes its feedback. There is no separate user-triggered publish-correction route. A valid feedback submitted during Thursday can finish after the deadline, but the replacement then cannot receive another feedback. Settlement still waits for all timely feedback/corrections to finish.

## Testing

1. Restart the backend to update the Hangfire schedule. No publication migration is required.
2. Prepare an approved prior-week period with valid work snapshots and approval before its deadline. Observe the initial job at Thursday 00:00 UTC+7; no duplicate slips should appear on retry.
3. Call the three GET routes using Admin, Accountant and Staff tokens. Verify scoped lists, cross-user/branch IDs and combined search/filters.
4. Compare work rows and amounts to the published version. Subsequent live Attendance changes must not change the response.
5. Test correction approval and history: old slip is `REPLACED`, new slip is `PUBLISHED`, history preserves both, fixed deadline remains Friday 00:00 UTC+7 (exclusive).
