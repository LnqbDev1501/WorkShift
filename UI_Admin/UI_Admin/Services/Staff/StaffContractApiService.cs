using UI_Admin.Services;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Staff.Contract;

namespace UI_Admin.Services.Staff;

public class StaffContractApiService(HttpClient client, IConfiguration configuration)
{
    private readonly string baseUrl = configuration.GetWorkShiftApiBaseUrl();

    public async Task<IReadOnlyList<StaffContractListItemViewModel>> GetMyContractsAsync(
        string? status = null,
        string? search = null,
        string? startDateFrom = null,
        string? startDateTo = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (!string.IsNullOrWhiteSpace(startDateFrom))
            queryParams.Add($"startDateFrom={Uri.EscapeDataString(startDateFrom.Trim())}");
        if (!string.IsNullOrWhiteSpace(startDateTo))
            queryParams.Add($"startDateTo={Uri.EscapeDataString(startDateTo.Trim())}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"{baseUrl}/api/staff/contracts{queryString}";

        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        return await response.Content.ReadFromJsonAsync<List<StaffContractListItemViewModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<StaffContractDetailViewModel> GetMyContractByIdAsync(int id, CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/contracts/{id}";
        using var response = await client.GetAsync(url, ct);
        await EnsureSuccessAsync(response, ct);

        var data = await response.Content.ReadFromJsonAsync<StaffContractDetailViewModel>(cancellationToken: ct);
        return data ?? throw new InvalidOperationException("Unable to read contract details.");
    }

    public async Task<StaffContractDetailViewModel> AcceptContractAsync(int id, CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/contracts/{id}/accept";
        using var response = await client.PostAsync(url, null, ct);
        await EnsureSuccessAsync(response, ct);

        var data = await response.Content.ReadFromJsonAsync<StaffContractDetailViewModel>(cancellationToken: ct);
        return data ?? throw new InvalidOperationException("Unable to read the contract acceptance result.");
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> GetStaffPdfFileAsync(
        int id,
        bool isDownload,
        CancellationToken ct = default)
    {
        var endpoint = isDownload
            ? $"{baseUrl}/api/staff/contracts/{id}/pdf/download"
            : $"{baseUrl}/api/staff/contracts/{id}/pdf";

        using var response = await client.GetAsync(endpoint, ct);
        await EnsureSuccessAsync(response, ct);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"contract_{id}.pdf";
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        return (bytes, contentType, fileName);
    }

    public async Task<StaffContractChangeRequestViewModel> SubmitChangeRequestAsync(
        int id,
        SubmitStaffContractChangeRequestViewModel model,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/contracts/{id}/change-requests";

        var payload = new
        {
            requestType = string.IsNullOrWhiteSpace(model.RequestType) ? "CHANGE_REQUEST" : model.RequestType,
            staffComment = string.IsNullOrWhiteSpace(model.StaffComment) ? null : model.StaffComment.Trim(),
            items = new[]
            {
                new
                {
                    fieldName = model.FieldName.Trim(),
                    proposedValue = model.ProposedValue.Trim(),
                    reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim()
                }
            }
        };

        using var response = await client.PostAsJsonAsync(url, payload, ct);
        await EnsureSuccessAsync(response, ct);

        var result = await response.Content.ReadFromJsonAsync<StaffContractChangeRequestViewModel>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Unable to read the change request response.");
    }

    public async Task<IReadOnlyList<StaffContractChangeRequestViewModel>> GetMyContractChangeRequestsAsync(
        int id,
        CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/contracts/{id}/change-requests";
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<StaffContractChangeRequestViewModel>>(cancellationToken: ct) ?? [];
    }

    public async Task<List<string>> GetActivePositionOptionsAsync(CancellationToken ct = default)
    {
        var url = $"{baseUrl}/api/staff/contracts/active-positions";
        using var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return [];

        return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct) ?? [];
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
