using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Accountant;

/// <summary>
/// Sales reports for accountants (system-wide).
/// </summary>
[Route("api/accountant/reports")]
[ApiController]
[Authorize(Roles = "ACCOUNTANT,ADMIN")]
public class AccountantReportController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AccountantReportController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }

    /// <summary>
    /// Exports branch sales report (CSV/Excel).
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
        [FromQuery] string format = "csv")
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            // TODO: Implement report generation
            var csv = "Branch,OrderCount,TotalAmount\nSample Branch,10,5000000";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var fileName = $"branch_sales_{DateTime.UtcNow:yyyyMMdd}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception exception)
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