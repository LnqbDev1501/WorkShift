using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            model.ErrorMessage = "Unable to load shifts. Please try again later.";
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
            TempData["ErrorMessage"] = "Invalid shift ID.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        try
        {
            await api.RegisterShiftAsync(shiftId, ct);
            TempData["SuccessMessage"] = "Shift registration successful. The shift has been added to your schedule.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Shift registration failed: " + ex.Message;
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
            TempData["ErrorMessage"] = "Invalid shift ID.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please enter a reason for cancelling the shift registration.";
            return RedirectToAction(nameof(Index), new { tab, fromDate, toDate });
        }

        try
        {
            await api.CancelRegistrationAsync(shiftId, reason, ct);
            TempData["SuccessMessage"] = "Shift registration cancelled successfully.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to cancel registration: " + ex.Message;
        }

        return RedirectToAction(nameof(Index), new { tab = tab ?? "my-schedule", fromDate, toDate });
    }
}
