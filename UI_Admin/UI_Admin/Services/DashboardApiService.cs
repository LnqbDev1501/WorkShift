using System.Text.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

public sealed class DashboardApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<DashboardDataViewModel> GetAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["WorkShiftApi:BaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("WorkShiftApi:BaseUrl must be a valid HTTP or HTTPS URL.");

        using var response = await httpClient.GetAsync($"{baseUrl!.TrimEnd('/')}/api/admin/dashboard", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<DashboardDataViewModel>(cancellationToken)
            ?? throw new JsonException("The dashboard API returned an empty response.");
        if (data.TodayAttendance is null || data.TodayAttendance.ByStatus is null || data.RecentPayrollPeriods is null)
            throw new JsonException("The dashboard API returned an invalid response.");
        return data;
    }
}
