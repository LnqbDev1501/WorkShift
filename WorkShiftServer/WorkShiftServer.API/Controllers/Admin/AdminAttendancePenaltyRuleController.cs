using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/attendance-penalty-rules")]
public class AdminAttendancePenaltyRuleController : ControllerBase
{
    private readonly IAttendancePenaltyRuleService _attendancePenaltyRuleService;
    private readonly ILogger<AdminAttendancePenaltyRuleController> _logger;

    public AdminAttendancePenaltyRuleController(
        IAttendancePenaltyRuleService attendancePenaltyRuleService,
        ILogger<AdminAttendancePenaltyRuleController> logger)
    {
        _attendancePenaltyRuleService = attendancePenaltyRuleService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AttendancePenaltyRuleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttendancePenaltyRuleListResponse>> GetAttendancePenaltyRules(
        [FromQuery] GetAttendancePenaltyRulesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _attendancePenaltyRuleService.GetAttendancePenaltyRulesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while retrieving attendance penalty rules");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Attendance penalty rules could not be retrieved. Please try again later." });
        }
    }
}
