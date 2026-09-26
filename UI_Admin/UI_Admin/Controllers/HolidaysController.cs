using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Holidays;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class HolidaysController(HolidayApiService service, ILogger<HolidaysController> logger) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PublishRule(int id, CancellationToken cancellationToken) => TransitionRule(id, true, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> CancelRule(int id, CancellationToken cancellationToken) => TransitionRule(id, false, cancellationToken);

    private async Task<IActionResult> TransitionRule(int id, bool publish, CancellationToken ct)
    {
        try
        {
            var result = await service.TransitionRuleAsync(id, publish, ct);
            TempData[result.Success ? "HolidaySuccess" : "HolidayError"] = result.Success
                ? (publish ? "Rule published. Affected payroll periods require recalculation." : "Rule cancelled. Affected payroll periods require recalculation.") : result.Error;
        }
        catch (Exception ex) when (IsApiException(ex, ct))
        {
            logger.LogError(ex, "Unable to change holiday rule status.");
            TempData["HolidayError"] = "Unable to confirm the action. Please reload the list.";
        }
        return RedirectToAction(nameof(Rules));
    }
    [HttpGet]
    public async Task<IActionResult> Rules(CancellationToken cancellationToken)
    {
        try { return View(await service.GetRulesAsync(cancellationToken)); }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to load holiday rules.");
            ViewData["LoadError"] = "Unable to load pay rules. Please try again.";
            Response.StatusCode = 503;
            return View(new List<HolidayRuleViewModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Rule(int? id, CancellationToken cancellationToken)
    {
        if (!id.HasValue) return View(new HolidayRuleViewModel { CanEdit = true });
        try
        {
            var rule = (await service.GetRulesAsync(cancellationToken)).SingleOrDefault(r => r.HolidayPayRuleId == id);
            return rule == null ? NotFound() : View(rule);
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to load holiday rule.");
            TempData["HolidayError"] = "Unable to load pay rules.";
            return RedirectToAction(nameof(Rules));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Rule(HolidayRuleViewModel model, CancellationToken cancellationToken)
    {
        // This flag controls presentation only. The API enforces edit permissions.
        model.CanEdit = true;
        if (!ModelState.IsValid) return View(model);
        try
        {
            var result = await service.SaveRuleAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["HolidaySuccess"] = "Draft saved. Publish it when ready to apply the rule.";
                return RedirectToAction(nameof(Rules));
            }
            ModelState.AddModelError("", result.Error ?? "Unable to save the pay rule.");
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to save holiday rule.");
            ModelState.AddModelError("", "Unable to reach the server. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.DeleteRuleAsync(id, cancellationToken);
            TempData[result.Success ? "HolidaySuccess" : "HolidayError"] = result.Success ? "Draft deleted." : result.Error;
        }
        catch (Exception ex) when (IsApiException(ex, cancellationToken))
        {
            logger.LogError(ex, "Unable to delete holiday rule.");
            TempData["HolidayError"] = "Unable to confirm the action. Please reload the list.";
        }
        return RedirectToAction(nameof(Rules));
    }

    [HttpGet]
    public IActionResult Index() => RedirectToActionPermanent(nameof(List));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] HolidayListQueryViewModel query, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        if (query.Month.HasValue && !query.Year.HasValue)
            ModelState.AddModelError(nameof(query.Year), "Year is required when filtering by month.");

        var model = new HolidayIndexViewModel { Query = query };
        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "The selected holiday filters are invalid.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View("Index", model);
        }

        try
        {
            var listTask = service.GetPageAsync(query, cancellationToken);
            var summaryTask = service.GetAllAsync(query.Year, null, null, true, cancellationToken);
            await Task.WhenAll(listTask, summaryTask);
            model.Result = await listTask;
            model.SummaryItems = await summaryTask;

            if (query.PageNumber > Math.Max(1, model.Result.TotalPages))
                return RedirectToAction(nameof(List), new
                {
                    pageNumber = Math.Max(1, model.Result.TotalPages),
                    query.Year,
                    query.Month,
                    query.Search,
                    query.IncludeInactive,
                    query.IsActive
                });
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to load public holidays.");
            model.ErrorMessage = "Unable to load public holidays from the server. Please try again later.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        model.SuccessMessage = TempData["HolidaySuccess"] as string;
        model.ErrorMessage ??= TempData["HolidayError"] as string;
        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        try
        {
            var item = await service.GetByIdAsync(id, cancellationToken);
            if (item is null) return NotFound();
            return View(item);
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to load public holiday {HolidayId}.", id);
            TempData["HolidayError"] = "Unable to load that public holiday.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateHolidaysViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateHolidaysViewModel model, CancellationToken cancellationToken)
    {
        model.Holidays ??= [];
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await service.CreateAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["HolidaySuccess"] = $"Created {model.TotalDays} public holiday days successfully. Affected payroll periods must be recalculated before approval.";
                return RedirectToAction(nameof(List), new { year = model.Holidays[0].FromDate?.Year });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The public holidays could not be created.");
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to create public holidays.");
            ModelState.AddModelError(string.Empty, "Unable to reach the server. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        try
        {
            var item = await service.GetByIdAsync(id, cancellationToken);
            if (item is null) return NotFound();
            if (!item.IsActive || item.IsPayrollLocked) { TempData["HolidayError"] = "Inactive holidays and holidays used in payroll are read-only."; return RedirectToAction(nameof(Details), new { id }); }
            return View(new EditHolidayViewModel
            {
                PublicHolidayId = item.PublicHolidayId,
                HolidayName = item.HolidayName,
                HolidayDate = item.HolidayDate,
                IsActive = item.IsActive
            });
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to load public holiday {HolidayId} for editing.", id);
            TempData["HolidayError"] = "Unable to load that public holiday.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditHolidayViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.PublicHolidayId) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        try
        {
            var result = await service.UpdateAsync(id, model, cancellationToken);
            if (result.Success)
            {
                TempData["HolidaySuccess"] = $"Public holiday ‘{model.HolidayName.Trim()}’ updated successfully. Affected payroll periods have returned to draft and must be recalculated before approval.";
                return RedirectToAction(nameof(List), new { year = model.HolidayDate?.Year });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The public holiday could not be updated.");
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to update public holiday {HolidayId}.", id);
            ModelState.AddModelError(string.Empty, "Unable to reach the server. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest();
        try
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            TempData[result.Success ? "HolidaySuccess" : "HolidayError"] = result.Success
                ? "Public holiday deactivated successfully. Affected payroll periods must be recalculated before approval."
                : result.Error ?? "The public holiday could not be deactivated.";
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to deactivate public holiday {HolidayId}.", id);
            TempData["HolidayError"] = "Unable to reach the server. No change was confirmed.";
        }
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        if (id <= 0) return BadRequest();
        try
        {
            var result = await service.RestoreAsync(id, cancellationToken);
            TempData[result.Success ? "HolidaySuccess" : "HolidayError"] = result.Success
                ? "Public holiday restored successfully. Affected payroll periods must be recalculated before approval."
                : result.Error ?? "Unable to restore this holiday.";
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to restore holiday {HolidayId}.", id);
            TempData["HolidayError"] = "Unable to confirm restoration. Please check the list before retrying.";
        }
        return RedirectToAction(nameof(List), new { year = (int?)null, includeInactive = true });
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        [FromQuery] int? year, [FromQuery] int? month, [FromQuery] string? search,
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        try
        {
            var items = await service.GetAllAsync(year, month, search, includeInactive, cancellationToken);
            var csv = new StringBuilder("Holiday Name,Date,Pay Multiplier,Status\r\n");
            foreach (var item in items)
                csv.Append(Csv(item.HolidayName)).Append(',')
                    .Append(item.HolidayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                    .Append((item.PayMultiplier?.ToString("0.00", CultureInfo.InvariantCulture) ?? "No applicable rule")).Append(',')
                    .Append(item.IsActive ? "Active" : "Inactive").Append("\r\n");
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv", $"public-holidays-{year?.ToString() ?? "all"}.csv");
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to export public holidays.");
            TempData["HolidayError"] = "Unable to export public holidays right now.";
            return RedirectToAction(nameof(List), new { year, month, search, includeInactive });
        }
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static bool IsApiException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException or InvalidOperationException
        || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;
}



