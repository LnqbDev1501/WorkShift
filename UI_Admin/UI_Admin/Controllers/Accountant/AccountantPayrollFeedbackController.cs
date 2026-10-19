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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, FeedbackReviewInput input, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        return await Change(async () =>
        {
            await Api.ApproveFeedbackAsync(id, input.ResolutionNote?.Trim(), ct);
        }, () => RedirectToAction(nameof(Details), new { id }), ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Explain(int id, FeedbackReviewInput input, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        if (string.IsNullOrWhiteSpace(input.ResolutionNote))
        {
            TempData["WorkflowError"] = "Explanation note is required to explain feedback.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return await Change(async () =>
        {
            await Api.ExplainFeedbackAsync(id, input.ResolutionNote.Trim(), ct);
        }, () => RedirectToAction(nameof(Details), new { id }), ct);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id, FeedbackReviewInput input, CancellationToken ct) => Explain(id, input, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendPreview(int feedbackId, string reason, CancellationToken ct)
    {
        if (feedbackId <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["WorkflowError"] = "Please provide a reason to resend the preview to staff.";
            return RedirectToAction(nameof(Details), new { id = feedbackId });
        }
        return await Change(async () =>
        {
            await Api.ResendPreviewAsync(feedbackId, reason.Trim(), ct);
        }, () => RedirectToAction(nameof(Details), new { id = feedbackId }), ct);
    }
}
