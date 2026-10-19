using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Admin.Payroll;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
namespace UI_Admin.Controllers.Admin;
[Authorize(Roles = "ADMIN"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminPayrollRevisionController(PayrollDocumentsApiService api, ILogger<AdminPayrollRevisionController> logger) : PayrollDocumentsUiController(logger)
{
    [HttpGet] public Task<IActionResult> Review(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    { var p = await api.Payroll(id, ct); return View("~/Views/PayrollRevision/Review.cshtml", new PayrollReview { Payroll = p, History = await api.Versions(id, ct), PeriodStatus = await api.PeriodStatus(p.PayrollPeriodId, false, ct) }); }, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> ApproveCorrection(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Change(async () =>
    { await api.Post<CalculatedPayroll>($"/payrolls/{id}/approve-correction", null, ct); }, () => RedirectToAction(nameof(Review), new { id }), ct);
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, [Bind("Reason,RejectionCategory")] PayrollRejectionDecision input, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        if (!ModelState.IsValid) { TempData["WorkflowError"] = "Chọn loại lỗi và nhập lý do từ chối (tối đa 1000 ký tự)."; return RedirectToAction(nameof(Review), new { id }); }
        return await Change(async () => { var p = await api.Payroll(id, ct); await api.Post<System.Text.Json.JsonElement>($"/admin/payroll-periods/{p.PayrollPeriodId}/payrolls/{id}/reject", new { reason = input.Reason.Trim(), rejectionCategory = input.RejectionCategory }, ct); }, () => RedirectToAction(nameof(Review), new { id }), ct);
    }
}
