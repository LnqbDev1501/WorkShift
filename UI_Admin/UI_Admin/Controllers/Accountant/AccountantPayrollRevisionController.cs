using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
namespace UI_Admin.Controllers.Accountant;
[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollRevisionController(PayrollDocumentsApiService api, ILogger<AccountantPayrollRevisionController> logger) : PayrollDocumentsUiController(logger)
{
    [HttpGet] public Task<IActionResult> Review(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    { var p = await api.Payroll(id, ct); return View("~/Views/PayrollRevision/Review.cshtml", new PayrollReview { Payroll = p, History = await api.Versions(id, ct), PeriodStatus = await api.PeriodStatus(p.PayrollPeriodId, true, ct) }); }, ct);
    [HttpGet] public Task<IActionResult> Correct(int id, CancellationToken ct) => Form(id, false, null, ct);
    [HttpGet] public Task<IActionResult> CorrectFeedback(int id, CancellationToken ct) => Form(id, true, null, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Correct(int id, CorrectionInput input, CancellationToken ct) => Form(id, false, input, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> CorrectFeedback(int id, CorrectionInput input, CancellationToken ct) => Form(id, true, input, ct);
    private Task<IActionResult> Form(int id, bool feedback, CorrectionInput? input, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    {
        CalculatedPayroll p;
        if (feedback)
        {
            var f = await api.Feedback(id, ct);
            if (f.Feedback.Status != "APPROVED" || f.Feedback.CorrectionStatus == "COMPLETED" || f.CorrectionPayroll != null && f.CorrectionPayroll.Status != "REJECTED")
            { TempData["WorkflowError"] = "Feedback chưa được chấp nhận, đã hoàn tất hoặc đã có version sửa đang xử lý."; return RedirectToAction("Details", "AccountantPayrollFeedback", new { id }); }
            p = f.CorrectionPayroll ?? f.OriginalPayroll;
        }
        else
        {
            p = await api.Payroll(id, ct);
            var versions = await api.Versions(id, ct);
            if (p.Status != "REJECTED" || versions.LatestCandidatePayrollId != id || await api.PeriodStatus(p.PayrollPeriodId, true, ct) != "REVISION_REQUIRED")
            { TempData["WorkflowError"] = "Chỉ sửa version REJECTED mới nhất trước duyệt; correction sau phát hành phải đi từ feedback."; return RedirectToAction(nameof(Review), new { id }); }
        }
        if (input != null && ModelState.IsValid)
        {
            if (input.AttendanceDeductions.Any(a => !p.Details.Any(d => d.AttendancePenaltyId == a.AttendancePenaltyId && d.Type == "ATTENDANCE_DEDUCTION")) || input.AttendanceDeductions.Select(a => a.AttendancePenaltyId).Distinct().Count() != input.AttendanceDeductions.Count)
                ModelState.AddModelError("", "Nguồn giảm trừ không thuộc Payroll này hoặc bị trùng.");
            else
                try { var next = await api.Post<CalculatedPayroll>(feedback ? $"/payroll-feedback-requests/{id}/correction" : $"/payrolls/{id}/recalculate", input, ct); return RedirectToAction(nameof(Review), new { id = next.PayrollId }); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; ModelState.AddModelError("", Message(ex)); }
        }
        input ??= new() { AttendanceDeductions = p.Details.Where(d => d.Type == "ATTENDANCE_DEDUCTION" && d.AttendancePenaltyId.HasValue).Select(d => new AttendanceCorrectionInput { AttendancePenaltyId = d.AttendancePenaltyId!.Value, Amount = d.Amount, Reason = d.Note ?? "Điều chỉnh giảm trừ" }).ToList() };
        return View("~/Views/PayrollRevision/Correct.cshtml", new CorrectionForm { SourceId = id, FromFeedback = feedback, Payroll = p, Input = input });
    }, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Submit(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Change(async () =>
    { var p = await api.Payroll(id, ct); await api.Post<JsonElement>($"/accountant/payroll-periods/{p.PayrollPeriodId}/payrolls/{id}/submit", null, ct); }, () => RedirectToAction(nameof(Review), new { id }), ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> SubmitPeriod(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Change(async () =>
    { await api.Post<JsonElement>($"/accountant/payroll-periods/{id}/submit", null, ct); }, () => RedirectToAction("Details", "AccountantPayrollPeriods", new { id }), ct);
}
