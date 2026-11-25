using UI_Admin.Services;
using System.Text.Json;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerDashboardApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<ManagerDashboardData> GetAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration.GetWorkShiftApiBaseUrl();

        using var response = await httpClient.GetAsync($"{baseUrl}/api/manager/dashboard", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<ManagerDashboardData>(cancellationToken)
            ?? throw new JsonException("The dashboard API returned an empty response.");
        if (data.TodayAttendance is null || data.TodayAttendance.ByStatus is null || data.PendingRequestsByType is null || data.RecentRequests is null)
            throw new JsonException("The dashboard API returned an invalid response.");
        return data;
    }
}

