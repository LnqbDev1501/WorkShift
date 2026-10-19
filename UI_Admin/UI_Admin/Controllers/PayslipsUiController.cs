using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;
namespace UI_Admin.Controllers;

public abstract class PayslipsUiController(PayrollDocumentsApiService api, ILogger logger) : PayrollDocumentsUiController(logger)
{
    protected Task<IActionResult> List(PayslipListRequest query, CancellationToken ct)
    {
        query.PageSize = 20;
        query.Status = query.Status?.Trim().ToUpperInvariant();
        if (query.Status == "REPLACED") query.IncludeHistory = true;
        if (query.Page < 1 || query.Page > int.MaxValue / 100 || query.Keyword?.Length > 200 || query.PeriodFrom > query.PeriodTo) ModelState.AddModelError("", "Bộ lọc phiếu lương không hợp lệ.");
        if (query.Status != null && query.Status is not ("PUBLISHED" or "REPLACED" or "")) ModelState.AddModelError("Status", "Trạng thái phiếu lương không hợp lệ.");
        return Read(async () =>
        {
            var model = new PayslipListViewModel { Query = query };
            if (ModelState.IsValid)
                try { model.Data = await api.Get<PayslipListResponse>("/payslips" + PayrollDocumentsApiService.Query(query), ct); }
                catch (Exception ex) when (Handled(ex) && ex is not OperationCanceledException) { if (Access(ex) is IActionResult denied) return denied; model.Error = Message(ex); }
            return View("~/Views/Payslips/Index.cshtml", model);
        }, ct);
    }
    protected Task<IActionResult> Detail(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () => View("~/Views/Payslips/Details.cshtml", await api.Payslip(id, ct)), ct);
    protected Task<IActionResult> History(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () => View("~/Views/Payslips/Versions.cshtml", await api.Get<PayslipVersionHistoryResponse>($"/payslips/{id}/versions", ct)), ct);
}
