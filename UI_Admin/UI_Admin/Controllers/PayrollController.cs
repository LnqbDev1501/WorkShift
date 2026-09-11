using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Payroll;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

public class PayrollController(PayrollApiService payroll, RegionApiService regions, ILogger<PayrollController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(PayrollFilter filter, CancellationToken ct)
    {
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
                var result = await payroll.GetPeriodsAsync(new PayrollFilter { Status = status, PageSize = 1,
                    Search = filter.Search, RegionId = filter.RegionId, BranchId = filter.BranchId }, ct);
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
            model.Regions = await regions.GetAllAsync();
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
    public async Task<IActionResult> Details(int id, int pageNumber = 1, CancellationToken ct = default)
    {
        if (id < 1 || pageNumber < 1) return BadRequest();
        try { return View(await payroll.GetDetailAsync(id, pageNumber, ct)); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { return NotFound(); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Could not load payroll period {Id}", id);
            TempData["PayrollError"] = "Không thể tải chi tiết kỳ lương. Vui lòng thử lại.";
            return RedirectToAction(nameof(List));
        }
    }
}
