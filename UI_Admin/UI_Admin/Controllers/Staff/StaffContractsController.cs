using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.Staff.Contract;
using UI_Admin.Services.Staff;

namespace UI_Admin.Controllers.Staff;

[Authorize(Roles = "STAFF")]
public class StaffContractsController(StaffContractApiService api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? startDateFrom,
        string? startDateTo,
        CancellationToken ct = default)
    {
        var model = new StaffContractListViewModel
        {
            Search = search,
            StatusFilter = status,
            StartDateFrom = startDateFrom,
            StartDateTo = startDateTo
        };

        try
        {
            model.Items = await api.GetMyContractsAsync(status, search, startDateFrom, startDateTo, ct);
        }
        catch (HttpRequestException ex)
        {
            model.ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            model.ErrorMessage = "Không thể tải danh sách hợp đồng. Vui lòng thử lại sau.";
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct = default)
    {
        try
        {
            var detail = await api.GetMyContractByIdAsync(id, ct);
            detail.ChangeRequests = await api.GetMyContractChangeRequestsAsync(id, ct);
            detail.ChangeRequestForm = new SubmitStaffContractChangeRequestViewModel { ContractId = id };
            try { detail.PositionOptions = await api.GetActivePositionOptionsAsync(ct); } catch { detail.PositionOptions = []; }
            return View(detail);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            TempData["ErrorMessage"] = "Không tìm thấy hợp đồng yêu cầu hoặc bạn không có quyền xem.";
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Không thể tải thông tin chi tiết hợp đồng.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id, CancellationToken ct = default)
    {
        try
        {
            await api.AcceptContractAsync(id, ct);
            TempData["SuccessMessage"] = "Bạn đã chấp thuận hợp đồng lao động thành công!";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Không thể chấp thuận hợp đồng: {ex.Message}";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi chấp thuận hợp đồng. Vui lòng thử lại.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id, CancellationToken ct = default)
    {
        try
        {
            var (bytes, contentType, fileName) = await api.GetStaffPdfFileAsync(id, isDownload: false, ct);
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{fileName}\"");
            return File(bytes, contentType);
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Không thể xem PDF hợp đồng: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Không thể mở tệp PDF hợp đồng.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct = default)
    {
        try
        {
            var (bytes, contentType, fileName) = await api.GetStaffPdfFileAsync(id, isDownload: true, ct);
            return File(bytes, contentType, fileName);
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Không thể tải PDF hợp đồng: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Không thể tải tệp PDF hợp đồng.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitChangeRequest(
        int id,
        SubmitStaffContractChangeRequestViewModel changeRequestForm,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu yêu cầu thay đổi không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            changeRequestForm.ContractId = id;
            await api.SubmitChangeRequestAsync(id, changeRequestForm, ct);
            TempData["SuccessMessage"] = "Đã gửi yêu cầu thay đổi hợp đồng thành công. Quản lý sẽ sớm phản hồi.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Không thể gửi yêu cầu: {ex.Message}";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi gửi yêu cầu thay đổi hợp đồng.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
