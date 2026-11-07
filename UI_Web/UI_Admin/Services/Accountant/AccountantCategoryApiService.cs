using UI_Admin.Services;
using System.Net.Http.Json;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Models.Accountant;

namespace UI_Admin.Services.Accountant;

public sealed class AccountantCategoryApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public AccountantCategoryApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration.GetWorkShiftApiBaseUrl();
    }

    public async Task<AccountantCategoryListViewModel> GetPagedAsync(
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

        return new AccountantCategoryListViewModel
        {
            Items = paged.Items.Select(i => new AccountantCategoryListItemViewModel
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

    public async Task<AccountantCategoryDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _httpClient.GetFromJsonAsync<CategoryDetailViewModel>(
            BuildUrl($"api/categories/{id}"), cancellationToken);

        if (detail == null) return null;

        return new AccountantCategoryDetailViewModel
        {
            CategoryId = detail.CategoryId,
            CategoryName = detail.CategoryName,
            Description = detail.Description,
            Status = detail.Status,
            ProductCount = detail.ProductCount,
            Products = detail.Products
        };
    }

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl}/{relativePath}";
}