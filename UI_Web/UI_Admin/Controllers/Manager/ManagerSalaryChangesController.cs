using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Controllers.SalaryChanges;
using UI_Admin.Models.SalaryChanges;
using UI_Admin.Services;
using UI_Admin.Services.Manager;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public sealed class ManagerSalaryChangesController(SalaryChangeApiService api, ManagerStaffApiService staff,
    ManagerEmployeeRequestApiService requests, ILogger<ManagerSalaryChangesController> logger) : SalaryChangeControllerBase(api, logger)
{
    private async Task LoadOptions(CreateSalaryChangeViewModel model, CancellationToken ct)
    {
        // Read every page so branches with more than 100 employees are not truncated.
        for (var page = 1; ; page++)
        {
            var result = await staff.GetStaffListAsync(null, "ACTIVE", page, 100, ct);
            model.StaffOptions.AddRange(result.Items);
            if (result.Items.Count == 0 || (long)page * 100 >= result.TotalCount) break;
        }
        model.StaffOptions = model.StaffOptions.DistinctBy(x => x.UserId).OrderBy(x => x.FullName).ToList();
        model.OptionsLoaded = true;
    }
    private async Task ValidateLinks(CreateSalaryChangeViewModel model, CancellationToken ct)
    {
        if (model.EmployeeRequestId.HasValue)
        {
            var request = await requests.GetByIdAsync(model.EmployeeRequestId.Value, ct);
            if (request == null || request.RequestType != "SALARY_INCREASE" || request.Status != "APPROVED" || request.UserId != model.UserId)
                ModelState.AddModelError("", "The employee request must be an approved salary increase request for this employee.");
        }
        if (model.PreviousRequestId.HasValue)
        {
            var previous = await Api.DetailAsync(model.PreviousRequestId.Value, ct);
            if (previous.UserId != model.UserId || previous.Status is not ("REJECTED" or "CANCELLED"))
                ModelState.AddModelError("", "Only a rejected or cancelled request for this employee can be resubmitted.");
        }
    }
    [HttpGet]
    public async Task<IActionResult> Create(int? employeeRequestId, int? previousRequestId, CancellationToken ct)
    {
        var model = new CreateSalaryChangeViewModel { EmployeeRequestId = employeeRequestId, PreviousRequestId = previousRequestId, ProposedEffectiveFrom = SalaryChangeDisplay.NextMonday };
        try
        {
            if (employeeRequestId.HasValue)
            {
                var request = await requests.GetByIdAsync(employeeRequestId.Value, ct);
                if (request == null || request.RequestType != "SALARY_INCREASE" || request.Status != "APPROVED") return BadRequest("An approved salary increase request is required.");
                model.UserId = request.UserId; model.Reason = request.Reason ?? "";
                model.ProposedHourlyRate = request.SalaryIncreaseDetail?.ProposedHourlyRate;
            }
            if (previousRequestId.HasValue)
            {
                var previous = await Api.DetailAsync(previousRequestId.Value, ct);
                if (previous.Status is not ("REJECTED" or "CANCELLED")) return BadRequest("Only rejected or cancelled requests can be resubmitted.");
                model.UserId = previous.UserId; model.Reason = previous.Reason; model.ProposedHourlyRate = previous.ProposedHourlyRate;
                model.EmployeeRequestId = previous.EmployeeRequestId;
            }
            await LoadOptions(model, ct);
            return View(ViewRoot + "Create.cshtml", model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSalaryChangeViewModel model, CancellationToken ct)
    {
        try
        {
            await LoadOptions(model, ct);
            if (!model.StaffOptions.Any(x => x.UserId == model.UserId)) ModelState.AddModelError(nameof(model.UserId), "Select an active employee in your branch.");
            if (ModelState.IsValid) await ValidateLinks(model, ct);
            if (ModelState.IsValid)
            {
                var result = await Api.CreateAsync(model, ct);
                TempData["SalaryChangeSuccess"] = "Salary increase request sent to Admin.";
                return RedirectToAction(nameof(Details), new { id = result.SalaryChangeRequestId });
            }
        }
        catch (SalaryChangeApiException ex) when (ex.Status is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden) { return Failure(ex); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Salary request creation failed."); ModelState.AddModelError("", Error(ex)); }
        return View(ViewRoot + "Create.cshtml", model);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id, [Bind(Prefix = "Decision")] SalaryChangeDecision decision, CancellationToken ct) => Decide(id, decision, "cancel", ct);
}
