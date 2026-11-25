using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Controllers.Staff;

/// <summary>
/// Read-only product listing for staff (mobile app).
/// Returns ACTIVE products available in the staff's branch.
/// </summary>
[Route("api/staff/products")]
[ApiController]
[Authorize(Roles = "STAFF")]
public class StaffProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly AppDbContext _context;

    public StaffProductController(IProductService productService, AppDbContext context)
    {
        _productService = productService;
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductListQuery query)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var actorUser = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == actor.Value && u.Status == "ACTIVE" && u.DeletedAt == null)
                .Select(u => new { u.BranchId })
                .FirstOrDefaultAsync();

            int? staffBranchId = actorUser?.BranchId;
            query.BranchId = staffBranchId;

            var result = await _productService.GetPagedAsync(query, actor.Value);
            var filteredItems = result.Items.Where(p => p.Status == "ACTIVE").ToList();

            return Ok(new PagedResponse<ProductResponse>
            {
                Items = filteredItems,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalItems = filteredItems.Count
            });
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

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var actor = GetActorId();
        if (actor == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "The authenticated user ID is invalid.");

        try
        {
            var product = await _productService.GetByIdAsync(id, actor.Value);
            if (product == null || product.Status != "ACTIVE")
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "Product not found or not available in your branch."
                });
            }

            return Ok(product);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden, detail: exception.Message);
        }
    }

    private Guid? GetActorId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(subject, out var actor) && actor != Guid.Empty ? actor : null;
    }
}
