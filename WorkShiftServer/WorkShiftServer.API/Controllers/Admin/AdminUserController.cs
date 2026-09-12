using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/users")]
public class AdminUserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AdminUserController> _logger;

    public AdminUserController(IUserService userService, ILogger<AdminUserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminUserListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminUserListResponse>> GetAdminUsers(
        [FromQuery] GetAdminUsersRequest request)
    {
        try
        {
            return Ok(await _userService.GetAdminUsersAsync(request));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Users could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> GetAssignableRoles()
    {
        try
        {
            return Ok(await _userService.GetAssignableRolesAsync());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving assignable roles");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Roles could not be retrieved. Please try again later." });
        }
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminUserDetailResponse>> GetAdminUserDetail(Guid userId)
    {
        try
        {
            return Ok(await _userService.GetAdminUserDetailAsync(userId));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The user could not be retrieved. Please try again later." });
        }
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(typeof(UpdateAdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateAdminUserResponse>> UpdateAdminUser(
        Guid userId,
        [FromForm] UpdateAdminUserRequest request)
    {
        try
        {
            return Ok(await _userService.UpdateAdminUserAsync(userId, request));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while updating user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The account could not be updated. Please try again later." });
        }
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SoftDeleteAdminUser(Guid userId)
    {
        try
        {
            await _userService.SoftDeleteAdminUserAsync(userId);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while deleting user {UserId}", userId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while deleting user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The account could not be deleted. Please try again later." });
        }
    }

    [HttpPost("{userId:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreAdminUser(
        Guid userId,
        [FromBody] RestoreAdminUserRequest request)
    {
        try
        {
            await _userService.RestoreAdminUserAsync(userId, request);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while restoring user {UserId}", userId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while restoring user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The account could not be restored. Please try again later." });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateAdminUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateAdminUserResponse>> CreateAdminUser(
        [FromBody] CreateAdminUserRequest request)
    {
        try
        {
            var result = await _userService.CreateAdminUserAsync(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            return Conflict(new { message = "Email already exists or cannot be used." });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while creating a user");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
        catch (InvalidDataException exception)
        {
            _logger.LogError(exception, "Supabase Auth returned invalid data");
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service returned invalid data." });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while creating a user");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The account could not be created. Please try again later." });
        }
    }

    [HttpPost("{userId:guid}/resend-setup-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendSetupLink(Guid userId)
    {
        try
        {
            await _userService.ResendSetupLinkAsync(userId);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Please wait before sending another invitation." });
        }
        catch (HttpRequestException exception)
            when (exception.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
        {
            return Conflict(new { message = "The account setup link cannot be sent for this Auth account." });
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Supabase Auth failed while resending an account setup link for {UserId}", userId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service is temporarily unavailable." });
        }
        catch (InvalidDataException exception)
        {
            _logger.LogError(exception, "Supabase Auth returned invalid data while resending an account setup link for {UserId}", userId);
            return StatusCode(StatusCodes.Status502BadGateway,
                new { message = "Authentication service returned invalid data." });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while resending an account setup link for {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "The account setup link could not be sent. Please try again later." });
        }
    }
}
