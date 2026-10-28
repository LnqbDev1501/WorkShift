using UI_Admin.Models.Accountant;

namespace UI_Admin.Services.Accountant;

public class AccountantOrderListResponse
{
    public List<AccountantOrderListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class AccountantOrderApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');

    public async Task<AccountantOrderListResponse> GetPagedAsync(
        string? search, int? branchId, string? status, string? paymentStatus,
        DateTime? dateFrom, DateTime? dateTo,
        int pageNumber, int pageSize, CancellationToken ct)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (branchId.HasValue && branchId.Value > 0) queryParams.Add($"branchId={branchId.Value}");
        if (!string.IsNullOrWhiteSpace(status) && status != "all") queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (!string.IsNullOrWhiteSpace(paymentStatus) && paymentStatus != "all") queryParams.Add($"paymentStatus={Uri.EscapeDataString(paymentStatus.Trim())}");
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var url = $"{baseUrl}/api/accountant/orders?{string.Join("&", queryParams)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AccountantOrderListResponse>(cancellationToken: ct) ?? new AccountantOrderListResponse();
    }

    public async Task<AccountantOrderDetailViewModel> GetByIdAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/accountant/orders/{id}", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AccountantOrderDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read order details.");
    }

    public async Task<byte[]> GetInvoicePdfAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/accountant/orders/{id}/invoice", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportBranchSalesReportAsync(int? branchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var queryParams = new List<string>();
        if (branchId.HasValue) queryParams.Add($"branchId={branchId.Value}");
        if (dateFrom.HasValue) queryParams.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
        if (dateTo.HasValue) queryParams.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var url = $"{baseUrl}/api/accountant/reports/branch-sales{(queryParams.Any() ? "?" + string.Join("&", queryParams) : "")}";
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
