using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Manager.Timesheet;

namespace UI_Admin.Services.Manager;

public sealed class ManagerTimesheetApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerTimesheetApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<ManagerTimesheetPagedResponseModel<ManagerWeeklyTimesheetItemModel>> GetWeeklyTimesheetsAsync(
        string? search,
        int? branchId,
        int? scheduleWeekId,
        int? month,
        int? year,
        string? status,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"pageIndex={pageIndex}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (branchId.HasValue && branchId.Value > 0)
            parameters.Add($"branchId={branchId.Value}");

        if (scheduleWeekId.HasValue && scheduleWeekId.Value > 0)
            parameters.Add($"scheduleWeekId={scheduleWeekId.Value}");

        if (month.HasValue && month.Value > 0)
            parameters.Add($"month={month.Value}");

        if (year.HasValue && year.Value > 0)
            parameters.Add($"year={year.Value}");

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            parameters.Add($"status={Uri.EscapeDataString(status.Trim())}");

        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets?{string.Join('&', parameters)}";

        var result = await _httpClient.GetFromJsonAsync<ManagerTimesheetPagedResponseModel<ManagerWeeklyTimesheetItemModel>>(
            url,
            cancellationToken);

        return result ?? new ManagerTimesheetPagedResponseModel<ManagerWeeklyTimesheetItemModel>();
    }

    public async Task<ManagerWeeklyTimesheetDetailViewModel?> GetWeeklyTimesheetDetailAsync(
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/{scheduleWeekId}";
        try
        {
            return await _httpClient.GetFromJsonAsync<ManagerWeeklyTimesheetDetailViewModel>(
                url,
                cancellationToken);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
