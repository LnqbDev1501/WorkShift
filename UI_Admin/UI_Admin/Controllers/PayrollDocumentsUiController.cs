using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public abstract class PayrollDocumentsUiController(ILogger logger) : Controller
{
    protected static bool Handled(Exception ex) => ex is CalculationApiException or HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException;
    protected IActionResult? Access(Exception ex) => (ex as CalculationApiException)?.Status switch { HttpStatusCode.Unauthorized => Challenge(), HttpStatusCode.Forbidden => Forbid(), HttpStatusCode.NotFound => NotFound(), _ => null };
    protected string Message(Exception ex) { logger.LogWarning(ex, "Payroll document request failed."); return ex is CalculationApiException ? ex.Message : "Không xác nhận được kết quả. Hãy tải lại dữ liệu trước khi thử lại."; }
    protected async Task<IActionResult> Read(Func<Task<IActionResult>> action, CancellationToken ct)
    {
        try { return await action(); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { return Access(ex) ?? View("~/Views/PayrollCalculation/Error.cshtml", Message(ex)); }
    }
    protected async Task<IActionResult> Change(Func<Task> action, Func<IActionResult> next, CancellationToken ct)
    {
        try { await action(); TempData["WorkflowSuccess"] = "Thao tác đã được ghi nhận."; }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["WorkflowError"] = Message(ex); }
        return next();
    }
}
