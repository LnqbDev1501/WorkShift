using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerScheduleController(ManagerScheduleApiService schedules, ILogger<ManagerScheduleController> logger) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OpenRegistration(int id, CancellationToken ct) => await RegistrationAsync(id, true, ct);
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseRegistration(int id, CancellationToken ct) => await RegistrationAsync(id, false, ct);
    private async Task<IActionResult> RegistrationAsync(int id, bool open, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            await schedules.RegistrationAsync(id, open, ct);
            TempData["ScheduleSuccess"] = open ? "Shift registration opened successfully." : "Shift registration closed successfully.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsApiFailure(ex)) { TempData["ScheduleError"] = ReadFailure(ex); }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet]
    public IActionResult Create() => View("Edit", new ManagerScheduleEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind("WeekStartDate,RegistrationDeadline")] ManagerScheduleEditViewModel model, CancellationToken ct) => SaveAsync(model, ct);

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = new ManagerScheduleEditViewModel { Id = id, CanSave = false };
        try
        {
            var week = await schedules.GetDetailAsync(id, ct);
            model.WeekStartDate = week.WeekStartDate;
            model.RegistrationDeadline = ScheduleDisplay.Vietnam(week.RegistrationDeadline);
            model.LockStartDate = week.Shifts.Count > 0;
            model.CanSave = week.Status == "DRAFT";
            if (!model.CanSave) model.ErrorMessage = "Only draft schedules can be updated.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsApiFailure(ex)) { model.ErrorMessage = ReadFailure(ex); }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind("WeekStartDate,RegistrationDeadline")] ManagerScheduleEditViewModel model, CancellationToken ct)
    {
        if (id <= 0) return Task.FromResult<IActionResult>(NotFound());
        model.Id = id;
        return SaveAsync(model, ct);
    }

    private async Task<IActionResult> SaveAsync(ManagerScheduleEditViewModel model, CancellationToken ct)
    {
        if (model.Id is { } id)
        {
            try
            {
                var current = await schedules.GetDetailAsync(id, ct);
                model.CanSave = current.Status == "DRAFT";
                model.LockStartDate = current.Shifts.Count > 0;
                if (!model.CanSave) ModelState.AddModelError("", "Only draft schedules can be updated.");
                if (model.LockStartDate && model.WeekStartDate != current.WeekStartDate)
                    ModelState.AddModelError(nameof(model.WeekStartDate), "The start date cannot change after shifts have been created.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (IsApiFailure(ex))
            {
                model.CanSave = false;
                model.ErrorMessage = ReadFailure(ex);
                return View("Edit", model);
            }
        }
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (model.WeekStartDate is { } start)
        {
            if (start.DayOfWeek != DayOfWeek.Monday) ModelState.AddModelError(nameof(model.WeekStartDate), "Choose a Monday.");
            if (start <= today) ModelState.AddModelError(nameof(model.WeekStartDate), "The week must start in the future.");
        }
        if (model.RegistrationDeadline is { } deadline)
        {
            if (deadline <= DateTime.UtcNow.AddHours(7)) ModelState.AddModelError(nameof(model.RegistrationDeadline), "The deadline must be in the future.");
            if (model.WeekStartDate is { } date && DateOnly.FromDateTime(deadline) >= date)
                ModelState.AddModelError(nameof(model.RegistrationDeadline), "The deadline must be before the week starts.");
        }
        if (!ModelState.IsValid) return View("Edit", model);
        try
        {
            var saved = await schedules.SaveAsync(model, ct);
            TempData["ScheduleSuccess"] = model.Id.HasValue ? "Weekly schedule updated successfully." : "Weekly schedule created successfully.";
            return RedirectToAction(nameof(Details), new { id = saved.ScheduleWeekId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsApiFailure(ex))
        {
            logger.LogWarning(ex, "Unable to save weekly schedule.");
            model.ErrorMessage = ReadFailure(ex);
            return View("Edit", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = new ManagerScheduleDetailViewModel { Id = id };
        try { model.Week = await schedules.GetDetailAsync(id, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsApiFailure(ex)) { model.ErrorMessage = ReadFailure(ex); }
        return View(model);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = new ManagerScheduleDetailViewModel { Id = id };
        try
        {
            model.Week = await schedules.GetDetailAsync(id, ct);
            if (model.Week.Status != "DRAFT")
            {
                model.ErrorMessage = "Only draft schedules can be deleted.";
                return View("Delete", model);
            }
            await schedules.DeleteAsync(id, ct);
            TempData["ScheduleSuccess"] = "Weekly schedule deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (IsApiFailure(ex)) { model.ErrorMessage = ReadFailure(ex); return View("Delete", model); }
    }

    private static bool IsApiFailure(Exception ex) => ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException;
    private static string ReadFailure(Exception ex) => ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict } ? ex.Message
        : ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ? "This schedule could not be found."
        : ex is HttpRequestException { StatusCode: HttpStatusCode.Forbidden } ? "You do not have access to this schedule."
        : "The operation could not be confirmed. Check the schedule list before trying again.";

    [HttpGet]
    public async Task<IActionResult> Index(ManagerScheduleFilter filter, CancellationToken ct)
    {
        var model = new ManagerScheduleListViewModel { Filter = filter };
        if (filter.FromDate > filter.ToDate) ModelState.AddModelError("", "From date cannot be later than to date.");
        if (!string.IsNullOrEmpty(filter.Status) && !ScheduleDisplay.Statuses.Contains(filter.Status))
            ModelState.AddModelError(nameof(filter.Status), "Select a valid schedule status.");
        if (!ModelState.IsValid) return View(model);
        try { model.Weeks = await schedules.GetListAsync(filter, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Unable to load weekly schedules.");
            model.ErrorMessage = "Unable to load weekly schedules. Please try again shortly.";
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = new ManagerScheduleDetailViewModel { Id = id };
        try { model.Week = await schedules.GetDetailAsync(id, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Unable to load schedule {WeekId}.", id);
            model.ErrorMessage = ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound }
                ? "This weekly schedule could not be found."
                : ex is HttpRequestException { StatusCode: HttpStatusCode.Forbidden }
                    ? "You do not have access to this weekly schedule."
                    : "Unable to load schedule details. Please try again shortly.";
        }
        return View(model);
    }
}
