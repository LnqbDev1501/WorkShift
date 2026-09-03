using System.Net;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
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
}
