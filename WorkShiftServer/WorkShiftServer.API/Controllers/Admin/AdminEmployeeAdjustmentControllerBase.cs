using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
using WorkShiftManagement.BLL.Interfaces.Manager;
namespace WorkShiftServer.API.Controllers.Admin;

[ApiController, Authorize(Roles = "ADMIN")]
public abstract class AdminEmployeeAdjustmentControllerBase(IEmployeeAdjustmentService service, ILogger logger) : ControllerBase
{
    protected abstract string AdjustmentType { get; }
    private async Task<IActionResult> Review(int id, ReviewEmployeeAdjustmentRequest request, bool approve, CancellationToken ct)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue("user_id");
        if (!Guid.TryParse(subject, out var actor) || actor == Guid.Empty) return Unauthorized(new { message = "Tài khoản không hợp lệ." });
        try { return Ok(await service.ReviewAsync(actor, AdjustmentType, id, approve, request, ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is ArgumentException or UnauthorizedAccessException or KeyNotFoundException or InvalidOperationException)
        {
            var status = ex switch { ArgumentException => 400, UnauthorizedAccessException => 403, KeyNotFoundException => 404, _ => 409 };
            return StatusCode(status, new ProblemDetails { Status = status, Detail = ex.Message, Instance = Request.Path });
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Employee adjustment review persistence failed.");
            return StatusCode(409, new ProblemDetails { Status = 409, Detail = "Không thể lưu quyết định duyệt do ràng buộc dữ liệu. Hãy tải lại trạng thái trước khi thử lại.", Instance = Request.Path });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Employee adjustment review failed.");
            return StatusCode(500, new ProblemDetails { Status = 500, Detail = "Không xác nhận được kết quả duyệt. Vui lòng tải lại trạng thái trước khi thử lại.", Instance = Request.Path });
        }
    }
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Approve(int id, [FromBody] ReviewEmployeeAdjustmentRequest request, CancellationToken ct)
        => Review(id, request, true, ct);
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Reject(int id, [FromBody] ReviewEmployeeAdjustmentRequest request, CancellationToken ct)
        => Review(id, request, false, ct);
}
