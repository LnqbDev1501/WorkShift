using System.Net.Http.Json;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services;

public class ManagerContractListResponse
{
    public List<ManagerContractListItemViewModel> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class ManagerContractApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');

    public Task<IReadOnlyList<ContractStaffOption>> GetStaffOptionsAsync(CancellationToken ct) =>
        GetStaffOptionsAsync(null, ct);

    public async Task<IReadOnlyList<ContractStaffOption>> GetStaffOptionsAsync(string? mode, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(mode)
            ? $"{baseUrl}/api/manager/contracts/staff-options"
            : $"{baseUrl}/api/manager/contracts/staff-options?mode={Uri.EscapeDataString(mode.Trim())}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<ContractStaffOption>>(cancellationToken: ct) ?? [];
    }

    public async Task<ContractCreatedResponse> CreateAsync(CreateContractViewModel model, CancellationToken ct)
    {
        // Explicit allowlist: never serialize the view model or dropdown data as the API request.
        object payload;
        if (string.Equals(model.ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase) ||
            (model.ReplacesContractId.HasValue && model.ReplacesContractId.Value > 0))
        {
            payload = new
            {
                staffUserId = model.StaffUserId,
                startDate = model.StartDate,
                endDate = model.EndDate,
                employeePosition = model.EmployeePosition.Trim(),
                contractTerms = string.IsNullOrWhiteSpace(model.ContractTerms) ? null : model.ContractTerms.Trim(),
                contractType = "RENEWAL",
                replacesContractId = model.ReplacesContractId
            };
        }
        else
        {
            payload = new
            {
                staffUserId = model.StaffUserId,
                startDate = model.StartDate,
                endDate = model.EndDate,
                employeePosition = model.EmployeePosition.Trim(),
                contractTerms = string.IsNullOrWhiteSpace(model.ContractTerms) ? null : model.ContractTerms.Trim()
            };
        }

        using var response = await client.PostAsJsonAsync($"{baseUrl}/api/manager/contracts", payload, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ContractCreatedResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read contract creation result. Please check before trying again.");
    }

    public async Task<ManagerContractListResponse> GetPagedAsync(
        string? search,
        string? status,
        Guid? staffUserId,
        int pageNumber,
        int pageSize,
        CancellationToken ct)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

        if (staffUserId.HasValue && staffUserId.Value != Guid.Empty)
            queryParams.Add($"staffUserId={staffUserId.Value}");

        var url = $"{baseUrl}/api/manager/contracts?{string.Join("&", queryParams)}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerContractListResponse>(cancellationToken: ct)
            ?? new ManagerContractListResponse();
    }

    public async Task<ManagerContractDetailViewModel> GetByIdAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/manager/contracts/{id}", ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerContractDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read contract details. Please try again.");
    }

    public async Task<ManagerContractDetailViewModel> UpdateAsync(int id, EditContractViewModel model, CancellationToken ct)
    {
        // Explicit allowlist: never serialize read-only metadata fields to the update endpoint.
        using var response = await client.PutAsJsonAsync($"{baseUrl}/api/manager/contracts/{id}", new
        {
            startDate = model.StartDate,
            endDate = model.EndDate,
            employeePosition = model.EmployeePosition.Trim(),
            contractTerms = string.IsNullOrWhiteSpace(model.ContractTerms) ? null : model.ContractTerms.Trim()
        }, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerContractDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read contract update result. Please try again.");
    }

    public async Task<ManagerContractDetailViewModel> CancelAsync(int id, string reason, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"{baseUrl}/api/manager/contracts/{id}/cancel", new
        {
            reason = reason.Trim()
        }, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerContractDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read contract cancellation result. Please try again.");
    }

    public async Task<(byte[] Bytes, string ContentType)> GetPdfPreviewAsync(int id, CancellationToken ct)
    {
        using var response = await client.GetAsync($"{baseUrl}/api/manager/contracts/{id}/pdf/preview", ct);
        await EnsureSuccessAsync(response, ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (bytes, "application/pdf");
    }

    public async Task<ManagerContractDetailViewModel> SendContractAsync(int id, CancellationToken ct)
    {
        using var response = await client.PostAsync($"{baseUrl}/api/manager/contracts/{id}/send", null, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ManagerContractDetailViewModel>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Failed to read sent contract response.");
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetPdfFileAsync(int id, bool isDownload, CancellationToken ct)
    {
        var endpoint = isDownload ? $"{baseUrl}/api/manager/contracts/{id}/pdf/download" : $"{baseUrl}/api/manager/contracts/{id}/pdf";
        using var response = await client.GetAsync(endpoint, ct);
        await EnsureSuccessAsync(response, ct);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"contract_{id}.pdf";
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (bytes, contentType, fileName);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ApiErrorReader.ReadErrorMessageAsync(response, ct), null, response.StatusCode);
    }
}
