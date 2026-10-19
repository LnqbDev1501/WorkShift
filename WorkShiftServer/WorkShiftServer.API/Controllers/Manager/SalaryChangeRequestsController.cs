using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.SalaryChanges;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Route("api/salary-change-requests")]
[Authorize(Roles = "MANAGER,ADMIN")]
public sealed class SalaryChangeRequestsController(ISalaryChangeService service, ILogger<SalaryChangeRequestsController> logger) : ControllerBase
{
    private async Task<IActionResult> Execute(Func<Guid, Task<IActionResult>> action)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var id) || id == Guid.Empty) return Unauthorized(new { message = "Tài khoản không hợp lệ." });
        try { return await action(id); }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
        catch (Exception ex) when (ex is DbUpdateException || ex is PostgresException { SqlState: "23505" or "23503" or "23514" or "40P01" or "40001" or "55P03" })
        {
            logger.LogError(ex, "Salary change transaction conflicted with persisted data.");
            return StatusCode(409, new ProblemDetails { Status = 409, Detail = "Dữ liệu tăng lương đã thay đổi hoặc vi phạm ràng buộc. Hãy tải lại trước khi thử lại.", Instance = Request.Path });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Salary change request failed.");
            return StatusCode(500, new ProblemDetails { Status = 500, Detail = "Không thể xử lý đề xuất tăng lương. Vui lòng tải lại trạng thái trước khi thử lại.", Instance = Request.Path });
        }
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SalaryChangeResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromQuery] SalaryChangeQuery query, CancellationToken ct)
        => Execute(async actor => Ok(await service.ListAsync(actor, query, ct)));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SalaryChangeResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Detail(int id, CancellationToken ct)
        => Execute(async actor => Ok(await service.DetailAsync(actor, id, ct)));

    [HttpPost, Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(SalaryChangeResponse), StatusCodes.Status201Created)]
    public Task<IActionResult> Create([FromBody] CreateSalaryChangeRequest request, CancellationToken ct)
        => Execute(async actor => { var result = await service.CreateAsync(actor, request, ct);
            return CreatedAtAction(nameof(Detail), new { id = result.SalaryChangeRequestId }, result); });

    [HttpPost("{id:int}/cancel"), Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(SalaryChangeResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Cancel(int id, [FromBody] CancelSalaryChangeRequest request, CancellationToken ct)
        => Execute(async actor => Ok(await service.CancelAsync(actor, id, request, ct)));

    [HttpPost("{id:int}/approve"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SalaryChangeResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Approve(int id, [FromBody] ApproveSalaryChangeRequest request, CancellationToken ct)
        => Execute(async actor => Ok(await service.ApproveAsync(actor, id, request, ct)));

    [HttpPost("{id:int}/reject"), Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(SalaryChangeResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Reject(int id, [FromBody] RejectSalaryChangeRequest request, CancellationToken ct)
        => Execute(async actor => Ok(await service.RejectAsync(actor, id, request, ct)));
}
