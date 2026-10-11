using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using UI_Admin.Models;
using UI_Admin.Models.Accountant.SalaryHistory;
namespace UI_Admin.Services.Accountant;

public sealed class SalaryHistoryApiException(HttpStatusCode status, string message) : Exception(message)
{ public HttpStatusCode Status { get; } = status; }

public sealed class SalaryHistoryApiService(HttpClient client, IConfiguration configuration)
{
    private string Endpoint
    {
        get
        {
            var root = configuration["WorkShiftApi:BaseUrl"];
            if (!Uri.TryCreate(root, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
                throw new InvalidOperationException("WorkShiftApi:BaseUrl must be an HTTP or HTTPS URL.");
            return root!.TrimEnd('/') + "/api/Accountant/salary-histories";
        }
    }
    private async Task<T> Read<T>(string url, CancellationToken ct)
    {
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = (int)response.StatusCode >= 500 ? "Unable to load salary history. Please try again." : await ApiErrorReader.ReadErrorMessageAsync(response, ct);
            throw new SalaryHistoryApiException(response.StatusCode, message ?? "Unable to load salary history.");
        }
        return await response.Content.ReadFromJsonAsync<T>(ct) ?? throw new JsonException("Empty salary history response.");
    }
    public async Task<SalaryHistoryListViewModel> GetListAsync(SalaryHistoryQuery q, CancellationToken ct)
    {
        var values = new Dictionary<string, string?>
        {
            ["keyword"] = string.IsNullOrWhiteSpace(q.Keyword) ? null : q.Keyword.Trim(), ["userId"] = q.UserId?.ToString(),
            ["status"] = string.IsNullOrWhiteSpace(q.Status) ? null : q.Status, ["changeType"] = string.IsNullOrWhiteSpace(q.ChangeType) ? null : q.ChangeType,
            ["effectiveFrom"] = q.EffectiveFrom?.ToString("yyyy-MM-dd"), ["effectiveTo"] = q.EffectiveTo?.ToString("yyyy-MM-dd"),
            ["page"] = q.Page.ToString(), ["pageSize"] = ListPaginationViewModel.DefaultPageSize.ToString()
        };
        var result = await Read<PagedSalaryHistory>(QueryHelpers.AddQueryString(Endpoint, values), ct);
        if (result.Items == null || result.Items.Any(i => i.Contract == null) || result.TotalItems < 0 || result.PageNumber < 1 || result.PageSize != ListPaginationViewModel.DefaultPageSize)
            throw new JsonException("Invalid salary history response.");
        return new() { Query = q, Items = result.Items, PageNumber = result.PageNumber, PageSize = result.PageSize, TotalItems = result.TotalItems };
    }
    public async Task<SalaryHistoryItem> GetDetailAsync(int id, CancellationToken ct)
    {
        var row = await Read<SalaryHistoryItem>($"{Endpoint}/{id}", ct);
        if (row.SalaryHistoryId != id || row.Contract == null) throw new JsonException("Invalid salary history detail.");
        return row;
    }
    private sealed class PagedSalaryHistory
    {
        public List<SalaryHistoryItem> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }
}
