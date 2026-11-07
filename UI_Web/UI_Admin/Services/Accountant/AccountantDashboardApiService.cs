using UI_Admin.Services;
using System.Text.Json;
using UI_Admin.Models.Accountant;

namespace UI_Admin.Services.Accountant;

public sealed class AccountantDashboardApiService(HttpClient httpClient, IConfiguration configuration)
{
    public async Task<AccountantDashboardData> GetAsync(CancellationToken cancellationToken)
    {
        var baseUrl = configuration.GetWorkShiftApiBaseUrl();

        using var response = await httpClient.GetAsync($"{baseUrl}/api/accountant/dashboard", cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<AccountantDashboardData>(cancellationToken)
            ?? throw new JsonException("The dashboard API returned an empty response.");
        if (data.PayrollPeriodsByStatus is null || data.PayrollsByStatus is null || data.RecentPayrollPeriods is null || data.MonthlySalaries is null)
            throw new JsonException("The dashboard API returned an invalid response.");
        return data;
    }
}
