using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Manager.TimesheetAdjustment;

namespace UI_Admin.Services.Manager;

public sealed class ManagerTimesheetAdjustmentApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerTimesheetAdjustmentApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<ManagerTimesheetAdjustmentListViewModel?> GetAdjustmentsAsync(
        string? search,
        string? status,
        int? scheduleWeekId,
        int pageIndex = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"pageIndex={pageIndex}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            parameters.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
            parameters.Add($"scheduleWeekId={scheduleWeekId.Value}");

        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/adjustments?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ManagerTimesheetAdjustmentListViewModel>(cancellationToken: cancellationToken);
    }

    public async Task<ManagerTimesheetAdjustmentItemModel?> GetAdjustmentDetailAsync(
        int adjustmentRequestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/adjustments/{adjustmentRequestId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ManagerTimesheetAdjustmentItemModel>(cancellationToken: cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> ApproveAdjustmentAsync(
        int adjustmentRequestId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/adjustments/{adjustmentRequestId}/approve";
        var response = await _httpClient.PostAsync(url, null, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var errorObj = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
        string? msg = null;
        if (errorObj != null)
        {
            if (errorObj.TryGetValue("detail", out var detailVal) && detailVal != null && !string.IsNullOrWhiteSpace(detailVal.ToString()))
            {
                msg = detailVal.ToString();
            }
            else if (errorObj.TryGetValue("message", out var val) && val != null)
            {
                msg = val.ToString();
            }
        }
        msg ??= $"Failed to approve adjustment (HTTP {response.StatusCode})";

        return (false, msg);
    }

    public async Task<(bool Success, string? ErrorMessage)> RejectAdjustmentAsync(
        int adjustmentRequestId,
        string managerNote,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/adjustments/{adjustmentRequestId}/reject";
        var payload = new { managerNote };
        var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var errorObj = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>(cancellationToken: cancellationToken);
        string? msg = null;
        if (errorObj != null)
        {
            if (errorObj.TryGetValue("detail", out var detailVal) && detailVal != null && !string.IsNullOrWhiteSpace(detailVal.ToString()))
            {
                msg = detailVal.ToString();
            }
            else if (errorObj.TryGetValue("message", out var val) && val != null)
            {
                msg = val.ToString();
            }
        }
        msg ??= $"Failed to reject adjustment (HTTP {response.StatusCode})";

        return (false, msg);
    }
}
