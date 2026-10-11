using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant.SalaryHistory;
using UI_Admin.Services.Accountant;
namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT")]
public sealed class AccountantSalaryHistoryController(SalaryHistoryApiService api, ILogger<AccountantSalaryHistoryController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] SalaryHistoryQuery query, CancellationToken ct)
    {
        query.Page = Math.Clamp(query.Page, 1, int.MaxValue / 100);
        query.Status = query.Status?.Trim().ToUpperInvariant(); query.ChangeType = query.ChangeType?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(query.Status) && query.Status is not ("SCHEDULED" or "ACTIVE" or "INACTIVE")) ModelState.AddModelError("Status", "Choose a valid status.");
        if (!string.IsNullOrEmpty(query.ChangeType) && query.ChangeType is not ("INITIAL" or "INCREASE" or "DECREASE")) ModelState.AddModelError("ChangeType", "Choose a valid change type.");
        if (query.UserId == Guid.Empty) ModelState.AddModelError("UserId", "Enter a valid Staff ID.");
        if (query.EffectiveFrom > query.EffectiveTo) ModelState.AddModelError("EffectiveTo", "End date must be on or after the start date.");
        var model = new SalaryHistoryListViewModel { Query = query, PageNumber = query.Page };
        if (!ModelState.IsValid) return View(model);
        try
        {
            model = await api.GetListAsync(query, ct);
            var max = Math.Max(1, (int)Math.Ceiling(model.TotalItems / (double)model.PageSize));
            if (query.Page > max) { query.Page = max; model = await api.GetListAsync(query, ct); }
        }
        catch (SalaryHistoryApiException ex)
        {
            if (ex.Status == HttpStatusCode.Unauthorized) return Challenge();
            if (ex.Status == HttpStatusCode.Forbidden) return Forbid();
            model.ErrorMessage = ex.Message;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        { logger.LogWarning(ex, "Unable to load salary history."); model.ErrorMessage = "Unable to load salary history. Please try again."; }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return View(await api.GetDetailAsync(id, ct)); }
        catch (SalaryHistoryApiException ex)
        {
            if (ex.Status == HttpStatusCode.Unauthorized) return Challenge();
            if (ex.Status == HttpStatusCode.Forbidden) return Forbid();
            if (ex.Status == HttpStatusCode.NotFound) return NotFound();
            TempData["SalaryHistoryError"] = ex.Message;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or InvalidOperationException)
        { logger.LogWarning(ex, "Unable to load salary history {Id}.", id); TempData["SalaryHistoryError"] = "Unable to load salary change details. Please try again."; }
        return RedirectToAction(nameof(Index));
    }
}
