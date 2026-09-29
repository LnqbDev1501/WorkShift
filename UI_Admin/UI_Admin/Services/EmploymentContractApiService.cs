using System.Net.Http.Json;
using System.Text;
using UI_Admin.Models.EmploymentContracts;

namespace UI_Admin.Services;

public class EmploymentContractApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public EmploymentContractApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    /// <summary>
    /// GET /api/admin/contracts — returns paged list with optional filters.
    /// </summary>
    public async Task<EmploymentContractPagedResponse> GetPagedAsync(
        string? search = null,
        string? status = null,
        int? branchId = null,
        int? regionId = null,
        string? startDateFrom = null,
        string? startDateTo = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new StringBuilder();
        query.Append($"pageNumber={pageNumber}&pageSize={pageSize}");

        if (!string.IsNullOrWhiteSpace(search))
            query.Append($"&search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query.Append($"&status={Uri.EscapeDataString(status.Trim().ToUpperInvariant())}");

        if (branchId.HasValue && branchId.Value > 0)
            query.Append($"&branchId={branchId.Value}");

        if (regionId.HasValue && regionId.Value > 0)
            query.Append($"&regionId={regionId.Value}");

        if (!string.IsNullOrWhiteSpace(startDateFrom))
            query.Append($"&startDateFrom={Uri.EscapeDataString(startDateFrom.Trim())}");

        if (!string.IsNullOrWhiteSpace(startDateTo))
            query.Append($"&startDateTo={Uri.EscapeDataString(startDateTo.Trim())}");

        var url = $"{_apiBaseUrl}/api/admin/contracts?{query}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new EmploymentContractPagedResponse();

        var paged = await response.Content.ReadFromJsonAsync<EmploymentContractPagedResponse>(cancellationToken: cancellationToken);
        return paged ?? new EmploymentContractPagedResponse();
    }

    /// <summary>
    /// GET /api/admin/contracts/{contractId} — returns contract detail or null on 404.
    /// </summary>
    public async Task<EmploymentContractDetailViewModel?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/contracts/{contractId}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<EmploymentContractDetailViewModel>(cancellationToken: cancellationToken);
    }
}
