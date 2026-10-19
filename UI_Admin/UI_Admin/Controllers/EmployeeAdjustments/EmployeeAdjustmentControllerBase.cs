using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.EmployeeAdjustments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.EmployeeAdjustments;

[Authorize(Roles = "ADMIN,MANAGER,ACCOUNTANT")]
public abstract class EmployeeAdjustmentControllerBase(EmployeeAdjustmentApiService api, ILogger logger) : Controller
{
    protected readonly EmployeeAdjustmentApiService Api = api;
    protected const string ViewRoot = "~/Views/Shared/EmployeeAdjustments/";

    protected abstract string AdjustmentType { get; }

    /// <summary>Admin list (pending approvals) defaults to PENDING when no status filter is supplied.</summary>
    protected virtual string? DefaultApprovalStatus => null;

    protected IActionResult Failure(Exception ex)
    {
        if (ex is EmployeeAdjustmentApiException { Status: HttpStatusCode.Unauthorized }) return RedirectToAction("Login", "Auth");
        if (ex is EmployeeAdjustmentApiException { Status: HttpStatusCode.Forbidden }) return Forbid();
        if (ex is EmployeeAdjustmentApiException { Status: HttpStatusCode.NotFound }) return NotFound();
        logger.LogError(ex, "Employee adjustment UI request failed.");
        TempData["EmployeeAdjustmentError"] = Error(ex);
        return RedirectToAction(nameof(Index));
    }

    protected static string Error(Exception ex) => ex is EmployeeAdjustmentApiException
        ? ex.Message
        : "Unable to connect to the server. Reload the page before trying again.";

    [HttpGet]
    public async Task<IActionResult> Index(EmployeeAdjustmentQuery query, CancellationToken ct)
    {
        if (!User.IsInRole("ADMIN"))
        {
            query.BranchId = null;
            ModelState.Remove(nameof(query.BranchId));
        }
        if (string.IsNullOrWhiteSpace(query.ApprovalStatus) && DefaultApprovalStatus != null)
            query.ApprovalStatus = DefaultApprovalStatus;

        var model = new EmployeeAdjustmentListViewModel { AdjustmentType = AdjustmentType, Query = query };
        if (!ModelState.IsValid) return View(ViewRoot + "Index.cshtml", model);
        try { model = await Api.ListAsync(AdjustmentType, query, ct); }
        catch (EmployeeAdjustmentApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { return Failure(ex); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Employee adjustment list failed.");
            model.ErrorMessage = Error(ex);
        }
        return View(ViewRoot + "Index.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            return View(ViewRoot + "Details.cshtml", new EmployeeAdjustmentDetailsViewModel
            {
                Item = await Api.DetailAsync(AdjustmentType, id, ct)
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException) { return Failure(ex); }
    }
}
