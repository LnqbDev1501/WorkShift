using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;
using UI_Admin.Models.Manager;

namespace UI_Admin.Services.Manager;

public sealed class ManagerProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerProductApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<ManagerProductListViewModel> GetPagedAsync(
        string? search,
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

        // Manager is scoped to their own branch server-side; no branchId filter needed
        if (categoryId is > 0)
            parameters.Add($"categoryId={categoryId.Value}");

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        if (normalizedStatus is "active" or "inactive")
            parameters.Add($"status={normalizedStatus.ToUpperInvariant()}");

        var result = await _httpClient.GetFromJsonAsync<ProductListPagedViewModel>(
            BuildUrl($"api/products?{string.Join('&', parameters)}"), cancellationToken);

        var paged = result ?? new ProductListPagedViewModel();

        return new ManagerProductListViewModel
        {
            Items = paged.Items.Select(i => new ManagerProductListItemViewModel
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
            CategoryId = paged.CategoryId,
            StatusFilter = paged.StatusFilter,
            Categories = await GetActiveCategoriesAsync(cancellationToken)
        };
    }

    public async Task<ManagerProductDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var detail = await _httpClient.GetFromJsonAsync<ProductDetailViewModel>(
            BuildUrl($"api/products/{id}"), cancellationToken);

        if (detail == null) return null;

        return new ManagerProductDetailViewModel
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

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(ManagerCreateProductViewModel model, CancellationToken cancellationToken = default)
    {
        using var content = BuildFormContent(
            model.CategoryId, model.ProductCode, model.ProductName, model.Price,
            model.Description, model.ImageUrl, model.ImageFile);
        using var response = await _httpClient.PostAsync(BuildUrl("api/products"), content, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, ManagerEditProductViewModel model, CancellationToken cancellationToken = default)
    {
        using var content = BuildFormContent(
            model.CategoryId, null, model.ProductName, model.Price,
            model.Description, model.ImageUrl, model.ImageFile);
        using var response = await _httpClient.PutAsync(BuildUrl($"api/products/{id}"), content, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(BuildUrl($"api/products/{id}"), cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl($"api/products/{id}/restore"),
            new { },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
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

    private static MultipartFormDataContent BuildFormContent(
        int categoryId,
        string? productCode,
        string productName,
        decimal price,
        string? description,
        string? imageUrl,
        IFormFile? imageFile)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(categoryId.ToString()), "categoryId");
        if (!string.IsNullOrWhiteSpace(productCode))
            content.Add(new StringContent(productCode), "productCode");
        content.Add(new StringContent(productName ?? string.Empty), "productName");
        content.Add(new StringContent(price.ToString(CultureInfo.InvariantCulture)), "price");
        content.Add(new StringContent(description ?? string.Empty), "description");
        content.Add(new StringContent(imageUrl ?? string.Empty), "imageUrl");

        if (imageFile != null && imageFile.Length > 0)
        {
            var fileContent = new StreamContent(imageFile.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "imageFile", imageFile.FileName);
        }

        return content;
    }

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl.TrimEnd('/')}/{relativePath}";
}