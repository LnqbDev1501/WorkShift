using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UI_Admin.Services;
using UI_Admin.Models;
using UI_Admin.Models.Admin.Payroll;
using UI_Admin.Services.Admin;



namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public class PayrollController(PayrollApiService payroll, RegionApiService regions, PayrollPeriodReportApiService reports, ILogger<PayrollController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(PayrollFilter filter, CancellationToken ct)
    {
        filter.PageSize = ListPaginationViewModel.DefaultPageSize;
        ModelState.Remove(nameof(filter.PageSize));
        if (filter.StartDateFrom > filter.StartDateTo) ModelState.AddModelError("StartDateTo", "Khoảng ngày bắt đầu kỳ không hợp lệ.");
        var model = new PayrollListViewModel { Filter = filter };
        if (!ModelState.IsValid)
        {
            model.Error = "Bộ lọc không hợp lệ. Hãy đặt lại bộ lọc và thử lại.";
            return View(model);
        }
        try
        {
            model.Page = await payroll.GetPeriodsAsync(filter, ct);
            foreach (var status in new[] { "", "SUBMITTED", "APPROVED", "REVISION_REQUIRED" })
            {
                var result = await payroll.GetPeriodsAsync(new PayrollFilter
                {
                    Status = status,
                    PageSize = 1,
                    Search = filter.Search,
                    RegionId = filter.RegionId,
                    BranchId = filter.BranchId,
                    StartDateFrom = filter.StartDateFrom,
                    StartDateTo = filter.StartDateTo
                }, ct);
                model.Counts[status] = result.TotalCount;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not load payroll periods");
            model.Error = "Không thể tải dữ liệu kỳ lương. Vui lòng kiểm tra kết nối API và thử lại.";
        }
        try
        {
            var regionPage = await regions.GetPagedAsync(
                status: "ACTIVE",
                pageNumber: 1,
                pageSize: 100,
                cancellationToken: ct);
            model.Regions = regionPage.Items;
            if (!string.IsNullOrWhiteSpace(regionPage.ErrorMessage))
            {
                model.LookupError = regionPage.ErrorMessage;
            }
            model.Branches = await payroll.GetBranchesAsync(filter.RegionId, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not load payroll filters");
            model.LookupError = "Không thể tải đầy đủ bộ lọc khu vực/chi nhánh.";
        }
        return View(model);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Details(int id, PayrollDetailQuery query, CancellationToken ct)
    {
        query.PageSize = ListPaginationViewModel.DefaultPageSize;
        ModelState.Remove(nameof(query.PageSize));
        if (id < 1 || !ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var model = await LoadDetailAsync(id, query, ct);
            return View(model);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return NotFound(); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not load payroll period {Id}", id);
            TempData["PayrollError"] = "Không thể tải chi tiết kỳ lương. Vui lòng thử lại.";
            return RedirectToAction(nameof(List));
        }
    }

    private async Task<PayrollDetailViewModel> LoadDetailAsync(int id, PayrollDetailQuery query, CancellationToken ct) =>
        new() { Period = await payroll.GetDetailAsync(id, query, ct), Query = query };

    [HttpGet]
    public async Task<IActionResult> Export(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        try { return File(await reports.DownloadAsync(id, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Payroll-{id}.xlsx"); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized) { return Challenge(); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden) { return Forbid(); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return NotFound(); }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidOperationException)
        { logger.LogWarning(ex, "Payroll export failed {Id}", id); TempData["PayrollError"] = ex is HttpRequestException http && http.StatusCode == System.Net.HttpStatusCode.Conflict ? http.Message : "Không thể xuất báo cáo. Vui lòng thử lại."; return RedirectToAction(nameof(Details), new { id }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(PayrollReviewInput input, CancellationToken ct)
    {
        if (input.PeriodId < 1) return BadRequest();
        try
        {
            await payroll.ApproveAsync(input.PeriodId, ct);
            TempData["PayrollSuccess"] = "Đã duyệt toàn bộ kỳ lương. Hệ thống đã khóa công toàn kỳ và phát hành phiếu lương chính thức (Published Payslips).";
            TempData["PayrollAction"] = "approve";
            return RedirectToAction(nameof(Details), new { id = input.PeriodId, input.Search, input.PageNumber, input.PageSize });
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Payroll review failed for {Id}", input.PeriodId);
            ModelState.AddModelError("", "Không xác nhận được kết quả phê duyệt. Hãy tải lại trang để kiểm tra trạng thái trước khi thử lại.");
        }
        try
        {
            var model = await LoadDetailAsync(input.PeriodId, input, ct);
            return View("Details", model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not refresh payroll review");
            TempData["PayrollError"] = "Không thể tải lại kỳ lương. Vui lòng kiểm tra trạng thái trước khi thực hiện lại thao tác.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reject(PayrollRejectInput input)
    {
        TempData["PayrollError"] = "Admin chỉ phê duyệt toàn bộ kỳ lương đã được Submit. Các trường hợp cần chỉnh sửa phải được Accountant và Manager xử lý trước khi gửi duyệt.";
        return RedirectToAction(nameof(Details), new { id = input.PeriodId });
    }
}
