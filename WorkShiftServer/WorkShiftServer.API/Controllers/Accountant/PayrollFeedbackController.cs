using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

[ApiController, Authorize(Roles = "ACCOUNTANT,STAFF"), Route("api")]
public sealed class PayrollFeedbackController(IPayrollFeedbackService service) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<object>> action)
    {
        try
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var actor) || actor == Guid.Empty)
                throw new UnauthorizedAccessException("Tài khoản không hợp lệ.");
            return Ok(await action(actor));
        }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
    }

    [HttpPost("payslips/{id:int}/feedback-requests"), Authorize(Roles = "STAFF")]
    public Task<IActionResult> Submit(int id, SubmitPayrollFeedbackRequest request, CancellationToken ct)
        => Execute(async actor => await service.SubmitAsync(actor, id, request, ct));
    [HttpGet("payroll-feedback-requests")]
    public Task<IActionResult> List([FromQuery] PayrollFeedbackListRequest query, CancellationToken ct)
        => Execute(async actor => await service.ListAsync(actor, query, ct));
    [HttpGet("payroll-feedback-requests/{id:int}")]
    public Task<IActionResult> Details(int id, CancellationToken ct)
        => Execute(async actor => await service.GetAsync(actor, id, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/approve"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> Approve(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
        => Execute(async actor => await service.ApproveAsync(actor, id, request, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/reject"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> Reject(int id, ReviewPayrollFeedbackRequest request, CancellationToken ct)
        => Execute(async actor => await service.RejectAsync(actor, id, request, ct));
    [HttpPost("payroll-feedback-requests/{id:int}/correction"), Authorize(Roles = "ACCOUNTANT")]
    public Task<IActionResult> Correction(int id, PayrollCorrectionRequest request, CancellationToken ct)
        => Execute(async actor => await service.CreateCorrectionAsync(actor, id, request, ct));
}
