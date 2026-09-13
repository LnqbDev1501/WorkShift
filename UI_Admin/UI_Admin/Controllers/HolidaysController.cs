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
    [HttpGet]
    public async Task<IActionResult> Index(
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
            return View(model);
        }

        try
        {
            var listTask = service.GetPageAsync(query, cancellationToken);
            var summaryTask = service.GetAllAsync(query.Year, null, null, true, cancellationToken);
            await Task.WhenAll(listTask, summaryTask);
            model.Result = await listTask;
            model.SummaryItems = await summaryTask;

            if (query.PageNumber > Math.Max(1, model.Result.TotalPages))
                return RedirectToAction(nameof(Index), new
                {
                    pageNumber = Math.Max(1, model.Result.TotalPages), query.Year, query.Month,
                    query.Search, query.IncludeInactive
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
        return View(model);
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
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateHolidaysViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateHolidaysViewModel model, CancellationToken cancellationToken)
    {
        model.Holidays ??= [];
        var duplicateDates = model.Holidays.Where(item => item.HolidayDate.HasValue)
            .GroupBy(item => item.HolidayDate).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        if (duplicateDates.Count > 0)
            ModelState.AddModelError(string.Empty, "Each holiday in the batch must have a unique date.");
        if (!ModelState.IsValid) return View(model);

        try
        {
            var result = await service.CreateAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["HolidaySuccess"] = $"Created {model.Holidays.Count} public holiday{(model.Holidays.Count == 1 ? string.Empty : "s")} successfully.";
                return RedirectToAction(nameof(Index), new { year = model.Holidays[0].HolidayDate?.Year });
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
            if (!item.IsActive)
            {
                TempData["HolidayError"] = "Inactive public holidays are read-only.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return View(new EditHolidayViewModel
            {
                PublicHolidayId = item.PublicHolidayId,
                HolidayName = item.HolidayName,
                HolidayDate = item.HolidayDate,
                PayMultiplier = item.PayMultiplier,
                IsActive = item.IsActive
            });
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to load public holiday {HolidayId} for editing.", id);
            TempData["HolidayError"] = "Unable to load that public holiday.";
            return RedirectToAction(nameof(Index));
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
                TempData["HolidaySuccess"] = $"Public holiday ‘{model.HolidayName.Trim()}’ updated successfully.";
                return RedirectToAction(nameof(Index), new { year = model.HolidayDate?.Year });
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
                ? "Public holiday deactivated successfully. Its date remains reserved for audit history."
                : result.Error ?? "The public holiday could not be deactivated.";
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to deactivate public holiday {HolidayId}.", id);
            TempData["HolidayError"] = "Unable to reach the server. No change was confirmed.";
        }
        return RedirectToAction(nameof(Index));
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
                    .Append(item.PayMultiplier.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
                    .Append(item.IsActive ? "Active" : "Inactive").Append("\r\n");
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv", $"public-holidays-{year?.ToString() ?? "all"}.csv");
        }
        catch (Exception exception) when (IsApiException(exception, cancellationToken))
        {
            logger.LogError(exception, "Unable to export public holidays.");
            TempData["HolidayError"] = "Unable to export public holidays right now.";
            return RedirectToAction(nameof(Index), new { year, month, search, includeInactive });
        }
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static bool IsApiException(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or JsonException or InvalidOperationException
        || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested;
}
