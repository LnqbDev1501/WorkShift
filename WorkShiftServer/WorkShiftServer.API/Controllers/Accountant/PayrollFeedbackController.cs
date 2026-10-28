using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.Services.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController, Authorize(Roles = "ACCOUNTANT,STAFF,MANAGER,ADMIN"), Route("api")]
public sealed class PayrollFeedbackController(IPayrollFeedbackService service) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<object>> action)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor) || actor == Guid.Empty) return Unauthorized();
        try { return Ok(await action(actor)); }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            var problem = new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path };
            if (ex is PayrollSourceChangedException changed) problem.Extensions["sourceImpact"] = changed.SourceImpact;
            return StatusCode(status, problem);
        }
    }
    [HttpPost("payroll-periods/{id:int}/send-previews"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> SendPreviews(int id, CancellationToken ct) => Execute(async a => await service.SendPreviewsAsync(a, id, ct));
    [HttpGet("payroll-previews"), HttpGet("payroll-feedback-requests"), Authorize(Roles = "ACCOUNTANT,STAFF")]
    public Task<IActionResult> List([FromQuery] PayrollFeedbackListRequest query, CancellationToken ct) => Execute(async a => await service.ListAsync(a, query, ct));
    [HttpGet("payroll-previews/{id:int}"), HttpGet("payroll-feedback-requests/{id:int}"), Authorize(Roles = "ACCOUNTANT,STAFF")]
    public Task<IActionResult> Detail(int id, CancellationToken ct) => Execute(async a => await service.GetAsync(a, id, ct));
    [HttpPost("payroll-previews/{id:int}/confirm"), Authorize(Roles = "STAFF")]
    public Task<IActionResult> Confirm(int id, CancellationToken ct) => Execute(async a => await service.ConfirmAsync(a, id, ct));
    [HttpPost("payroll-previews/{id:int}/feedback"), Authorize(Roles = "STAFF")]
    public Task<IActionResult> Feedback(int id, SubmitPayrollFeedbackRequest request, CancellationToken ct) => Execute(async a => await service.SubmitAsync(a, id, request, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/approve"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ApproveFeedback(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Execute(async a => await service.ApproveAsync(a, id, request, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/explain"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ExplainFeedback(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Execute(async a => await service.ExplainAsync(a, id, request, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/reject"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> RejectFeedback(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Execute(async a => await service.ExplainAsync(a, id, request, ct));
    [HttpPost("payrolls/{id:int}/correction-requests"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> CreateIssue(int id, CreatePayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.CreateIssueAsync(a, id, request, ct));
    [HttpGet("payroll-periods/{id:int}/correction-requests"), Authorize(Roles = "ACCOUNTANT,MANAGER")]
    public Task<IActionResult> Issues(int id, CancellationToken ct) => Execute(async a => await service.IssuesAsync(a, id, ct));
    [HttpGet("payroll-correction-requests"), Authorize(Roles = "ACCOUNTANT,MANAGER,ADMIN")]
    public Task<IActionResult> ListCorrectionRequests([FromQuery] int? periodId, [FromQuery] string? status, CancellationToken ct)
        => Execute(async a => await service.ListIssuesAsync(a, periodId, status, ct));
    [HttpPost("payroll-correction-requests/{id:int}/respond"), Authorize(Roles = "MANAGER")]
    public Task<IActionResult> Respond(int id, RespondPayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.RespondAsync(a, id, request, ct));
    [HttpPost("payroll-correction-requests/{id:int}/review"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ReviewIssue(int id, ReviewPayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.ReviewIssueAsync(a, id, request, ct));
    [HttpGet("payrolls/{id:int}/correction-preview"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> PreviewCorrection(int id, [FromQuery] int correctionRequestId, CancellationToken ct)
        => Execute(async a => await service.PreviewCorrectionAsync(a, id, correctionRequestId, ct));
    [HttpPost("payroll-correction-requests/{id:int}/complete"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> CompleteCorrection(int id, CompletePayrollCorrectionRequest request, CancellationToken ct)
        => Execute(async a => await service.CompleteCorrectionAsync(a, id, request, ct));
    [HttpGet("payroll-periods/{id:int}/source-impact"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> SourceImpact(int id, CancellationToken ct) => Execute(async a => await service.SourceImpactAsync(a, id, ct));
    [HttpPost("payroll-correction-requests/{id:int}/register-related-impact"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> RegisterRelatedImpact(int id, CancellationToken ct) => Execute(async a => await service.RegisterRelatedImpactAsync(a, id, ct));
    [HttpPost("payroll-correction-requests/{id:int}/resend"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ResendCorrection(int id, ResendPayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.ResendIssueAsync(a, id, request, ct));
    [HttpPost("payroll-previews/{id:int}/resend"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ResendPreview(int id, ResendPayrollPreviewRequest request, CancellationToken ct) => Execute(async a => await service.ResendPreviewAsync(a, id, request, ct));
}
