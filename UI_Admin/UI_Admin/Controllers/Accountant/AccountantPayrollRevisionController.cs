using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollRevisionController(PayrollDocumentsApiService api, ILogger<AccountantPayrollRevisionController> logger)
    : PayrollDocumentsUiController(logger)
{
    [HttpGet]
    public Task<IActionResult> Review(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () =>
    {
        var p = await api.Payroll(id, ct);
        var history = await api.Versions(id, ct);
        var periodStatus = await api.PeriodStatus(p.PayrollPeriodId, true, ct);
        var review = new PayrollReview
        {
            Payroll = p,
            History = history,
            PeriodStatus = periodStatus
        };
        try
        {
            var allIssues = await api.CorrectionRequestsAsync(p.PayrollPeriodId, ct);
            var versionIds = history.Versions.Select(v => v.PayrollId).ToHashSet();
            review.Issues = allIssues.Where(r => versionIds.Contains(r.PayrollId) || (r.ResolvedPayrollId.HasValue && versionIds.Contains(r.ResolvedPayrollId.Value))).ToList();
        }
        catch { /* non-blocking optional fetch */ }
        try
        {
            var feedbacks = await api.Get<PayrollFeedbackListResponse>($"/payroll-feedback-requests?payrollPeriodId={p.PayrollPeriodId}&pageSize=100", ct);
            review.LatestFeedbackId = feedbacks.Items.Where(f => f.UserId == p.UserId).MaxBy(f => f.FeedbackId)?.FeedbackId;
        }
        catch { /* non-blocking optional fetch */ }
        return View("~/Views/PayrollRevision/Review.cshtml", review);
    }, ct);

    // Step 5: Accountant creates a Correction Request for a Payroll
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestCorrection(CreateCorrectionInput input, CancellationToken ct)
    {
        if (input.PayrollId <= 0 || string.IsNullOrWhiteSpace(input.Reason))
        {
            TempData["ErrorMessage"] = "Please enter the reason for the correction request (up to 2000 characters).";
            return RedirectToAction(nameof(Review), new { id = input.PayrollId });
        }

        try
        {
            await api.CreateCorrectionRequestAsync(input.PayrollId, input.Reason.Trim(), ct);
            TempData["SuccessMessage"] = "Correction request submitted to Manager successfully.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = input.PayrollId });
    }

    // Step 5: Accountant reviews Manager's response (ACCEPT_EXPLANATION or RETURN)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewCorrection(ReviewCorrectionInput input, int payrollId, CancellationToken ct)
    {
        if (input.CorrectionRequestId <= 0 || string.IsNullOrWhiteSpace(input.Note) || !ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please enter your review note for this response.";
            return RedirectToAction(nameof(Review), new { id = payrollId });
        }

        try
        {
            await api.ReviewCorrectionAsync(input.CorrectionRequestId, input.Decision, input.Note.Trim(), ct);
            TempData["SuccessMessage"] = "Manager response reviewed successfully.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = payrollId });
    }

    // Step 6: Accountant previews correction after Manager responded CORRECT
    [HttpGet]
    public async Task<IActionResult> PreviewCorrection(int payrollId, int correctionRequestId, CancellationToken ct)
    {
        if (payrollId <= 0 || correctionRequestId <= 0) return NotFound();
        try
        {
            var preview = await api.PreviewCorrectionAsync(payrollId, correctionRequestId, ct);
            return View("~/Views/PayrollRevision/CorrectionPreview.cshtml", preview);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
            return RedirectToAction(nameof(Review), new { id = payrollId });
        }
    }

    // Step 6: Recalculate rejected payroll
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(int payrollId, int correctionRequestId, CancellationToken ct)
    {
        if (payrollId <= 0 || correctionRequestId <= 0) return BadRequest();
        try
        {
            var next = await api.RecalculatePayrollAsync(payrollId, correctionRequestId, ct);
            TempData["SuccessMessage"] = $"Payroll recalculated successfully (Version V{next.VersionNo}). Please review the result and complete the correction.";
            return RedirectToAction(nameof(Review), new { id = next.PayrollId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
            return RedirectToAction(nameof(Review), new { id = payrollId });
        }
    }

    // Step 6: Accountant completes correction
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCorrection(CompleteCorrectionInput input, CancellationToken ct)
    {
        if (input.CorrectionRequestId <= 0 || input.PayrollId <= 0 || string.IsNullOrWhiteSpace(input.Note) || !ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please enter the completion note.";
            return RedirectToAction(nameof(Review), new { id = input.PayrollId });
        }

        try
        {
            await api.CompleteCorrectionAsync(input.CorrectionRequestId, input.PayrollId, input.Note.Trim(), ct);
            TempData["SuccessMessage"] = "Correction request completed successfully.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = input.PayrollId });
    }

    // Step 6: Register related shared-source impact
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterRelatedImpact(int requestId, int payrollId, CancellationToken ct)
    {
        if (requestId <= 0) return BadRequest();
        try
        {
            await api.RegisterRelatedImpactAsync(requestId, ct);
            TempData["SuccessMessage"] = "Related source impacts registered successfully.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = payrollId });
    }

    // Step 5: Accountant resends correction request to Manager (e.g. after EXPIRED)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendCorrection(int requestId, int payrollId, string reason, CancellationToken ct)
    {
        if (requestId <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do gửi lại yêu cầu kiểm tra cho Quản lý.";
            return RedirectToAction(nameof(Review), new { id = payrollId });
        }

        try
        {
            await api.ResendCorrectionAsync(requestId, reason.Trim(), ct);
            TempData["SuccessMessage"] = "Đã gửi lại yêu cầu kiểm tra nguồn cho Quản lý với hạn phản hồi mới (23:59:59).";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = payrollId });
    }

    // Step 6/8: Accountant explicitly resends preview to Staff with reason
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendPreview(int feedbackId, int payrollId, string reason, CancellationToken ct)
    {
        if (feedbackId <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do gửi lại bản xem trước (Preview) cho Nhân viên.";
            return RedirectToAction(nameof(Review), new { id = payrollId });
        }

        try
        {
            await api.ResendPreviewAsync(feedbackId, reason.Trim(), ct);
            TempData["SuccessMessage"] = "Đã gửi lại bản xem trước (Payroll Preview) cho Nhân viên thành công với hạn phản hồi mới.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            TempData["ErrorMessage"] = Message(ex);
        }

        return RedirectToAction(nameof(Review), new { id = payrollId });
    }
}
