using WorkShiftManagement.BLL.Services.Accountant;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Accountant;

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
            IPayrollApprovalService service, IPayrollCalculationRepo actors)
        {
            _service = service;
            _actors = actors;
        }

        private readonly IPayrollCalculationRepo _actors;
        private async Task<bool> ActiveAdmin()
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)) return false;
            var u = await _actors.GetActorAsync(id, HttpContext.RequestAborted);
            return u != null && u.Status == "ACTIVE" && u.DeletedAt == null && u.Role.Status == "ACTIVE" && u.Role.RoleName == "ADMIN";
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
                [FromQuery] int pageSize = 20,
                [FromQuery] DateOnly? startDateFrom = null,
                [FromQuery] DateOnly? startDateTo = null)
        {
            if (!await ActiveAdmin()) return Forbid();
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
                            pageSize,
                            startDateFrom,
                            startDateTo);

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
            if (!await ActiveAdmin()) return Forbid();
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
        [NonAction]
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
