using UI_Admin.Services;
using System.Net.Http.Json;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerCategoryApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerCategoryApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration.GetWorkShiftApiBaseUrl();
    }

    public async Task<ManagerCategoryListViewModel> GetPagedAsync(
        string? search,
        string? status = "all",
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"pageNumber={Math.Max(pageNumber, 1)}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        if (normalizedStatus is "active" or "inactive")
            parameters.Add($"status={normalizedStatus.ToUpperInvariant()}");

        var result = await _httpClient.GetFromJsonAsync<CategoryListPagedViewModel>(
            BuildUrl($"api/categories?{string.Join('&', parameters)}"), cancellationToken);

        var paged = result ?? new CategoryListPagedViewModel();

        return new ManagerCategoryListViewModel
        {
            Items = paged.Items.Select(i => new ManagerCategoryListItemViewModel
            {
                CategoryId = i.CategoryId,
                CategoryName = i.CategoryName,
                Description = i.Description,
                Status = i.Status,
                ProductCount = i.ProductCount
            }).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages,
            Search = paged.Search,
            StatusFilter = paged.StatusFilter
        };
    }

    public async Task<ManagerCategoryDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _httpClient.GetFromJsonAsync<CategoryDetailViewModel>(
            BuildUrl($"api/categories/{id}"), cancellationToken);

        if (detail == null) return null;

        return new ManagerCategoryDetailViewModel
        {
            CategoryId = detail.CategoryId,
            CategoryName = detail.CategoryName,
            Description = detail.Description,
            Status = detail.Status,
            ProductCount = detail.ProductCount,
            Products = detail.Products
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(ManagerCreateCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl("api/categories"),
            new { categoryName = model.CategoryName, description = model.Description },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, ManagerEditCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            BuildUrl($"api/categories/{id}"),
            new { categoryName = model.CategoryName, description = model.Description },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(BuildUrl($"api/categories/{id}"), cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl($"api/categories/{id}/restore"),
            new { },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl}/{relativePath}";
}