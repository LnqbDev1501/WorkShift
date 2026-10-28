using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;

namespace UI_Admin.Controllers;
public abstract class PayrollFeedbackUiController(PayrollDocumentsApiService api, ILogger logger) : PayrollDocumentsUiController(logger)
{
    protected PayrollDocumentsApiService Api { get; } = api;
    protected Task<IActionResult> List(PayrollFeedbackListRequest query, CancellationToken ct)
    {
        query.PageSize = 20;
        if (query.Page < 1 || query.Page > int.MaxValue / 100 || query.Keyword?.Length > 200 || query.CreatedFrom > query.CreatedTo) ModelState.AddModelError("", "Bộ lọc phản hồi không hợp lệ.");
        return Read(async () =>
        {
            var model = new FeedbackListViewModel { Query = query };
            if (ModelState.IsValid)
                try { model.Data = await Api.Get<PayrollFeedbackListResponse>("/payroll-feedback-requests" + PayrollDocumentsApiService.Query(query), ct); }
                catch (Exception ex) when (Handled(ex) && ex is not OperationCanceledException) { if (Access(ex) is IActionResult denied) return denied; model.Error = Message(ex); }
            return View("~/Views/PayrollFeedback/Index.cshtml", model);
        }, ct);
    }
    protected Task<IActionResult> Detail(int id, CancellationToken ct) => id <= 0 ? Task.FromResult<IActionResult>(NotFound()) : Read(async () => View("~/Views/PayrollFeedback/Details.cshtml", await Api.Feedback(id, ct)), ct);
}
