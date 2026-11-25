using System;
using System.Threading;
using System.Threading.Tasks;
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
            model.ErrorMessage = "Unable to load contracts. Please try again later.";
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
            TempData["ErrorMessage"] = "Contract not found or access denied.";
            return RedirectToAction(nameof(Index));
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to load contract details.";
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
            TempData["SuccessMessage"] = "Employment contract accepted successfully.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Unable to accept contract: {ex.Message}";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to accept the contract. Please try again.";
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
            TempData["ErrorMessage"] = $"Unable to view contract PDF: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to open the contract PDF.";
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
            TempData["ErrorMessage"] = $"Unable to download contract PDF: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to download the contract PDF.";
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
            TempData["ErrorMessage"] = "Invalid change request. Please review your input.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            changeRequestForm.ContractId = id;
            await api.SubmitChangeRequestAsync(id, changeRequestForm, ct);
            TempData["SuccessMessage"] = "Contract change request submitted. Your manager will review it soon.";
        }
        catch (HttpRequestException ex)
        {
            TempData["ErrorMessage"] = $"Unable to submit request: {ex.Message}";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Unable to submit the contract change request.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
