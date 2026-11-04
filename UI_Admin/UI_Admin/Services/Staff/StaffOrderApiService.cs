using UI_Admin.Services;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Staff.Orders;

namespace UI_Admin.Services.Staff;

public sealed class StaffOrderApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string _baseUrl = configuration.GetWorkShiftApiBaseUrl();

    public async Task<StaffOrderListViewModel> GetMyOrdersAsync(
        string? search,
        string? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var parameters = new List<string>
        {
            $"pageNumber={Math.Max(pageNumber, 1)}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status))
            parameters.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (dateFrom.HasValue)
            parameters.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");

        if (dateTo.HasValue)
            parameters.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");

        var url = $"{_baseUrl}/api/staff/orders?{string.Join("&", parameters)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<StaffOrderListResponse>(cancellationToken: ct);
        return result?.ToViewModel() ?? new StaffOrderListViewModel();
    }

    public async Task<StaffOrderDetailViewModel?> GetOrderAsync(int id, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/staff/orders/{id}";
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<StaffOrderDetailResponse>(cancellationToken: ct);
        return result?.ToViewModel();
    }

    public async Task<StaffOrderDetailViewModel> CreateOrderAsync(StaffCreateOrderViewModel model, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/staff/orders";
        using var response = await client.PostAsJsonAsync(url, model.ToCreateRequest(), ct);
        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<StaffOrderDetailResponse>(cancellationToken: ct);
        return result?.ToViewModel() ?? throw new InvalidOperationException("Failed to create order.");
    }

    public async Task<bool> CompleteOrderAsync(int id, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/staff/orders/{id}/complete";
        using var response = await client.PostAsync(url, null, ct);
        await EnsureSuccessAsync(response, ct);
        return true;
    }

    public async Task<bool> CancelOrderAsync(int id, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/staff/orders/{id}/cancel";
        using var response = await client.PostAsync(url, null, ct);
        await EnsureSuccessAsync(response, ct);
        return true;
    }

    public async Task<List<StaffProductOptionViewModel>> GetActiveProductsAsync(CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/api/staff/products?pageNumber=1&pageSize=100&status=ACTIVE";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<StaffProductListResponse>(cancellationToken: ct);
        return result?.Items ?? [];
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ApiErrorReader.ReadErrorMessageAsync(response, ct);
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }
}