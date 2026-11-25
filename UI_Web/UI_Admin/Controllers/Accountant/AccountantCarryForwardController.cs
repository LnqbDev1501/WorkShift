using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using UI_Admin.Models.Accountant.PayrollSettlement;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantCarryForwardController(PayrollDocumentsApiService api, ILogger<AccountantCarryForwardController> logger)
    : PayrollDocumentsUiController(logger)
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CarryForwardListRequest query, CancellationToken ct)
    {
        query.PageSize = 20;
        var model = new CarryForwardListViewModel { Query = query };
        try
        {
            model.Data = await api.ListCarryAsync(query, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            model.ErrorMessage = Message(ex);
        }
        return View("~/Views/AccountantPayrollPeriods/CarryForwardList.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (id <= 0) return NotFound();
        var model = new CarryForwardDetailViewModel();
        try
        {
            model.Item = await api.GetCarryAsync(id, ct);
            model.Applications = await api.CarryApplicationsAsync(id, page, 50, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (Exception ex) when (Handled(ex))
        {
            if (Access(ex) is IActionResult denied) return denied;
            model.ErrorMessage = Message(ex);
        }
        return View("~/Views/AccountantPayrollPeriods/CarryForwardDetail.cshtml", model);
    }

    [HttpGet]
    public Task<IActionResult> CarryForwardDetail(int id, [FromQuery] int page = 1, CancellationToken ct = default)
        => Details(id, page, ct);
}
