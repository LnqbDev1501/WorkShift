using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
namespace UI_Admin.Controllers.Staff;
[Authorize(Roles = "STAFF"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class StaffPayrollFeedbackController(PayrollDocumentsApiService api, ILogger<StaffPayrollFeedbackController> logger) : PayrollFeedbackUiController(api, logger)
{
    [HttpGet] public Task<IActionResult> Index([FromQuery] PayrollFeedbackListRequest query, CancellationToken ct) => List(query, ct);
    [HttpGet] public Task<IActionResult> Details(int id, CancellationToken ct) => Detail(id, ct);
    [HttpGet] public Task<IActionResult> Create(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    {
        var slip = (await Api.Payslip(id, ct)).Payslip;
        if (!slip.CanSubmitFeedback) { TempData["WorkflowError"] = slip.FeedbackBlockedReason ?? "Phiếu không nhận phản hồi mới."; return RedirectToAction("Details", "StaffPayslips", new { id }); }
        return View("~/Views/PayrollFeedback/Create.cshtml", new FeedbackSubmitForm { Payslip = slip });
    }, ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create(int id, [Bind("ReasonCode,Description")] SubmitPayrollFeedbackRequest input, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    {
        var slip = (await Api.Payslip(id, ct)).Payslip;
        if (!slip.CanSubmitFeedback) { TempData["WorkflowError"] = slip.FeedbackBlockedReason ?? "Phiếu không nhận phản hồi mới."; return RedirectToAction("Details", "StaffPayslips", new { id }); }
        if (!PayrollDocumentDisplay.Reasons.Contains(input.ReasonCode)) ModelState.AddModelError("ReasonCode", "Loại phản hồi không hợp lệ.");
        if (ModelState.IsValid)
            try
            {
                var result = await Api.Post<PayrollFeedbackResponse>($"/payslips/{id}/feedback-requests", new { reasonCode = input.ReasonCode, description = input.Description.Trim() }, ct);
                return RedirectToAction(nameof(Details), new { id = result.FeedbackId });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; ModelState.AddModelError("", Message(ex)); }
        return View("~/Views/PayrollFeedback/Create.cshtml", new FeedbackSubmitForm { Payslip = slip, Input = input });
    }, ct);
}
