using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using UI_Admin.Models.Manager;
using UI_Admin.Models.Manager.Contract;
using UI_Admin.Services;

namespace UI_Admin.Controllers;

[Authorize(Roles = "MANAGER")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ManagerContractsController(ManagerContractApiService api, IMemoryCache cache) : Controller
{
    private string Owner => User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? string.Empty;
    private record Confirmation(string Owner, ContractCreatedResponse Contract);

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        Guid? staffUserId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = new ManagerContractListViewModel
        {
            PageNumber = page < 1 ? 1 : page,
            PageSize = 10,
            Search = search,
            StatusFilter = status,
            StaffUserIdFilter = staffUserId
        };

        try
        {
            var paged = await api.GetPagedAsync(search, status, staffUserId, model.PageNumber, model.PageSize, cancellationToken);
            model.Items = paged.Items;
            model.TotalItems = paged.TotalItems;
            model.PageNumber = paged.PageNumber;
            model.PageSize = paged.PageSize;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, "Unable to load contract list: " + ex.Message);
        }

        try
        {
            model.StaffOptions = await api.GetStaffOptionsAsync(cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await ExpiredAsync();
        }
        catch
        {
            // Dropdown error should not prevent displaying existing contracts
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        int historyPage = 1,
        int historyPageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        try
        {
            var contract = await api.GetByIdAsync(id, cancellationToken);
            historyPage = Math.Max(1, historyPage);
            historyPageSize = Math.Clamp(historyPageSize, 1, 50);

            try
            {
                contract.History = await api.GetHistoryAsync(
                    id,
                    pageNumber: historyPage,
                    pageSize: historyPageSize,
                    sort: "desc",
                    ct: cancellationToken);

                if (contract.History == null)
                {
                    contract.HistoryLoadFailed = true;
                    contract.HistoryErrorMessage = "Unable to load contract history data from the server.";
                }
            }
            catch (Exception ex)
            {
                contract.HistoryLoadFailed = true;
                contract.HistoryErrorMessage = "Connection error loading contract history: " + ex.Message;
            }

            return View(contract);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["ErrorMessage"] = "You do not have permission to access contracts belonging to another branch.";
                return RedirectToAction(nameof(Index));
            }
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] = "The requested contract was not found.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> StaffOptions([FromQuery] string? mode, CancellationToken cancellationToken)
    {
        try
        {
            var options = await api.GetStaffOptionsAsync(mode, cancellationToken);
            return Json(options);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> BranchMinimumWage(CancellationToken cancellationToken)
    {
        try
        {
            var wage = await api.GetBranchMinimumWageAsync(cancellationToken);
            return Json(new { minimumWage = wage });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? contractType, CancellationToken cancellationToken)
    {
        var model = new CreateContractViewModel
        {
            ContractType = string.Equals(contractType, "RENEWAL", StringComparison.OrdinalIgnoreCase) ? "RENEWAL" : "NEW"
        };
        return await ReloadAsync(model, cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateContractViewModel model, CancellationToken cancellationToken)
    {
        if (model.StaffUserId.HasValue && string.IsNullOrWhiteSpace(model.EmployeePosition))
        {
            try
            {
                var isRenewal = string.Equals(model.ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase);
                var mode = isRenewal ? "RENEWAL" : "NEW";
                var options = await api.GetStaffOptionsAsync(mode, cancellationToken);
                var selected = options.FirstOrDefault(s => s.StaffUserId == model.StaffUserId.Value);
                if (!string.IsNullOrWhiteSpace(selected?.CurrentPosition))
                {
                    model.EmployeePosition = selected.CurrentPosition;
                    ModelState.Remove(nameof(model.EmployeePosition));
                }
            }
            catch
            {
                // Fall through to regular model validation
            }
        }

        if (!ModelState.IsValid) return await ReloadAsync(model, cancellationToken);
        try
        {
            var result = await api.CreateAsync(model, cancellationToken);
            var key = Guid.NewGuid().ToString("N");
            cache.Set($"manager-contract:{key}", new Confirmation(Owner, result with { ContractTerms = null }), TimeSpan.FromMinutes(20));
            TempData["SuccessMessage"] = "Draft contract created successfully.";
            return RedirectToAction(nameof(Created), new { key });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            // A branch-scope 403 must keep the entered form and the backend explanation.
            if (ex.StatusCode == HttpStatusCode.Forbidden && ex.Message.StartsWith("Request failed with status code"))
                return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, ex.StatusCode.HasValue ? ex.Message : "Unable to connect to the server. Please check your connection and try again.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { ModelState.AddModelError(string.Empty, "Request timed out. Please try again."); }
        catch (System.Text.Json.JsonException) { ModelState.AddModelError(string.Empty, "Unable to parse contract creation response. Please try again."); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        return await ReloadAsync(model, cancellationToken);
    }

    [HttpGet]
    public IActionResult Created(string key)
    {
        if (string.IsNullOrEmpty(key) || !cache.TryGetValue<Confirmation>($"manager-contract:{key}", out var entry) || entry?.Owner != Owner)
            return RedirectToAction(nameof(Create));
        return View(entry.Contract);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        try
        {
            var contract = await api.GetByIdAsync(id, cancellationToken);
            if (!string.Equals(contract.Status, "DRAFT", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Contracts can only be edited while in DRAFT status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new EditContractViewModel
            {
                ContractId = contract.ContractId,
                ContractNumber = contract.ContractNumber,
                EmployeeFullName = contract.EmployeeFullName,
                BranchName = contract.BranchName,
                RegionName = contract.RegionName,
                CurrentHourlyRate = contract.HourlyRate,
                Status = contract.Status,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                EmployeePosition = contract.EmployeePosition,
                ContractTerms = contract.ContractTerms,
                PositionOptions = await api.GetActivePositionOptionsAsync(cancellationToken)
            };

            return View(model);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["ErrorMessage"] = "You do not have permission to access contracts belonging to another branch.";
                return RedirectToAction(nameof(Index));
            }
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                TempData["ErrorMessage"] = "The requested contract was not found.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditContractViewModel model, CancellationToken cancellationToken = default)
    {
        model.ContractId = id;
        if (!ModelState.IsValid)
        {
            await PopulateEditDisplayFieldsAsync(id, model, cancellationToken);
            return View(model);
        }

        try
        {
            await api.UpdateAsync(id, model, cancellationToken);
            TempData["SuccessMessage"] = "Draft contract updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden && ex.Message.StartsWith("Request failed with status code"))
                return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, ex.StatusCode.HasValue ? ex.Message : "Unable to connect to the server. Please check your connection and try again.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { ModelState.AddModelError(string.Empty, "Request timed out. Please try again."); }
        catch (System.Text.Json.JsonException) { ModelState.AddModelError(string.Empty, "Unable to parse contract update response. Please try again."); }
        catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }

        await PopulateEditDisplayFieldsAsync(id, model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for cancelling the contract.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await api.CancelAsync(id, reason.Trim(), cancellationToken);
            TempData["SuccessMessage"] = "Draft contract cancelled successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int id, string? reason, DateOnly? terminationEffectiveDate, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return RedirectToAction(nameof(Index));

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng cung cấp lý do chấm dứt hợp đồng.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await api.TerminateAsync(id, reason.Trim(), terminationEffectiveDate, cancellationToken);
            TempData["SuccessMessage"] = "Hợp đồng đã được chấm dứt trước thời hạn thành công.";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            TempData["ErrorMessage"] = ex.Message;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> PreviewPdf(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var res = await api.GetPdfPreviewAsync(id, cancellationToken);
            return File(res.Bytes, res.ContentType);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            await api.SendContractAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "The contract PDF has been generated and sent to the staff member successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var res = await api.GetPdfFileAsync(id, isDownload: false, cancellationToken);
            return File(res.Bytes, res.ContentType);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(Index));
        try
        {
            var res = await api.GetPdfFileAsync(id, isDownload: true, cancellationToken);
            return File(res.Bytes, res.ContentType, res.FileName ?? $"Contract_{id}.pdf");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // ==========================================
    // Contract Change Requests
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> ChangeRequests(
        string? status = "SUBMITTED",
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await api.GetChangeRequestsPagedAsync(status, page, 10, cancellationToken);
            return View(model);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            TempData["ErrorMessage"] = $"Unable to load change requests list: {ex.Message}";
            return View(new ManagerContractChangeRequestListViewModel { StatusFilter = status });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            return View(new ManagerContractChangeRequestListViewModel { StatusFilter = status });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ChangeRequestDetails(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(ChangeRequests));
        try
        {
            var detail = await api.GetChangeRequestByIdAsync(id, cancellationToken);
            return View(detail);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(ChangeRequests));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Change request details not found: {ex.Message}";
            return RedirectToAction(nameof(ChangeRequests));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveChangeRequest(
        int id,
        ApproveContractChangeRequestViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(ChangeRequests));

        try
        {
            await api.ApproveChangeRequestAsync(id, model, cancellationToken);
            TempData["SuccessMessage"] = "The change request has been approved and the contract updated. The revised contract has been sent to the staff member for acceptance.";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = $"Unable to approve change request: {ex.Message}";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while approving the change request: {ex.Message}";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectChangeRequest(
        int id,
        RejectContractChangeRequestViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0) return RedirectToAction(nameof(ChangeRequests));

        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            TempData["ErrorMessage"] = "A reason for rejecting the change request is required.";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }

        try
        {
            await api.RejectChangeRequestAsync(id, model.Reason, cancellationToken);
            TempData["SuccessMessage"] = "The change request has been rejected. The contract status has been restored.";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            TempData["ErrorMessage"] = $"Unable to reject change request: {ex.Message}";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while rejecting the change request: {ex.Message}";
            return RedirectToAction(nameof(ChangeRequestDetails), new { id });
        }
    }

    private async Task PopulateEditDisplayFieldsAsync(int id, EditContractViewModel model, CancellationToken ct)
    {
        try
        {
            var existing = await api.GetByIdAsync(id, ct);
            model.ContractNumber = existing.ContractNumber;
            model.EmployeeFullName = existing.EmployeeFullName;
            model.BranchName = existing.BranchName;
            model.RegionName = existing.RegionName;
            model.CurrentHourlyRate = existing.HourlyRate;
            model.Status = existing.Status;
            model.PositionOptions = await api.GetActivePositionOptionsAsync(ct);
        }
        catch
        {
            // Ignore display-enrichment failure on error redisplay
        }
    }

    private async Task<IActionResult> ReloadAsync(CreateContractViewModel model, CancellationToken ct)
    {
        try
        {
            var isRenewal = string.Equals(model.ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase);
            var mode = isRenewal ? "RENEWAL" : "NEW";
            var options = await api.GetStaffOptionsAsync(mode, ct);
            model.StaffOptions = options;
            if (isRenewal)
            {
                model.RenewalStaffOptions = options;
            }
            else
            {
                model.NewStaffOptions = options;
            }

            if (model.StaffUserId.HasValue && string.IsNullOrWhiteSpace(model.EmployeePosition))
            {
                var selected = options.FirstOrDefault(s => s.StaffUserId == model.StaffUserId.Value);
                if (!string.IsNullOrWhiteSpace(selected?.CurrentPosition))
                {
                    model.EmployeePosition = selected.CurrentPosition;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized) return await ExpiredAsync();
            if (ex.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Auth");
            ModelState.AddModelError(string.Empty, "Unable to load staff list. Please try again.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { ModelState.AddModelError(string.Empty, "Staff list loading timed out. Please try again."); }
        catch (System.Text.Json.JsonException) { ModelState.AddModelError(string.Empty, "Unable to parse staff list data. Please try again."); }

        try
        {
            model.PositionOptions = await api.GetActivePositionOptionsAsync(ct);
        }
        catch
        {
            model.PositionOptions = [];
        }

        try
        {
            model.RegionalMinimumWage = await api.GetBranchMinimumWageAsync(ct);
            if (string.Equals(model.ContractType, "NEW", StringComparison.OrdinalIgnoreCase) && (!model.HourlyRate.HasValue || model.HourlyRate.Value <= 0) && model.RegionalMinimumWage > 0)
            {
                model.HourlyRate = model.RegionalMinimumWage;
            }
        }
        catch
        {
            model.RegionalMinimumWage = 0m;
        }

        return View("Create", model);
    }

    private async Task<IActionResult> ExpiredAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Index), "ManagerContracts") });
    }
}
