using UI_Admin.Services;
using System.Net.Http.Json;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Models.Accountant;

namespace UI_Admin.Services.Accountant;

public sealed class AccountantProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public AccountantProductApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration.GetWorkShiftApiBaseUrl();
    }

    public async Task<AccountantProductListViewModel> GetPagedAsync(
        string? search,
        int? branchId,
        int? categoryId,
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

        if (branchId is > 0)
            parameters.Add($"branchId={branchId.Value}");

        if (categoryId is > 0)
            parameters.Add($"categoryId={categoryId.Value}");

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        if (normalizedStatus is "active" or "inactive")
            parameters.Add($"status={normalizedStatus.ToUpperInvariant()}");

        var result = await _httpClient.GetFromJsonAsync<ProductListPagedViewModel>(
            BuildUrl($"api/products?{string.Join('&', parameters)}"), cancellationToken);

        var paged = result ?? new ProductListPagedViewModel();

        return new AccountantProductListViewModel
        {
            Items = paged.Items.Select(i => new AccountantProductListItemViewModel
            {
                ProductId = i.ProductId,
                BranchId = i.BranchId,
                BranchName = i.BranchName,
                CategoryId = i.CategoryId,
                CategoryName = i.CategoryName,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Price = i.Price,
                Status = i.Status
            }).ToList(),
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages,
            Search = paged.Search,
            BranchId = paged.BranchId,
            CategoryId = paged.CategoryId,
            StatusFilter = paged.StatusFilter,
            Categories = await GetActiveCategoriesAsync(cancellationToken),
            Branches = paged.Branches
        };
    }

    public async Task<AccountantProductDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _httpClient.GetFromJsonAsync<ProductDetailViewModel>(
            BuildUrl($"api/products/{id}"), cancellationToken);

        if (detail == null) return null;

        return new AccountantProductDetailViewModel
        {
            ProductId = detail.ProductId,
            BranchId = detail.BranchId,
            BranchName = detail.BranchName,
            CategoryId = detail.CategoryId,
            CategoryName = detail.CategoryName,
            ProductCode = detail.ProductCode,
            ProductName = detail.ProductName,
            Price = detail.Price,
            Description = detail.Description,
            ImageUrl = detail.ImageUrl,
            Status = detail.Status
        };
    }

    public async Task<List<CategoryOptionViewModel>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<CategoryListPagedViewModel>(
                BuildUrl("api/categories?status=ACTIVE&pageNumber=1&pageSize=100"), cancellationToken);

            return result?.Items
                .Select(item => new CategoryOptionViewModel { Id = item.CategoryId, Name = item.CategoryName })
                .OrderBy(option => option.Name)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl}/{relativePath}";
}