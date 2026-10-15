using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Manager.Attendance;

namespace UI_Admin.Services.Manager;

public sealed class ManagerAttendanceApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerAttendanceApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<ManagerDailyAttendanceViewModel?> GetDailyAttendanceAsync(
        int? scheduleWeekId,
        DateOnly? date,
        string? search,
        string? status,
        int? shiftId,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"pageIndex={pageIndex}",
            $"pageSize={pageSize}"
        };

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
            parameters.Add($"scheduleWeekId={scheduleWeekId.Value}");

        if (date.HasValue)
            parameters.Add($"date={date.Value:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            parameters.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (shiftId.HasValue && shiftId.Value > 0)
            parameters.Add($"shiftId={shiftId.Value}");

        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/attendance?{string.Join('&', parameters)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ManagerDailyAttendanceViewModel>(cancellationToken: cancellationToken);
    }

    public async Task<ManagerAttendanceDetailViewModel?> GetAttendanceDetailAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/attendance/{assignmentId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ManagerAttendanceDetailViewModel>(cancellationToken: cancellationToken);
    }
}
