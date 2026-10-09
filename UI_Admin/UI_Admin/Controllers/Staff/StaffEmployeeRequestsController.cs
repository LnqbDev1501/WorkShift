using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UI_Admin.Models.Staff.Requests;
using UI_Admin.Services.Staff;

namespace UI_Admin.Controllers.Staff;

[Authorize(Roles = "STAFF")]
public class StaffEmployeeRequestsController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly StaffEmployeeRequestApiService _api;
    private readonly ILogger<StaffEmployeeRequestsController> _logger;

    public StaffEmployeeRequestsController(
        StaffEmployeeRequestApiService api,
        ILogger<StaffEmployeeRequestsController> logger)
    {
        _api = api;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? reason,
        string? requestType,
        string? status,
        string? fromDate,
        string? toDate,
        int page = 1,
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        var targetPage = Math.Max(1, page > 1 ? page : pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        // Validate date range
        if (!string.IsNullOrWhiteSpace(fromDate) && !string.IsNullOrWhiteSpace(toDate))
        {
            if (DateOnly.TryParse(fromDate, out var from) && DateOnly.TryParse(toDate, out var to))
            {
                if (from > to)
                {
                    ViewBag.ErrorMessage = "Khoảng ngày không hợp lệ: 'Từ ngày' phải trước hoặc cùng ngày với 'Đến ngày'.";
                }
            }
        }

        try
        {
            var viewModel = await _api.GetPagedAsync(
                reason: reason,
                requestType: requestType,
                status: status,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: targetPage,
                pageSize: pageSize,
                cancellationToken: ct);

            if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
            {
                ViewBag.ErrorMessage = viewModel.ErrorMessage;
            }

            return View(viewModel);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load employee requests for staff.");
            ViewBag.ErrorMessage = "Không thể tải danh sách đơn của bạn. Vui lòng thử lại sau.";
            return View(new StaffEmployeeRequestListViewModel
            {
                PageNumber = targetPage,
                PageSize = pageSize,
                Reason = reason,
                RequestType = requestType,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? returnUrl, CancellationToken ct = default)
    {
        if (id <= 0) return NotFound();

        if (TempData["SuccessMessage"] is string success)
            ViewBag.SuccessMessage = success;
        if (TempData["ErrorMessage"] is string error)
            ViewBag.ErrorMessage = error;

        try
        {
            var detail = await _api.GetByIdAsync(id, ct);
            if (detail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn yêu cầu hoặc bạn không có quyền xem.";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                detail.ReturnUrl = returnUrl;
            }

            return View(detail);
        }
        catch (UnauthorizedAccessException)
        {
            return RedirectToAction("Login", "Auth");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load request detail #{RequestId} for staff", id);
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? type = "LEAVE", CancellationToken ct = default)
    {
        var normType = (type?.ToUpperInvariant() is "LATE_ARRIVAL" or "EARLY_LEAVE") ? type.ToUpperInvariant() : "LEAVE";
        ViewBag.RequestType = normType;

        var eligibleShifts = await _api.GetEligibleShiftsAsync(normType, ct);
        ViewBag.EligibleShifts = eligibleShifts;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLeave(List<int> assignmentIds, string reason, CancellationToken ct = default)
    {
        if (assignmentIds == null || !assignmentIds.Any())
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ca làm việc để xin nghỉ phép.";
            return RedirectToAction(nameof(Create), new { type = "LEAVE" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do xin nghỉ phép.";
            return RedirectToAction(nameof(Create), new { type = "LEAVE" });
        }

        var (success, errorMsg) = await _api.SubmitLeaveAsync(assignmentIds, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Gửi đơn xin nghỉ phép thành công! Đang chờ Quản lý chi nhánh xét duyệt.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Gửi đơn xin nghỉ phép thất bại.";
        return RedirectToAction(nameof(Create), new { type = "LEAVE" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateLateArrival(int assignmentId, DateTime requestedTime, string reason, CancellationToken ct = default)
    {
        if (assignmentId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ca làm việc cần xin đi trễ.";
            return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do xin đi trễ.";
            return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
        }

        var dto = new DateTimeOffset(DateTime.SpecifyKind(requestedTime, DateTimeKind.Local));
        var (success, errorMsg) = await _api.SubmitLateArrivalAsync(assignmentId, dto, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Gửi đơn xin đi trễ thành công! Đang chờ Quản lý chi nhánh xét duyệt.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Gửi đơn xin đi trễ thất bại.";
        return RedirectToAction(nameof(Create), new { type = "LATE_ARRIVAL" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEarlyLeave(int assignmentId, DateTime requestedTime, string reason, CancellationToken ct = default)
    {
        if (assignmentId <= 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn ca làm việc cần xin về sớm.";
            return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do xin về sớm.";
            return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
        }

        var dto = new DateTimeOffset(DateTime.SpecifyKind(requestedTime, DateTimeKind.Local));
        var (success, errorMsg) = await _api.SubmitEarlyLeaveAsync(assignmentId, dto, reason, ct);
        if (success)
        {
            TempData["SuccessMessage"] = "Gửi đơn xin về sớm thành công! Đang chờ Quản lý chi nhánh xét duyệt.";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = errorMsg ?? "Gửi đơn xin về sớm thất bại.";
        return RedirectToAction(nameof(Create), new { type = "EARLY_LEAVE" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl, CancellationToken ct = default)
    {
        if (id <= 0) return NotFound();

        var (success, errorMsg) = await _api.CancelAsync(id, ct);
        if (success)
        {
            TempData["SuccessMessage"] = $"Đã hủy đơn #{id} thành công.";
        }
        else
        {
            TempData["ErrorMessage"] = errorMsg ?? "Hủy đơn yêu cầu thất bại.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
