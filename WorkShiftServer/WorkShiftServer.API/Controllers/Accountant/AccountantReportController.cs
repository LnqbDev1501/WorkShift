using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Accountant;

namespace WorkShiftServer.API.Controllers.Accountant;

/// <summary>
/// Sales reports for accountants (system-wide).
/// </summary>
[Route("api/accountant/reports")]
[ApiController]
[Authorize(Roles = "ACCOUNTANT,ADMIN")]
public class AccountantReportController : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IBranchSalesReportService _branchSalesReportService;

    public AccountantReportController(IBranchSalesReportService branchSalesReportService)
    {
        _branchSalesReportService = branchSalesReportService;
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }

    /// <summary>
    /// Exports the sales report (Excel) for a single branch within an optional date range.
    /// </summary>
    [HttpGet("branch-sales")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportBranchSalesReport(
        [FromQuery] int? branchId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        if (branchId is null || branchId <= 0)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "A branch must be selected to export the sales report."
            });

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "The start date cannot be after the end date."
            });

        try
        {
            var bytes = await _branchSalesReportService.GenerateBranchSalesReportAsync(
                branchId.Value, dateFrom, dateTo, cancellationToken);
            var fileName = $"branch_sales_{branchId.Value}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            return File(bytes, ExcelContentType, fileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = exception.Message
            });
        }
    }
}
