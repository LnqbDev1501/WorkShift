using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Accountant
{
    [ApiController]
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
                int periodId,
                [FromHeader(Name = "X-Accountant-Id")]
                string? accountantId)
        {
            if (string.IsNullOrWhiteSpace(accountantId) ||
                !Guid.TryParse(accountantId, out var accountantGuid))
            {
                return BadRequest(new { message = "X-Accountant-Id không hợp lệ." });
            }

            try
            {
                return Ok(await _service.SubmitPayrollPeriodAsync(periodId, accountantGuid));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        // Gửi lại một Payroll đã bị Admin reject.
        [HttpPost("{periodId:int}/payrolls/{payrollId:int}/submit")]
        public async Task<IActionResult>
            SubmitRejectedPayroll(
                int periodId,
                int payrollId,
                [FromHeader(Name = "X-Accountant-Id")]
                string? accountantId)
        {
            if (string.IsNullOrWhiteSpace(accountantId) ||
                !Guid.TryParse(
                    accountantId,
                    out var accountantGuid))
            {
                return BadRequest(new
                {
                    message =
                        "X-Accountant-Id không hợp lệ."
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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
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
