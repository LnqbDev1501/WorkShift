using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.EmployeeAdjustments;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftServer.API.Controllers.Staff;

[Route("api/staff/adjustments")]
[ApiController]
[Authorize(Roles = "STAFF")]
public class StaffAdjustmentController(IEmployeeAdjustmentRepo repo) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmployeeAdjustmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetList(
        [FromQuery] string type,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        var normalizedType = (type ?? "").Trim().ToUpperInvariant();
        if (normalizedType is not ("REWARD" or "PENALTY"))
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Bad Request", Detail = "Type must be 'REWARD' or 'PENALTY'." });

        string? normalizedStatus = string.IsNullOrWhiteSpace(status) || status.Equals("ALL", StringComparison.OrdinalIgnoreCase)
            ? null
            : status.Trim().ToUpperInvariant();

        if (normalizedStatus != null && normalizedStatus is not ("PENDING" or "APPROVED" or "REJECTED"))
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Bad Request", Detail = "Status must be 'PENDING', 'APPROVED', or 'REJECTED'." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var filter = new EmployeeAdjustmentFilter(
            Keyword: null,
            UserId: actor.Value,
            PeriodId: null,
            BranchId: null,
            Status: normalizedStatus,
            From: null,
            ToExclusive: null,
            Page: page,
            PageSize: pageSize
        );

        var (items, count) = await repo.ListAsync(normalizedType, filter, ct);

        return Ok(new PagedResponse<EmployeeAdjustmentResponse>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = count
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmployeeAdjustmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] string type, CancellationToken ct = default)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        var normalizedType = (type ?? "").Trim().ToUpperInvariant();
        if (normalizedType is not ("REWARD" or "PENALTY"))
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Bad Request", Detail = "Type must be 'REWARD' or 'PENALTY'." });

        var item = await repo.FindAsync(id, normalizedType, null, false, ct);
        if (item == null || item.IsDeleted || item.UserId != actor.Value)
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = "Record not found." });

        return Ok(Map(item));
    }

    [HttpGet("/api/staff/rewards")]
    public Task<IActionResult> GetRewards([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => GetList("REWARD", status, page, pageSize, ct);

    [HttpGet("/api/staff/rewards/{id:int}")]
    public Task<IActionResult> GetRewardDetail(int id, CancellationToken ct = default)
        => GetDetail(id, "REWARD", ct);

    [HttpGet("/api/staff/penalties")]
    public Task<IActionResult> GetPenalties([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => GetList("PENALTY", status, page, pageSize, ct);

    [HttpGet("/api/staff/penalties/{id:int}")]
    public Task<IActionResult> GetPenaltyDetail(int id, CancellationToken ct = default)
        => GetDetail(id, "PENALTY", ct);

    private static EmployeeAdjustmentResponse Map(EmployeeRewardsPenalty a) => new()
    {
        RewardPenaltyId = a.RewardPenaltyId,
        AdjustmentType = a.AdjustmentType,
        UserId = a.UserId,
        EmployeeName = a.User?.FullName ?? "",
        BranchId = a.PayrollPeriod?.BranchId ?? a.User?.BranchId ?? 0,
        BranchName = a.PayrollPeriod?.BranchNameSnapshot ?? "",
        PayrollPeriodId = a.PayrollPeriodId,
        PeriodCode = a.PayrollPeriod?.PeriodCode ?? "",
        PeriodName = a.PayrollPeriod?.PeriodName ?? "",
        PeriodStart = a.PayrollPeriod?.StartDate ?? DateOnly.MinValue,
        PeriodEnd = a.PayrollPeriod?.EndDate ?? DateOnly.MinValue,
        PeriodStatus = a.PayrollPeriod?.Status ?? "",
        AdjustmentName = a.AdjustmentName,
        Amount = a.Amount,
        Reason = a.Reason,
        ApprovalStatus = a.ApprovalStatus ?? "",
        CreatedBy = a.CreatedBy,
        CreatedByName = a.CreatedByNavigation?.FullName,
        CreatedAt = a.CreatedAt,
        ApprovedBy = a.ApprovedBy,
        ApprovedByName = a.ApprovedByNavigation?.FullName,
        ApprovedAt = a.ApprovedAt,
        RejectedBy = a.RejectedBy,
        RejectedByName = a.RejectedByNavigation?.FullName,
        RejectedAt = a.RejectedAt,
        ReviewNote = a.ReviewNote
    };

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }
}
