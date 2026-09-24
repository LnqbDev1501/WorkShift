using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Models.Payroll;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public class PayrollController(PayrollApiService payroll, RegionApiService regions, ILogger<PayrollController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(PayrollFilter filter, CancellationToken ct)
    {
        filter.PageSize = ListPaginationViewModel.DefaultPageSize;
        ModelState.Remove(nameof(filter.PageSize));
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
                    BranchId = filter.BranchId
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

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(PayrollReviewInput input, CancellationToken ct) => ReviewAsync(input, null, ct);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(PayrollRejectInput input, CancellationToken ct) => ReviewAsync(input, input, ct);

    private async Task<IActionResult> ReviewAsync(PayrollReviewInput input, PayrollRejectInput? rejection, CancellationToken ct)
    {
        if (input.PeriodId < 1 || input.PageNumber < 1 || input.PageSize is < 1 or > 100) return BadRequest();
        try
        {
            if (ModelState.IsValid)
            {
                if (rejection == null) await payroll.ApproveAsync(input.PeriodId, ct);
                else await payroll.RejectAsync(rejection, ct);
                TempData["PayrollSuccess"] = rejection == null ? "Đã duyệt toàn bộ kỳ lương." : "Đã gửi phiếu lương về để chỉnh sửa. Kỳ lương cần được gửi lại trước khi duyệt.";
                TempData["PayrollAction"] = rejection == null ? "approve" : "reject";
                return RedirectToAction(nameof(Details), new { id = input.PeriodId, input.Search, input.PageNumber, input.PageSize });
            }
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Payroll review failed for {Id}", input.PeriodId);
            ModelState.AddModelError("", "Không xác nhận được kết quả thao tác. Hãy tải lại trang để kiểm tra trạng thái trước khi thử lại.");
        }
        try
        {
            var model = await LoadDetailAsync(input.PeriodId, input, ct);
            model.RejectedPayrollId = rejection?.PayrollId;
            model.RejectionReason = rejection?.Reason;
            return View("Details", model);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not refresh payroll review");
            TempData["PayrollError"] = "Không thể tải lại kỳ lương. Vui lòng kiểm tra trạng thái trước khi thực hiện lại thao tác.";
            return RedirectToAction(nameof(List));
        }
    }
}
