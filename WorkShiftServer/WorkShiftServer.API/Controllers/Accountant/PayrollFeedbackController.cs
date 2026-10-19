using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.Interfaces.Accountant;
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
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
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
    [HttpPost("payroll-feedback-requests/{id:int}/reject"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> RejectFeedback(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct) => Execute(async a => await service.RejectAsync(a, id, request, ct));
    [HttpPost("payrolls/{id:int}/correction-requests"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> CreateIssue(int id, CreatePayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.CreateIssueAsync(a, id, request, ct));
    [HttpGet("payroll-periods/{id:int}/correction-requests"), Authorize(Roles = "ACCOUNTANT,MANAGER")]
    public Task<IActionResult> Issues(int id, CancellationToken ct) => Execute(async a => await service.IssuesAsync(a, id, ct));
    [HttpPost("payroll-correction-requests/{id:int}/respond"), Authorize(Roles = "MANAGER")]
    public Task<IActionResult> Respond(int id, RespondPayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.RespondAsync(a, id, request, ct));
    [HttpPost("payroll-correction-requests/{id:int}/review"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> ReviewIssue(int id, ReviewPayrollIssueRequest request, CancellationToken ct) => Execute(async a => await service.ReviewIssueAsync(a, id, request, ct));
}
