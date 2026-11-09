using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.EmployeeAdjustments;

[Authorize(Roles = "MANAGER")]
public abstract class ManagerEmployeeAdjustmentControllerBase(EmployeeAdjustmentApiService api,
    ManagerStaffApiService staff, ILogger logger) : EmployeeAdjustmentControllerBase(api, logger)
{
    protected abstract string EntityLabel { get; }

    private async Task LoadOptions(SaveEmployeeAdjustmentViewModel model, CancellationToken ct)
    {
        model.StaffOptions.Clear();
        // Read every page so branches with more than 100 employees are not truncated.
        for (var page = 1; ; page++)
        {
            var result = await staff.GetStaffListAsync(null, "ACTIVE", page, 100, ct);
            model.StaffOptions.AddRange(result.Items);
            if (result.Items.Count == 0 || (long)page * 100 >= result.TotalCount) break;
        }
        model.StaffOptions = model.StaffOptions.DistinctBy(x => x.UserId).OrderBy(x => x.FullName).ToList();

        // Calculate dates in current week from Monday to today
        var nowVn = DateTime.UtcNow.AddHours(7);
        var today = DateOnly.FromDateTime(nowVn);
        int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        var monday = today.AddDays(-diff);

        model.AvailableDates = [];
        for (var d = monday; d <= today; d = d.AddDays(1))
        {
            var dayName = d.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ Hai",
                DayOfWeek.Tuesday => "Thứ Ba",
                DayOfWeek.Wednesday => "Thứ Tư",
                DayOfWeek.Thursday => "Thứ Năm",
                DayOfWeek.Friday => "Thứ Sáu",
                DayOfWeek.Saturday => "Thứ Bảy",
                DayOfWeek.Sunday => "Chủ Nhật",
                _ => ""
            };
            var label = $"{dayName}, {d:dd/MM/yyyy}" + (d == today ? " (Hôm nay)" : "");
            model.AvailableDates.Add(new DateOptionViewModel { Date = d, DisplayText = label });
        }

        if (!model.AdjustmentDate.HasValue)
        {
            model.AdjustmentDate = today;
        }

        try { model.PayrollPeriodOptions = await Api.GetPayrollPeriodsAsync(ct); }
        catch { model.PayrollPeriodOptions = []; }
        model.OptionsLoaded = true;
    }

    private void ValidateEmployee(SaveEmployeeAdjustmentViewModel model) =>
        ModelState.AddModelError(nameof(SaveEmployeeAdjustmentViewModel.UserId),
            "Select an active employee in your branch.");

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new SaveEmployeeAdjustmentViewModel { AdjustmentType = AdjustmentType };
        try { await LoadOptions(model, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
        return View(ViewRoot + "Create.cshtml", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaveEmployeeAdjustmentViewModel model, CancellationToken ct)
    {
        model.AdjustmentType = AdjustmentType;
        try
        {
            await LoadOptions(model, ct);
            if (!model.StaffOptions.Any(x => x.UserId == model.UserId)) ValidateEmployee(model);
            if (ModelState.IsValid)
            {
                var result = await Api.CreateAsync(AdjustmentType, model, ct);
                TempData["EmployeeAdjustmentSuccess"] = $"{EntityLabel} recorded and sent to Admin for approval.";
                return RedirectToAction(nameof(Details), new { id = result.RewardPenaltyId });
            }
        }
        catch (EmployeeAdjustmentApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { return Failure(ex); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Employee {Label} creation failed.", EntityLabel);
            ModelState.AddModelError("", Error(ex));
        }
        return View(ViewRoot + "Create.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var item = await Api.DetailAsync(AdjustmentType, id, ct);
            if (!item.CanUpdate)
            {
                TempData["EmployeeAdjustmentError"] = $"Only a pending {EntityLabel.ToLowerInvariant()} in your branch can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }
            var model = new SaveEmployeeAdjustmentViewModel
            {
                AdjustmentType = AdjustmentType,
                UserId = item.UserId,
                PayrollPeriodId = item.PayrollPeriodId,
                AdjustmentDate = item.AdjustmentDate,
                AdjustmentName = item.AdjustmentName,
                Amount = item.Amount,
                Reason = item.Reason
            };
            await LoadOptions(model, ct);
            return View(ViewRoot + "Edit.cshtml", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SaveEmployeeAdjustmentViewModel model, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        model.AdjustmentType = AdjustmentType;
        try
        {
            await LoadOptions(model, ct);
            if (!model.StaffOptions.Any(x => x.UserId == model.UserId)) ValidateEmployee(model);
            if (ModelState.IsValid)
            {
                await Api.UpdateAsync(AdjustmentType, id, model, ct);
                TempData["EmployeeAdjustmentSuccess"] = $"{EntityLabel} updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (EmployeeAdjustmentApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound) { return Failure(ex); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "Employee {Label} update failed for {Id}.", EntityLabel, id);
            ModelState.AddModelError("", Error(ex));
        }
        return View(ViewRoot + "Edit.cshtml", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            await Api.DeleteAsync(AdjustmentType, id, ct);
            TempData["EmployeeAdjustmentSuccess"] = $"{EntityLabel} deleted.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            TempData["EmployeeAdjustmentError"] = Error(ex);
        }
        return RedirectToAction(nameof(Index));
    }
}
