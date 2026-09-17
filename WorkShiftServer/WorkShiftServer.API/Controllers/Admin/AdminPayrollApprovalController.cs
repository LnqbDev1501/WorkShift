using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Controllers.Admin
{
[ApiController]
[Authorize(Policy = "Admin")]
    [Route("api/admin/payroll-periods")]
    public class AdminPayrollApprovalController
        : ControllerBase
    {
        private readonly IPayrollApprovalService
            _service;

        public AdminPayrollApprovalController(
            IPayrollApprovalService service)
        {
            _service = service;
        }

        // =====================================================
        // GET /api/admin/payroll-periods
        // =====================================================
        [HttpGet]
        public async Task<IActionResult>
            GetPayrollPeriods(
                [FromQuery] string? status = null,
                [FromQuery] int? regionId = null,
                [FromQuery] int? branchId = null,
                [FromQuery] string? search = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 20)
        {
            try
            {
                var result =
                    await _service
                        .GetPayrollPeriodsAsync(
                            status,
                            regionId,
                            branchId,
                            search,
                            pageNumber,
                            pageSize);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // GET /api/admin/payroll-periods/{id}
        // =====================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult>
            GetPayrollPeriodDetail(
                int id,
                [FromQuery] string? search = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 20)
        {
            try
            {
                var result =
                    await _service
                        .GetPayrollPeriodDetailAsync(
                            id,
                            search,
                            pageNumber,
                            pageSize);

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
        }

        // =====================================================
        // POST
        // /api/admin/payroll-periods/{periodId}
        // /payrolls/{payrollId}/reject
        // =====================================================
        [HttpPost(
            "{periodId:int}/payrolls/{payrollId:int}/reject")]
        public async Task<IActionResult>
            RejectPayroll(
                int periodId,
                int payrollId,
                [FromBody] RejectPayrollRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var adminGuid))
            {
                return Unauthorized(new
                {
                    message = "The authenticated Admin ID is invalid."
                });
            }

            try
            {
                var result =
                    await _service.RejectPayrollAsync(
                        periodId,
                        payrollId,
                        request,
                        adminGuid);

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

        // =====================================================
        // POST /api/admin/payroll-periods/{id}/approve
        // =====================================================
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult>
            ApprovePayrollPeriod(
                int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(userId, out var adminGuid))
            {
                return Unauthorized(new
                {
                    message = "The authenticated Admin ID is invalid."
                });
            }

            try
            {
                var result =
                    await _service
                        .ApprovePayrollPeriodAsync(
                            id,
                            adminGuid);

                return Ok(result);
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
