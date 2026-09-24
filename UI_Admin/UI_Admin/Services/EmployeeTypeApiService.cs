using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.EmployeeTypes;

namespace UI_Admin.Services;

public class EmployeeTypeApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public EmployeeTypeApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/') + "/api/admin/employee-types";
    }

    public async Task<EmployeeTypeListPagedViewModel> GetPagedAsync(
        string? search = null, string? status = "all", int pageNumber = 1, int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var statusFilter = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
        var apiStatus = statusFilter == "all" ? string.Empty : statusFilter.ToUpperInvariant();
        using var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl}?search={Uri.EscapeDataString(search ?? string.Empty)}&status={Uri.EscapeDataString(apiStatus)}&pageNumber={pageNumber}&pageSize={pageSize}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var model = await response.Content.ReadFromJsonAsync<EmployeeTypeListPagedViewModel>(cancellationToken: cancellationToken)
            ?? throw new HttpRequestException("The server returned an invalid response.");
        model.Search = search;
        model.StatusFilter = statusFilter;
        return model;
    }

    public async Task<EmployeeTypeDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{_apiBaseUrl}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<EmployeeTypeDetailViewModel>(cancellationToken: cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(CreateEmployeeTypeViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl, CreatePayload(model), cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, EditEmployeeTypeViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{id}", CreatePayload(model), cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/{id}", cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync($"{_apiBaseUrl}/{id}/restore", null, cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    private static object CreatePayload(EmployeeTypeFormViewModel model) => new
    {
        typeName = model.TypeName.Trim(),
        description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()

    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await ReadErrorMessageAsync(response, cancellationToken), null, response.StatusCode);
    }

    private static async Task<(bool Success, string? ErrorMessage)> ReadResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadErrorMessageAsync(response, cancellationToken));
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if ((int)response.StatusCode < 500)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
                if (problem?.Errors.Count > 0) return string.Join(" ", problem.Errors.Values.SelectMany(x => x));
            }
            catch (JsonException) { }
            catch (NotSupportedException) { }
        }
        return "Unable to complete the request. Please try again.";
    }
}

