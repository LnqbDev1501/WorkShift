using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models.Admin.Payroll;

namespace UI_Admin.Services.Admin;

public class PayrollApiService(HttpClient client, IConfiguration configuration)
{
    private string BaseUrl => (configuration["WorkShiftApi:BaseUrl"]
        ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.")).TrimEnd('/');

    public async Task<PayrollPeriodListResponse> GetPeriodsAsync(PayrollFilter filter, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["search"] = filter.Search,
            ["status"] = filter.Status,
            ["regionId"] = filter.RegionId?.ToString(),
            ["branchId"] = filter.BranchId?.ToString(),
            ["startDateFrom"] = filter.StartDateFrom?.ToString("yyyy-MM-dd"),
            ["startDateTo"] = filter.StartDateTo?.ToString("yyyy-MM-dd"),
            ["pageNumber"] = filter.PageNumber.ToString(),
            ["pageSize"] = filter.PageSize.ToString()
        };
        return await GetAsync<PayrollPeriodListResponse>(QueryHelpers.AddQueryString("/api/admin/payroll-periods", query), ct);
    }

    public Task<PayrollPeriodDetailResponse> GetDetailAsync(int id, PayrollDetailQuery query, CancellationToken ct) =>
        GetAsync<PayrollPeriodDetailResponse>(QueryHelpers.AddQueryString($"/api/admin/payroll-periods/{id}",
            new Dictionary<string, string?>
            {
                ["search"] = query.Search,
                ["pageNumber"] = query.PageNumber.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            }), ct);

    public Task ApproveAsync(int id, CancellationToken ct) =>
        SendReviewAsync($"/api/admin/payroll-periods/{id}/approve", null, ct);

    private async Task SendReviewAsync(string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Conflict)
        {
            var error = await response.Content.ReadFromJsonAsync<ReviewError>(cancellationToken: ct);
            throw new InvalidOperationException(error?.Message ?? "Cannot perform operation in the current state.");
        }
        response.EnsureSuccessStatusCode();
    }

    private class ReviewError { public string? Message { get; set; } }

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
