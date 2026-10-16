using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Accountant.PayrollPeriods;
using UI_Admin.Services;
using UI_Admin.Services.Accountant;
namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayrollPeriodsController(PayrollPeriodApiService api, PayrollPeriodReportApiService reports, ILogger<AccountantPayrollPeriodsController> logger) : Controller
{
    private IActionResult? Access(Exception ex)
    {
        var status = ex is PayrollPeriodApiException p ? p.Status : (ex as HttpRequestException)?.StatusCode;
        return status switch { HttpStatusCode.Unauthorized => Challenge(), HttpStatusCode.Forbidden => Forbid(), HttpStatusCode.NotFound => NotFound(), _ => null };
    }
    private static bool Handled(Exception ex) => ex is PayrollPeriodApiException or HttpRequestException or JsonException or InvalidOperationException or OperationCanceledException;
    private string Error(Exception ex)
    {
        logger.LogWarning(ex, "Payroll period UI request failed.");
        return ex is PayrollPeriodApiException || ex is HttpRequestException { StatusCode: HttpStatusCode.BadRequest or HttpStatusCode.Conflict }
            ? ex.Message : "Không xác nhận được kết quả thao tác. Hãy tải lại kỳ lương để kiểm tra trước khi thử lại.";
    }
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] PeriodQuery query, CancellationToken ct)
    {
        query.Status = query.Status?.Trim().ToUpperInvariant();
        if (!string.IsNullOrEmpty(query.Status) && !PeriodDisplay.Statuses.Contains(query.Status)) ModelState.AddModelError("Status", "Trạng thái không hợp lệ.");
        if (query.StartDateFrom > query.StartDateTo) ModelState.AddModelError("StartDateTo", "Ngày kết thúc bộ lọc phải sau hoặc bằng ngày bắt đầu.");
        var model = new PeriodListViewModel { Query = query };
        if (ModelState.IsValid)
            try
            {
                model.Page = await api.ListAsync(query, ct);
                if (query.PageNumber > model.Page.TotalPages) { query.PageNumber = model.Page.TotalPages; model.Page = await api.ListAsync(query, ct); }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; model.Error = Error(ex); }
        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return View(await api.DetailAsync(id, ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["PeriodError"] = Error(ex); return RedirectToAction(nameof(Index)); }
    }
    private async Task LoadWeeks(PeriodForm form, PeriodRecord? current, CancellationToken ct)
    {
        form.Weeks = await api.WeeksAsync(ct);
        if (current?.ScheduleWeekId is int week && !form.Weeks.Any(w => w.ScheduleWeekId == week))
            form.Weeks.Insert(0, new() { ScheduleWeekId = week, BranchId = current.BranchId, WeekStartDate = current.StartDate, WeekEndDate = current.EndDate, Status = "Tuần hiện tại" });
        form.WeeksLoaded = true;
    }
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var form = new PeriodForm();
        try { await LoadWeeks(form, null, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; ModelState.AddModelError("", Error(ex)); }
        return View("Form", form);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var current = await api.DetailAsync(id, ct);
            if (!current.CanUpdate) { TempData["PeriodError"] = "Kỳ này không còn được sửa bản nháp."; return RedirectToAction(nameof(Details), new { id }); }
            var form = new PeriodForm { Id = id, PeriodName = current.PeriodName, ScheduleWeekId = current.ScheduleWeekId ?? 0 };
            await LoadWeeks(form, current, ct); return View("Form", form);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["PeriodError"] = Error(ex); return RedirectToAction(nameof(Details), new { id }); }
    }
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Create([Bind("PeriodName,ScheduleWeekId")] PeriodForm form, CancellationToken ct) => Save(form, ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Edit(int id, [Bind("Id,PeriodName,ScheduleWeekId")] PeriodForm form, CancellationToken ct)
    { if (id <= 0 || id != form.Id) return Task.FromResult<IActionResult>(BadRequest()); return Save(form, ct); }
    private async Task<IActionResult> Save(PeriodForm form, CancellationToken ct)
    {
        PeriodRecord? current = null;
        try
        {
            if (form.Id != 0)
            {
                current = await api.DetailAsync(form.Id, ct);
                if (!current.CanUpdate) { TempData["PeriodError"] = "Bản nháp không còn được sửa."; return RedirectToAction(nameof(Details), new { id = form.Id }); }
            }
            if (ModelState.IsValid)
            {
                var id = await api.SaveAsync(form, ct);
                TempData["PeriodSuccess"] = form.Id == 0 ? "Đã tạo kỳ lương DRAFT." : "Đã cập nhật bản nháp.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; ModelState.AddModelError("", Error(ex)); }
        try { await LoadWeeks(form, current, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; ModelState.AddModelError("", Error(ex)); }
        return View("Form", form);
    }
    [HttpGet] public Task<IActionResult> Open(int id, CancellationToken ct) => Confirm(id, false, ct);
    [HttpGet] public Task<IActionResult> Delete(int id, CancellationToken ct) => Confirm(id, true, ct);
    private async Task<IActionResult> Confirm(int id, bool delete, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var p = await api.DetailAsync(id, ct);
            if (!(delete ? p.CanDelete : p.CanOpen)) { TempData["PeriodError"] = "Kỳ lương không còn đủ điều kiện thực hiện thao tác."; return RedirectToAction(nameof(Details), new { id }); }
            return View("Confirm", new PeriodConfirmation(p, delete));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["PeriodError"] = Error(ex); return RedirectToAction(nameof(Index)); }
    }
    [HttpPost, ActionName("Open"), ValidateAntiForgeryToken] public Task<IActionResult> OpenConfirmed(int id, CancellationToken ct) => Mutate(id, false, ct);
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken] public Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct) => Mutate(id, true, ct);
    private async Task<IActionResult> Mutate(int id, bool delete, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try
        {
            var p = await api.DetailAsync(id, ct);
            if (!(delete ? p.CanDelete : p.CanOpen)) throw new PayrollPeriodApiException(HttpStatusCode.Conflict, "Kỳ đã thay đổi; thao tác không còn được phép.");
            if (delete) await api.DeleteAsync(id, ct); else await api.OpenAsync(id, ct);
            TempData["PeriodSuccess"] = delete ? "Đã xóa bản nháp." : "Đã mở kỳ lương. Không thể sửa/xóa kỳ sau khi OPEN.";
            return delete ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Details), new { id });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["PeriodError"] = Error(ex); return RedirectToAction(nameof(Details), new { id }); }
    }
    [HttpGet]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return File(await reports.DownloadAsync(id, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Payroll-{id}.xlsx"); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (Handled(ex)) { if (Access(ex) is IActionResult denied) return denied; TempData["PeriodError"] = Error(ex); return RedirectToAction(nameof(Details), new { id }); }
    }
}
