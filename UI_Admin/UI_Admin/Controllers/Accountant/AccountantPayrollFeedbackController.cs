using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
namespace UI_Admin.Controllers.Accountant;
[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollFeedbackController(PayrollDocumentsApiService api, ILogger<AccountantPayrollFeedbackController> logger) : PayrollFeedbackUiController(api, logger)
{
    [HttpGet] public Task<IActionResult> Index([FromQuery] PayrollFeedbackListRequest query, CancellationToken ct) => List(query, ct);
    [HttpGet] public Task<IActionResult> Details(int id, CancellationToken ct) => Detail(id, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Approve(int id, FeedbackReviewInput input, CancellationToken ct) => Review(id, input, false, ct);
    [HttpPost, ValidateAntiForgeryToken] public Task<IActionResult> Reject(int id, FeedbackReviewInput input, CancellationToken ct) => Review(id, input, true, ct);
    private async Task<IActionResult> Review(int id, FeedbackReviewInput input, bool reject, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        if (reject && string.IsNullOrWhiteSpace(input.ResolutionNote)) ModelState.AddModelError("ResolutionNote", "Từ chối phản hồi phải ghi lý do.");
        if (!ModelState.IsValid) { TempData["WorkflowError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)); return RedirectToAction(nameof(Details), new { id }); }
        return await Change(async () => { await Api.Post<PayrollFeedbackResponse>($"/payroll-feedback-requests/{id}/{(reject ? "reject" : "approve")}", new { resolutionNote = input.ResolutionNote?.Trim() }, ct); }, () => RedirectToAction(nameof(Details), new { id }), ct);
    }
}
