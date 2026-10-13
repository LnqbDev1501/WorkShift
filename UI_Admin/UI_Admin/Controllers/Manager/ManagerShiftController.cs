using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Manager;
using UI_Admin.Services.Manager;
namespace UI_Admin.Controllers.Manager;
[Authorize(Roles = "MANAGER")]
public sealed class ManagerShiftController(ManagerShiftApiService shifts, ManagerScheduleApiService schedules, ILogger<ManagerShiftController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = await LoadDetailAsync(id, ct);
        return View(model);
    }
    private async Task<ManagerShiftDetailViewModel> LoadDetailAsync(int id, CancellationToken ct, bool options = true)
    {
        var model = new ManagerShiftDetailViewModel { Id = id };
        try
        {
            model.Shift = await shifts.GetAsync(id, ct);
            model.Week = await schedules.GetDetailAsync(model.Shift.ScheduleWeekId, ct);
            if (options && model.CanAssign) model.Options = await shifts.OptionsAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { model.ErrorMessage = ManagerShiftApiService.Error(ex); }
        return model;
    }
    [HttpGet]
    public async Task<IActionResult> Create(int weekId, CancellationToken ct)
    {
        if (weekId <= 0) return NotFound();
        var model = new ManagerShiftEditViewModel { WeekId = weekId };
        await LoadEditAsync(model, ct);
        model.WorkDate = model.Week?.WeekStartDate;
        return View("Edit", model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var model = new ManagerShiftEditViewModel { Id = id };
        try
        {
            var shift = await shifts.GetAsync(id, ct);
            model.WeekId = shift.ScheduleWeekId; model.ShiftTemplateId = shift.ShiftTemplateId;
            model.WorkDate = DateOnly.FromDateTime(ScheduleDisplay.Vietnam(shift.ScheduledStartAt)); model.Note = shift.Note;
            model.EmployeeRequirements = shift.EmployeeRequirements.Select(x => new ShiftRequirementInput { EmployeeTypeId = x.EmployeeTypeId, RequiredCount = x.RequiredCount }).ToList();
            await LoadEditAsync(model, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { model.ErrorMessage = ManagerShiftApiService.Error(ex); }
        return View(model);
    }
    private async Task LoadEditAsync(ManagerShiftEditViewModel model, CancellationToken ct)
    {
        model.CanSave = false;
        try
        {
            if (model.Id is { } id)
            {
                var shift = await shifts.GetAsync(id, ct);
                model.WeekId = shift.ScheduleWeekId;
                if (shift.Status != "DRAFT" || shift.Staff.Count > 0) { model.ErrorMessage = "Only draft shifts without assignments can be edited."; return; }
            }
            model.Week = await schedules.GetDetailAsync(model.WeekId, ct);
            if (model.Week.Status != "DRAFT") { model.ErrorMessage = "Shifts can only be created or edited in a draft schedule."; return; }
            model.Options = await shifts.OptionsAsync(ct);
            model.CanSave = model.Options.Templates.Count > 0 && model.Options.EmployeeTypes.Count > 0;
            if (!model.CanSave) model.ErrorMessage = "Active shift templates and employee types are required. Contact your administrator.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { model.ErrorMessage = ManagerShiftApiService.Error(ex); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int weekId, [Bind("ShiftTemplateId,WorkDate,Note,EmployeeRequirements")] ManagerShiftEditViewModel model, CancellationToken ct)
    {
        if (weekId <= 0) return NotFound(); model.WeekId = weekId;
        return await SaveAsync(model, ct);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ShiftTemplateId,WorkDate,Note,EmployeeRequirements")] ManagerShiftEditViewModel model, CancellationToken ct)
    {
        if (id <= 0) return NotFound(); model.Id = id;
        return await SaveAsync(model, ct);
    }
    private async Task<IActionResult> SaveAsync(ManagerShiftEditViewModel model, CancellationToken ct)
    {
        await LoadEditAsync(model, ct);
        if (!model.CanSave) return View("Edit", model);
        if (model.WorkDate is { } date && model.Week is { } week && (date < week.WeekStartDate || date > week.WeekEndDate)) ModelState.AddModelError(nameof(model.WorkDate), "Choose a date within the schedule week.");
        if (!model.Options.Templates.Any(x => x.Id == model.ShiftTemplateId)) ModelState.AddModelError(nameof(model.ShiftTemplateId), "Choose an active shift template.");
        if (model.EmployeeRequirements is not { Count: > 0 }) ModelState.AddModelError("", "Add at least one employee requirement.");
        else
        {
            if (model.EmployeeRequirements.Select(x => x.EmployeeTypeId).Distinct().Count() != model.EmployeeRequirements.Count) ModelState.AddModelError("", "Select each employee type only once.");
            if (model.EmployeeRequirements.Any(x => !model.Options.EmployeeTypes.Any(t => t.Id == x.EmployeeTypeId))) ModelState.AddModelError("", "Choose active employee types.");
        }
        if (!ModelState.IsValid) return View("Edit", model);
        try
        {
            var saved = await shifts.SaveAsync(model, ct);
            TempData["ShiftSuccess"] = model.Id.HasValue ? "Shift updated successfully." : "Shift created successfully.";
            return RedirectToAction(nameof(Details), new { id = saved.ShiftId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { logger.LogWarning(ex, "Unable to save shift."); model.ErrorMessage = ManagerShiftApiService.Error(ex); return View("Edit", model); }
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    { if (id <= 0) return NotFound(); return View(await LoadDetailAsync(id, ct, false)); }
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound(); var model = await LoadDetailAsync(id, ct, false);
        if (model.ErrorMessage is not null) return View("Delete", model);
        if (!model.CanEdit) { model.ErrorMessage = "Only draft shifts in draft schedules without assignments can be deleted."; return View("Delete", model); }
        try { await shifts.DeleteAsync(id, ct); TempData["ScheduleSuccess"] = "Shift deleted successfully."; return RedirectToAction("Details", "ManagerSchedule", new { id = model.Shift!.ScheduleWeekId }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { model.ErrorMessage = ManagerShiftApiService.Error(ex); return View("Delete", model); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, Guid userId, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            if (userId == Guid.Empty) { TempData["ShiftError"] = "Select an employee."; }
            else { await shifts.AssignAsync(id, userId, ct); TempData["ShiftSuccess"] = "Employee assigned successfully."; }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { TempData["ShiftError"] = ManagerShiftApiService.Error(ex); }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet]
    public async Task<IActionResult> CancelAssignment(int id, int assignmentId, CancellationToken ct)
    {
        if (id <= 0 || assignmentId <= 0) return NotFound();
        var model = await LoadDetailAsync(id, ct, false);
        if (model.Shift is not null && !model.Shift.Staff.Any(x => x.AssignmentId == assignmentId)) return NotFound();
        ViewData["AssignmentId"] = assignmentId;
        return View(model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAssignment(int id, int assignmentId, ShiftCancelInput input, CancellationToken ct)
    {
        if (id <= 0 || assignmentId <= 0) return NotFound();
        var model = await LoadDetailAsync(id, ct, false); ViewData["AssignmentId"] = assignmentId;
        var assignment = model.Shift?.Staff.SingleOrDefault(x => x.AssignmentId == assignmentId);
        if (model.Shift is not null && assignment is null) return NotFound();
        if (model.ErrorMessage is not null || !ModelState.IsValid) return View(model);
        if (!model.CanChangeAssignments || assignment?.Status != "ASSIGNED") { model.ErrorMessage = "This assignment cannot be cancelled in its current state."; return View(model); }
        try { await shifts.CancelAsync(assignmentId, input.Reason, ct); TempData["ShiftSuccess"] = "Assignment cancelled successfully."; return RedirectToAction(nameof(Details), new { id }); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ManagerShiftApiService.IsFailure(ex)) { model.ErrorMessage = ManagerShiftApiService.Error(ex); return View(model); }
    }
}
