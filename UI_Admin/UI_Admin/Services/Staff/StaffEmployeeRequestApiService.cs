using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Staff.Requests;

namespace UI_Admin.Services.Staff;

public class StaffEmployeeRequestApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StaffEmployeeRequestApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    /// <summary>
    /// GET /api/staff/requests — retrieves paged list of employee requests of the current staff.
    /// </summary>
    public async Task<StaffEmployeeRequestListViewModel> GetPagedAsync(
        string? reason = null,
        string? requestType = null,
        string? status = null,
        string? fromDate = null,
        string? toDate = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new StringBuilder();
        query.Append($"pageNumber={Math.Max(1, pageNumber)}&pageSize={Math.Clamp(pageSize, 1, 100)}");

        if (!string.IsNullOrWhiteSpace(reason))
        {
            query.Append($"&reason={Uri.EscapeDataString(reason.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(requestType) && !requestType.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query.Append($"&requestType={Uri.EscapeDataString(requestType.Trim().ToUpperInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            query.Append($"&status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}");
        }

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            query.Append($"&fromDate={Uri.EscapeDataString(fromDate.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            query.Append($"&toDate={Uri.EscapeDataString(toDate.Trim())}");
        }

        var url = $"{_apiBaseUrl}/api/staff/requests?{query}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException("Session expired or token invalid.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException("Staff access required.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
                return new StaffEmployeeRequestListViewModel
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Reason = reason,
                    RequestType = requestType,
                    Status = status,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ErrorMessage = errorMsg ?? "Không thể tải danh sách đơn của bạn."
                };
            }

            var paged = await response.Content.ReadFromJsonAsync<PagedResult<StaffEmployeeRequestListItemViewModel>>(
                JsonOptions,
                cancellationToken);

            return new StaffEmployeeRequestListViewModel
            {
                Items = paged?.Items ?? new List<StaffEmployeeRequestListItemViewModel>(),
                PageNumber = paged?.PageNumber ?? pageNumber,
                PageSize = paged?.PageSize ?? pageSize,
                TotalItems = paged?.TotalItems ?? 0,
                Reason = reason,
                RequestType = requestType,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            };
        }
        catch (HttpRequestException ex)
        {
            return new StaffEmployeeRequestListViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Reason = reason,
                RequestType = requestType,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                ErrorMessage = $"Lỗi kết nối máy chủ: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// GET /api/staff/requests/{requestId} — retrieves detail of a single request.
    /// </summary>
    public async Task<StaffEmployeeRequestDetailViewModel?> GetByIdAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/{requestId}";

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
            throw new UnauthorizedAccessException("Staff access required.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMsg ?? "Không thể tải thông tin chi tiết đơn.");
        }

        return await response.Content.ReadFromJsonAsync<StaffEmployeeRequestDetailViewModel>(
            JsonOptions,
            cancellationToken);
    }

    /// <summary>
    /// GET /api/staff/requests/eligible-shifts?requestType={requestType}
    /// </summary>
    public async Task<IReadOnlyList<StaffEligibleShiftViewModel>> GetEligibleShiftsAsync(
        string requestType,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/eligible-shifts?requestType={Uri.EscapeDataString(requestType)}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<StaffEligibleShiftViewModel>();
            }

            var list = await response.Content.ReadFromJsonAsync<List<StaffEligibleShiftViewModel>>(
                JsonOptions,
                cancellationToken);

            return list ?? new List<StaffEligibleShiftViewModel>();
        }
        catch
        {
            return Array.Empty<StaffEligibleShiftViewModel>();
        }
    }

    /// <summary>
    /// POST /api/staff/requests/leave — submits a leave request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SubmitLeaveAsync(
        List<int> assignmentIds,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/leave";
        var payload = new
        {
            assignmentIds,
            reason = reason.Trim()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Gửi đơn xin nghỉ phép thất bại.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Lỗi kết nối máy chủ: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/staff/requests/late-arrival — submits a late arrival request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SubmitLateArrivalAsync(
        int assignmentId,
        DateTimeOffset requestedTime,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/late-arrival";
        var payload = new
        {
            assignmentId,
            requestedTime = requestedTime.ToUniversalTime(),
            reason = reason.Trim()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Gửi đơn xin đi trễ thất bại.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Lỗi kết nối máy chủ: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/staff/requests/early-leave — submits an early leave request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SubmitEarlyLeaveAsync(
        int assignmentId,
        DateTimeOffset requestedTime,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/early-leave";
        var payload = new
        {
            assignmentId,
            requestedTime = requestedTime.ToUniversalTime(),
            reason = reason.Trim()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Gửi đơn xin về sớm thất bại.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Lỗi kết nối máy chủ: {ex.Message}");
        }
    }

    /// <summary>
    /// POST /api/staff/requests/{requestId}/cancel — cancels a pending request.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> CancelAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/staff/requests/{requestId}/cancel";

        try
        {
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorMsg = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            return (false, errorMsg ?? "Hủy đơn yêu cầu thất bại.");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Lỗi kết nối máy chủ: {ex.Message}");
        }
    }

    private class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }
}
