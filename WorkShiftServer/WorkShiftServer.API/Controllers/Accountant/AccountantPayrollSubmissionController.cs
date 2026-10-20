using WorkShiftManagement.BLL.Services.Accountant;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Accountant
{
    [ApiController]
    [Authorize(Roles = "ACCOUNTANT")]
    [Route("api/accountant/payroll-periods")]
    public class AccountantPayrollSubmissionController
        : ControllerBase
    {
        private readonly IPayrollApprovalService
            _service;

        public AccountantPayrollSubmissionController(
            IPayrollApprovalService service)
        {
            _service = service;
        }

        // =====================================================
        // POST
        // /api/accountant/payroll-periods/{periodId}/submit
        // =====================================================
        [HttpPost("{periodId:int}/submit")]
        public async Task<IActionResult>
            SubmitPayrollPeriod(
                int periodId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var accountantGuid))
            {
                return Unauthorized(new { message = "The authenticated Accountant ID is invalid." });
            }

            try
            {
                return Ok(await _service.SubmitPayrollPeriodAsync(periodId, accountantGuid));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { message = ex.Message }); }
            catch (ArgumentException ex)
            { return BadRequest(new { message = ex.Message }); }
            catch (PayrollSourceChangedException ex)
            { return Conflict(new { message = ex.Message, sourceImpact = ex.SourceImpact }); }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // Gửi lại một Payroll đã bị Admin reject.
        [NonAction]
        public async Task<IActionResult>
            SubmitRejectedPayroll(
                int periodId,
                int payrollId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var accountantGuid))
            {
                return Unauthorized(new
                {
                    message = "The authenticated user ID is invalid."
                });
            }

            try
            {
                var result =
                    await _service
                        .SubmitRejectedPayrollAsync(
                            periodId,
                            payrollId,
                            accountantGuid);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { message = ex.Message }); }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (PayrollSourceChangedException ex)
            { return Conflict(new { message = ex.Message, sourceImpact = ex.SourceImpact }); }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
