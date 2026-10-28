using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollCalculation;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public abstract class PayrollCalculationUiController(PayrollCalculationApiService api, ILogger logger) : Controller
{
    protected const string ListView = "~/Views/PayrollCalculation/Index.cshtml", DetailView = "~/Views/PayrollCalculation/Details.cshtml";
    protected static bool Handled(Exception ex) => ex is CalculationApiException or HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException;
    protected IActionResult? Access(Exception ex) => (ex as CalculationApiException)?.Status switch { HttpStatusCode.Unauthorized => Challenge(), HttpStatusCode.Forbidden => Forbid(), HttpStatusCode.NotFound => NotFound(), _ => null };
    protected string Error(Exception ex) { logger.LogWarning(ex, "Payroll calculation UI failed."); return ex is CalculationApiException ? ex.Message : "Không xác nhận được kết quả. Hãy tải lại dữ liệu Payroll trước khi thử lại."; }
    protected CalculationViewModel Model(int id, CalculationQuery query, bool accountant)
    {
        query.Status = query.Status?.Trim().ToUpperInvariant();
        if (query.UserId == Guid.Empty) ModelState.AddModelError("UserId", "Staff ID không hợp lệ.");
        if (!string.IsNullOrEmpty(query.Status) && query.Status is not ("CALCULATED" or "SUBMITTED" or "REJECTED" or "APPROVED" or "PAID" or "REPLACED")) ModelState.AddModelError("Status", "Trạng thái Payroll không hợp lệ.");
        return new() { PeriodId = id, Query = query, Accountant = accountant };
    }
    protected async Task Load(CalculationViewModel model, CancellationToken ct)
    {
        model.Page = await api.ListAsync(model.PeriodId, model.Query, ct);
        var last = Math.Max(1, (int)Math.Ceiling(model.Page.TotalItems / (double)model.Page.PageSize));
        if (model.Query.Page > last) { model.Query.Page = last; model.Page = await api.ListAsync(model.PeriodId, model.Query, ct); }
    }
    protected async Task<IActionResult> ReadDetail(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return View(DetailView, await api.DetailAsync(id, ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; return View("~/Views/PayrollCalculation/Error.cshtml", Error(ex)); }
    }
}
