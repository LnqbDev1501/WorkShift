using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class DashboardApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<DashboardDataViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration.GetWorkShiftApiBaseUrl();

        using var response = await httpClient.GetAsync($"{baseUrl}/api/admin/dashboard", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<DashboardDataViewModel>(cancellationToken)
            ?? throw new JsonException("The dashboard API returned an empty response.");
        if (data.TodayAttendance is null || data.TodayAttendance.ByStatus is null || data.RecentPayrollPeriods is null)
            throw new JsonException("The dashboard API returned an invalid response.");
        return data;
    }
}
