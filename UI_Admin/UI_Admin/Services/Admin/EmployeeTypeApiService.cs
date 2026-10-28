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
        if (model.Items is null) throw new JsonException("The employee type list returned invalid items.");
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

    public async Task<EmployeeTypeApiResult> CreateAsync(EmployeeTypeFormViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(_apiBaseUrl, CreatePayload(model), cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<EmployeeTypeApiResult> UpdateAsync(int id, EditEmployeeTypeViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{id}", CreatePayload(model), cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<EmployeeTypeApiResult> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/{id}", cancellationToken);
        return await ReadResultAsync(response, cancellationToken);
    }

    public async Task<EmployeeTypeApiResult> RestoreAsync(int id, CancellationToken cancellationToken = default)
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
        {
            var result = await ReadResultAsync(response, cancellationToken);
            throw new HttpRequestException(result.ErrorMessage ?? "Unable to complete the request. Please try again.", null, result.StatusCode);
        }
    }

    private static async Task<EmployeeTypeApiResult> ReadResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return new EmployeeTypeApiResult { Success = true, StatusCode = response.StatusCode };

        if ((int)response.StatusCode < 500)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: cancellationToken);
                if (problem is not null)
                {
                    var errors = problem.Errors is null
                        ? new Dictionary<string, string[]>()
                        : new Dictionary<string, string[]>(problem.Errors);
                    return new EmployeeTypeApiResult
                    {
                        StatusCode = response.StatusCode,
                        ErrorMessage = !string.IsNullOrWhiteSpace(problem.Detail) ? problem.Detail
                            : errors.Count == 0 ? "Unable to complete the request. Please try again." : null,
                        FieldErrors = errors
                    };
                }
            }
            catch (JsonException) { }
            catch (NotSupportedException) { }
        }
        return new EmployeeTypeApiResult
        {
            StatusCode = response.StatusCode,
            ErrorMessage = "Unable to complete the request. Please try again."
        };
    }
}

