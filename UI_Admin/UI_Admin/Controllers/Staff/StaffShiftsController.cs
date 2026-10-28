using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Staff.Shifts;
using UI_Admin.Services.Staff;

namespace UI_Admin.Controllers.Staff;

[Authorize(Roles = "STAFF")]
public class StaffShiftsController(StaffShiftApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? fromDate,
        DateOnly? toDate,
        string tab = "available",
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var effectiveFrom = fromDate ?? today;
        var effectiveTo = toDate ?? today.AddDays(14);

        var model = new StaffShiftListViewModel
        {
            FromDate = effectiveFrom,
            ToDate = effectiveTo,
            ActiveTab = string.Equals(tab, "my-schedule", StringComparison.OrdinalIgnoreCase) ? "my-schedule" : "available"
        };

        try
        {
            // Load both available and my-schedule so staff can switch tabs seamlessly
            model.AvailableShifts = await api.GetAvailableShiftsAsync(effectiveFrom, effectiveTo, ct: ct);
            model.MySchedule = await api.GetMyScheduleAsync(effectiveFrom, effectiveTo, ct: ct);
        }
        catch (HttpRequestException ex)
        {
            model.ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            model.ErrorMessage = "Không thể tải danh sách ca làm việc. Vui lòng thử lại sau.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        int shiftId,
        string? tab,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        if (shiftId <= 0)
        {
            TempData["ErrorMessage"] = "Mã ca làm không hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        try
        {
            await api.RegisterShiftAsync(shiftId, ct);
            TempData["SuccessMessage"] = "Đăng ký ca làm thành công! Ca làm đã được thêm vào lịch làm của bạn.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Đăng ký ca làm thất bại: " + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = tab ?? "available", fromDate, toDate });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int shiftId,
        string reason,
        string? tab,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        if (shiftId <= 0)
        {
            TempData["ErrorMessage"] = "Mã ca làm không hợp lệ.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy đăng ký ca làm.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        try
        {
            await api.CancelRegistrationAsync(shiftId, reason, ct);
            TempData["SuccessMessage"] = "Đã hủy đăng ký ca làm thành công.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Hủy đăng ký thất bại: " + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = tab ?? "my-schedule", fromDate, toDate });
    }
}
