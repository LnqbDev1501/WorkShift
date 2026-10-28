using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
using WorkShiftManagement.BLL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Manager;

[ApiController]
[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
public abstract class EmployeeAdjustmentControllerBase(IEmployeeAdjustmentService service, ILogger logger) : ControllerBase
{
    protected abstract string AdjustmentType { get; }
    protected async Task<IActionResult> Execute(Func<Guid, Task<IActionResult>> action)
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
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Employee adjustment persistence failed.");
            return StatusCode(409, new ProblemDetails { Status = 409, Detail = "Không thể lưu thưởng/phạt do ràng buộc dữ liệu. Hãy tải lại dữ liệu trước khi thử lại.", Instance = Request.Path });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Employee adjustment request failed.");
            return StatusCode(500, new ProblemDetails { Status = 500, Detail = "Không thể xử lý thưởng/phạt. Vui lòng tải lại trạng thái trước khi thử lại.", Instance = Request.Path });
        }
    }
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeAdjustmentResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromQuery] EmployeeAdjustmentQuery query, CancellationToken ct)
        => Execute(async actor => Ok(await service.ListAsync(actor, AdjustmentType, query, ct)));
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Detail(int id, CancellationToken ct)
        => Execute(async actor => Ok(await service.DetailAsync(actor, AdjustmentType, id, ct)));
    [HttpPost, Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status201Created)]
    public Task<IActionResult> Create([FromBody] SaveEmployeeAdjustmentRequest request, CancellationToken ct)
        => Execute(async actor => { var result = await service.CreateAsync(actor, AdjustmentType, request, ct); return CreatedAtAction(nameof(Detail), new { id = result.RewardPenaltyId }, result); });
    [HttpPatch("{id:int}"), Authorize(Roles = "MANAGER")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status200OK)]
    public Task<IActionResult> Update(int id, [FromBody] SaveEmployeeAdjustmentRequest request, CancellationToken ct)
        => Execute(async actor => Ok(await service.UpdateAsync(actor, AdjustmentType, id, request, ct)));
    [HttpDelete("{id:int}"), Authorize(Roles = "MANAGER")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => Execute(async actor => { await service.DeleteAsync(actor, AdjustmentType, id, ct); return NoContent(); });
}
