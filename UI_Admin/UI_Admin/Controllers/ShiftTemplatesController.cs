using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "ADMIN")]
public sealed class ShiftTemplatesController(
    ShiftTemplateApiService service, ILogger<ShiftTemplatesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        try
        {
            var model = await service.GetByIdAsync(id, cancellationToken);
            if (model is not null) return View(model);
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View(new ShiftTemplateDetailViewModel { ShiftTemplateId = id, ErrorMessage = "This shift template could not be found." });
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to load shift template {Id}.", id);
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new ShiftTemplateDetailViewModel { ShiftTemplateId = id, ErrorMessage = "Unable to load this template. Please try again later." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ChangeShiftTemplateStatusViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest();
        var id = model.Id;
        var restore = model.Restore!.Value;
        try
        {
            var result = await service.SetStatusAsync(id, restore, cancellationToken);
            if (!result.Success)
                TempData["ShiftTemplateError"] = result.Message;
            else
            {
                TempData["ShiftTemplateSuccess"] = result.Message;
                TempData["ShiftTemplateSuccessTitle"] = restore ? "Template restored" : "Template deactivated";
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to confirm status change for shift template {Id}.", id);
            TempData["ShiftTemplateError"] = "Unable to confirm the status change. Check the current status before trying again.";
        }
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateShiftTemplateViewModel
    {
        StartTime = "08:00:00", EndTime = "16:30:00", BreakMinutes = 45
    });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShiftTemplateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await service.CreateAsync(model, cancellationToken);
            if (result.Success)
            {
                TempData["ShiftTemplateSuccess"] = $"Shift template “{model.ShiftName?.Trim()}” created successfully.";
                return RedirectToAction(nameof(List));
            }
            ModelState.AddModelError(result.DuplicateName ? nameof(model.ShiftName) : string.Empty, result.Error!);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to confirm shift template creation.");
            ModelState.AddModelError(string.Empty,
                "Unable to confirm whether the template was created. Check the template list before trying again. Your entered values have been kept.");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ShiftTemplateListQueryViewModel query, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var model = new ShiftTemplateListViewModel { Query = query };
        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Invalid filters. Check the status, page and time range below.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(model);
        }

        try
        {
            model.Result = await service.GetAllAsync(query, cancellationToken);
            if (query.Page > Math.Max(1, model.LastPage))
                return RedirectToAction(nameof(List), new { page = Math.Max(1, model.LastPage), search = query.Search,
                    status = query.Status, startTime = query.StartTime?.ToString("HH:mm:ss"), endTime = query.EndTime?.ToString("HH:mm:ss") });
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Unable to load shift templates for page {Page}.", query.Page);
            model.ErrorMessage = "Unable to load shift templates. Please try again later.";
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        return View(model);
    }
}
