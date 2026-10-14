using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Accountant;

/// <summary>
/// Order and payment management for accountants (system-wide).
/// </summary>
[Route("api/accountant/orders")]
[ApiController]
[Authorize(Roles = "ACCOUNTANT,ADMIN")]
public class AccountantOrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AccountantOrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }

    /// <summary>
    /// Gets a paginated list of all orders (system-wide).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders([FromQuery] OrderListQuery query)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            return Ok(await _orderService.GetPagedAsync(query, actor.Value));
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
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Gets details of a specific order.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(int id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var order = await _orderService.GetByIdAsync(id, actor.Value);
            if (order == null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Order not found."
                });
            }

            return Ok(order);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Gets invoice PDF for an order.
    /// </summary>
    [HttpGet("{id:int}/invoice")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(int id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var pdf = await _orderService.GenerateInvoicePdfAsync(id, actor.Value);
            if (pdf == null || pdf.Length == 0)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Invoice not found."
                });
            }

            return File(pdf, "application/pdf", $"invoice_{id}.pdf");
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }
}