using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.Payroll;

namespace UI_Admin.Services;

public class PayrollApiService(HttpClient client, IConfiguration configuration)
{
    private string BaseUrl => (configuration["WorkShiftApi:BaseUrl"]
        ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.")).TrimEnd('/');

    public async Task<PayrollPeriodListResponse> GetPeriodsAsync(PayrollFilter filter, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["search"] = filter.Search, ["status"] = filter.Status,
            ["regionId"] = filter.RegionId?.ToString(), ["branchId"] = filter.BranchId?.ToString(),
            ["pageNumber"] = filter.PageNumber.ToString(), ["pageSize"] = filter.PageSize.ToString()
        };
        return await GetAsync<PayrollPeriodListResponse>(QueryHelpers.AddQueryString("/api/admin/payroll-periods", query), ct);
    }

    public Task<PayrollPeriodDetailResponse> GetDetailAsync(int id, int pageNumber, CancellationToken ct) =>
        GetAsync<PayrollPeriodDetailResponse>($"/api/admin/payroll-periods/{id}?pageNumber={pageNumber}&pageSize=20", ct);

    public async Task<List<PayrollBranch>> GetBranchesAsync(int? regionId, CancellationToken ct)
    {
        var items = new List<PayrollBranch>();
        for (var page = 1; ; page++)
        {
            var result = await GetAsync<LookupPage>($"/api/admin/branches?pageNumber={page}&pageSize=100&regionId={regionId}", ct);
            items.AddRange(result.Items);
            if (page >= result.TotalPages) return items;
        }
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await client.GetAsync(BaseUrl + path, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new HttpRequestException("API returned an empty response.");
    }

    private class LookupPage
    {
        public List<PayrollBranch> Items { get; set; } = [];
        public int TotalPages { get; set; }
    }
}
