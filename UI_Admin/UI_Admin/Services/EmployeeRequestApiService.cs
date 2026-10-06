using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.EmployeeRequests;

namespace UI_Admin.Services;

public class EmployeeRequestApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public EmployeeRequestApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    /// <summary>
    /// GET /api/admin/requests — retrieves paged list of employee requests.
    /// </summary>
    public async Task<AdminEmployeeRequestListViewModel> GetPagedAsync(
        string? search = null,
        string? requestType = null,
        string? status = null,
        int? branchId = null,
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

        if (branchId.HasValue && branchId.Value > 0)
        {
            query.Append($"&branchId={branchId.Value}");
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

        var url = $"{_apiBaseUrl}/api/admin/requests?{query}";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
                return new AdminEmployeeRequestListViewModel
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Search = search,
                    RequestType = requestType,
                    Status = status,
                    BranchId = branchId,
                    UserId = userId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ErrorMessage = errorMsg
                };
            }

            var paged = await response.Content.ReadFromJsonAsync<PagedApiResponse<AdminEmployeeRequestListItemViewModel>>(JsonOptions, cancellationToken);

            return new AdminEmployeeRequestListViewModel
            {
                Items = paged?.Items ?? [],
                PageNumber = paged?.PageNumber ?? pageNumber,
                PageSize = paged?.PageSize ?? pageSize,
                TotalItems = paged?.TotalItems ?? 0,
                TotalPages = paged?.TotalPages ?? 0,
                Search = search,
                RequestType = requestType,
                Status = status,
                BranchId = branchId,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };
        }
        catch (HttpRequestException ex)
        {
            return new AdminEmployeeRequestListViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                RequestType = requestType,
                Status = status,
                BranchId = branchId,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                ErrorMessage = $"Unable to connect to the server: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// GET /api/admin/requests/{requestId} — retrieves request details.
    /// </summary>
    public async Task<AdminEmployeeRequestDetailViewModel?> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/requests/{requestId}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Your session has expired. Please log in again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMsg);
        }

        return await response.Content.ReadFromJsonAsync<AdminEmployeeRequestDetailViewModel>(JsonOptions, cancellationToken);
    }

    /// <summary>
    /// GET /api/admin/branches?pageSize=100&status=ACTIVE — retrieves active branches for dropdown.
    /// </summary>
    public async Task<List<BranchFilterOptionViewModel>> GetBranchesAsync(
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches?pageSize=100&status=ACTIVE";

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<BranchApiItem>>(JsonOptions, cancellationToken);
            return result?.Items?.ConvertAll(b => new BranchFilterOptionViewModel
            {
                BranchId = b.BranchId,
                BranchName = b.BranchName,
                BranchCode = b.BranchCode
            }) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private class PagedApiResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    private class BranchApiItem
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
    }
}
