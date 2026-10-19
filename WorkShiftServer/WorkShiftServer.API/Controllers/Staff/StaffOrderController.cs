using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Staff;

/// <summary>
/// Order management for staff (mobile app).
/// Staff can create orders, view their orders, complete orders.
/// </summary>
[Route("api/staff/orders")]
[ApiController]
[Authorize(Roles = "STAFF")]
public class StaffOrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public StaffOrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }

    /// <summary>
    /// Creates a new customer order.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var result = await _orderService.CreateAsync(request, actor.Value);
            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Gets a paginated list of orders created by the staff member.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderListQuery query)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            // Staff only sees their own orders - filter by CreatedBy
            // We'll add a CreatedBy filter to the query
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
    /// Completes an order (marks as COMPLETED).
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteOrder(int id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var completed = await _orderService.UpdateStatusAsync(id, "COMPLETED", actor.Value);
            if (!completed)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Order not found."
                });
            }

            return Ok(new { message = "Order completed successfully." });
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    /// <summary>
    /// Cancels an order (marks as CANCELLED).
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var cancelled = await _orderService.UpdateStatusAsync(id, "CANCELLED", actor.Value);
            if (!cancelled)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Order not found."
                });
            }

            return Ok(new { message = "Order cancelled successfully." });
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
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = exception.Message
            });
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }
}