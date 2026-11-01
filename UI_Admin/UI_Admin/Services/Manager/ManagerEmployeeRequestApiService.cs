using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Manager.Requests;

namespace UI_Admin.Services.Manager;

public class ManagerEmployeeRequestApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ManagerEmployeeRequestApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    /// <summary>
    /// GET /api/manager/requests — retrieves paged list of employee requests in manager's branch.
    /// </summary>
    public async Task<ManagerEmployeeRequestListViewModel> GetPagedAsync(
        string? search = null,
        string? requestType = null,
        string? status = null,
        Guid? userId = null,
        string? fromDate = null,
        string? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new StringBuilder();
        query.Append($"pageNumber={Math.Max(1, pageNumber)}&pageSize={Math.Clamp(pageSize, 1, 100)}");

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Append($"&search={Uri.EscapeDataString(search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(requestType) && !requestType.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query.Append($"&requestType={Uri.EscapeDataString(requestType.Trim().ToUpperInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query.Append($"&status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}");
        }

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query.Append($"&userId={userId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            query.Append($"&fromDate={Uri.EscapeDataString(fromDate.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            query.Append($"&toDate={Uri.EscapeDataString(toDate.Trim())}");
        }

        var url = $"{_apiBaseUrl}/api/manager/requests?{query}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Session expired or token invalid.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Manager access required.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
                return new ManagerEmployeeRequestListViewModel
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Search = search,
                    RequestType = requestType,
                    Status = status,
                    UserId = userId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ErrorMessage = errorMsg ?? "Unable to load employee requests list."
                };
            }

            var paged = await response.Content.ReadFromJsonAsync<PagedResult<ManagerEmployeeRequestListItemViewModel>>(
                JsonOptions,
                cancellationToken);

            return new ManagerEmployeeRequestListViewModel
            {
                Items = paged?.Items ?? new List<ManagerEmployeeRequestListItemViewModel>(),
                PageNumber = paged?.PageNumber ?? pageNumber,
                PageSize = paged?.PageSize ?? pageSize,
                TotalItems = paged?.TotalItems ?? 0,
                Search = search,
                RequestType = requestType,
                Status = status,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };
        }
        catch (HttpRequestException ex)
        {
            return new ManagerEmployeeRequestListViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                RequestType = requestType,
                Status = status,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                ErrorMessage = $"Server connection error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// GET /api/manager/requests/{requestId} — retrieves details of a single request.
    /// </summary>
    public async Task<ManagerEmployeeRequestDetailViewModel?> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/manager/requests/{requestId}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Session expired or token invalid.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException("Manager access required.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMsg ?? "Unable to load request details.");
        }

        return await response.Content.ReadFromJsonAsync<ManagerEmployeeRequestDetailViewModel>(
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// GET /api/manager/requests/{requestId}/history — retrieves review history / audit logs for the request.
    /// </summary>
    public async Task<(IReadOnlyList<ManagerEmployeeRequestHistoryViewModel> Items, string? ErrorMessage)> GetHistoryAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/manager/requests/{requestId}/history";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ([], "Request not found in your branch.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Session expired or token invalid.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Manager access required.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
                return ([], errorMsg ?? "Unable to load review history.");
            }

            var list = await response.Content.ReadFromJsonAsync<List<ManagerEmployeeRequestHistoryViewModel>>(
                JsonOptions,
                cancellationToken);

            return (list ?? [], null);
        }
        catch (HttpRequestException ex)
        {
            return ([], $"Unable to connect to server: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/manager/requests/{requestId}/approve — approves an employee request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, int StatusCode)> ApproveAsync(
        int requestId,
        string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/manager/requests/{requestId}/approve";
        var payload = new { reviewNote = reviewNote?.Trim() };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, null, (int)response.StatusCode);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Failed to approve employee request.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Server connection error: {ex.Message}", 503);
        }
    }

    /// <summary>
    /// POST /api/manager/requests/{requestId}/reject — rejects an employee request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, int StatusCode)> RejectAsync(
        int requestId,
        string reviewNote,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/manager/requests/{requestId}/reject";
        var payload = new { reviewNote = reviewNote.Trim() };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return (true, null, (int)response.StatusCode);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Failed to reject employee request.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Server connection error: {ex.Message}", 503);
        }
    }

    /// <summary>
    /// GET /api/manager/staff?pageSize=100 — retrieves list of staff in manager's branch for filtering.
    /// </summary>
    public async Task<IReadOnlyList<ManagerStaffFilterOptionViewModel>> GetStaffOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/manager/staff?pageSize=100";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<ManagerStaffFilterOptionViewModel>();
            }

            var paged = await response.Content.ReadFromJsonAsync<StaffPagedResponseDto>(JsonOptions, cancellationToken);
            if (paged?.Items == null)
            {
                return Array.Empty<ManagerStaffFilterOptionViewModel>();
            }

            var result = new List<ManagerStaffFilterOptionViewModel>();
            foreach (var item in paged.Items)
            {
                result.Add(new ManagerStaffFilterOptionViewModel
                {
                    UserId = item.UserId,
                    FullName = item.FullName ?? string.Empty,
                    Email = item.Email ?? string.Empty
                });
            }

            return result;
        }
        catch
        {
            return Array.Empty<ManagerStaffFilterOptionViewModel>();
        }
    }

    private class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }

    private class StaffPagedResponseDto
    {
        public List<StaffItemDto> Items { get; set; } = new();
    }

    private class StaffItemDto
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
