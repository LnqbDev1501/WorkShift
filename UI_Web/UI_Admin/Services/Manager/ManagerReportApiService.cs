using System.Net.Http.Json;
using UI_Admin.Models.Accountant;
using UI_Admin.Services;

namespace UI_Admin.Services.Manager;

public class ManagerReportApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = configuration.GetWorkShiftApiBaseUrl();

    public async Task<BranchSalesSummaryModel> GetBranchSalesSummaryAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var url = $"{baseUrl}/api/manager/reports/branch-sales/summary{queryString}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<BranchSalesSummaryModel>(cancellationToken: ct)
            ?? new BranchSalesSummaryModel();
    }

    public async Task<byte[]> ExportBranchSalesReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var url = $"{baseUrl}/api/manager/reports/branch-sales{queryString}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
    }
}
