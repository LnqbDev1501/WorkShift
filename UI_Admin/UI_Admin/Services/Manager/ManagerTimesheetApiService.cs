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

    public async Task<ManagerWeeklyTimesheetDetailViewModel> GenerateWeeklyTimesheetAsync(
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/{scheduleWeekId}/generate";
        var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        var result = await response.Content.ReadFromJsonAsync<ManagerWeeklyTimesheetDetailViewModel>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to read timesheet details after generation.");
    }

    public async Task<ManagerWeeklyTimesheetDetailViewModel> LockWeeklyTimesheetAsync(
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/{scheduleWeekId}/lock";
        var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        var result = await response.Content.ReadFromJsonAsync<ManagerWeeklyTimesheetDetailViewModel>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to read timesheet details after locking.");
    }

    public async Task<(byte[] FileBytes, string FileName, string ContentType)> DownloadWeeklyTimesheetExcelAsync(
        int scheduleWeekId,
        CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl.TrimEnd('/')}/api/manager/timesheets/{scheduleWeekId}/export-excel";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
            throw new InvalidOperationException(errorMessage);
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"Weekly_Timesheet_{scheduleWeekId}.xlsx";
        string contentType = response.Content.Headers.ContentType?.MediaType
            ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        return (fileBytes, fileName, contentType);
    }
}

