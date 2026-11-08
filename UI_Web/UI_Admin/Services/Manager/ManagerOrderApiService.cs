using UI_Admin.Services;
using System.Net.Http.Json;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services;

public class ManagerOrderListResponse
{
    public List<ManagerOrderListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class ManagerOrderApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = configuration.GetWorkShiftApiBaseUrl();

    public async Task<ManagerOrderListResponse> GetPagedAsync(
        string? search, string? status, string? paymentStatus,
        DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(status) && status != "all") queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (!string.IsNullOrWhiteSpace(paymentStatus) && paymentStatus != "all") queryParams.Add($"paymentStatus={Uri.EscapeDataString(paymentStatus.Trim())}");
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var url = $"{baseUrl}/api/manager/orders?{string.Join("&", queryParams)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerOrderListResponse>(cancellationToken: ct) ?? new ManagerOrderListResponse();
    }

    public async Task<ManagerOrderDetailViewModel> GetByIdAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/manager/orders/{id}", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerOrderDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read order details.");
    }

    public async Task<byte[]> GetInvoicePdfAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/manager/orders/{id}/invoice", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task CompleteOrderAsync(int id, CancellationToken ct)
    {
        using var response = await client.PostAsync($"{baseUrl}/api/manager/orders/{id}/complete", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task CancelOrderAsync(int id, CancellationToken ct)
    {
        using var response = await client.PostAsync($"{baseUrl}/api/manager/orders/{id}/cancel", null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
    }
}
