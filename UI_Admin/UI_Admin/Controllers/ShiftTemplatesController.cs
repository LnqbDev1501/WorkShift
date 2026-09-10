using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public sealed class ShiftTemplatesController(
    ShiftTemplateApiService service, ILogger<ShiftTemplatesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ShiftTemplateListQueryViewModel query, CancellationToken cancellationToken)
    {
        query.Search = query.Search?.Trim();
        var model = new ShiftTemplateListViewModel { Query = query };
        if (!ModelState.IsValid)
        {
            model.ErrorMessage = "Invalid query. Page must be a whole number between 1 and 20.";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(model);
        }

        try
        {
            model.Result = await service.GetAllAsync(query, cancellationToken);
            if (query.Page > Math.Max(1, model.LastPage))
                return RedirectToAction(nameof(List), new { page = Math.Max(1, model.LastPage), search = query.Search });
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
