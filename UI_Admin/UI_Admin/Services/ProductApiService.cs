using System.Globalization;
using System.Net.Http.Headers;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Services;

public sealed class ProductApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ProductApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<ProductListPagedViewModel> GetPagedAsync(
        string? search,
        int? branchId,
        int? categoryId,
        string? status = "all",
        int pageNumber = 1,
        int pageSize = ListPaginationViewModel.DefaultPageSize,
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
        paged.Search = search;
        paged.BranchId = branchId is > 0 ? branchId : null;
        paged.CategoryId = categoryId is > 0 ? categoryId : null;
        paged.StatusFilter = normalizedStatus is "active" or "inactive" ? normalizedStatus : "all";
        return paged;
    }

    public async Task<ProductDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<ProductDetailViewModel>(
            BuildUrl($"api/products/{id}"), cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(CreateProductViewModel model, CancellationToken cancellationToken = default)
    {
        using var content = BuildFormContent(
            model.CategoryId, model.ProductCode, model.ProductName, model.Price,
            model.Description, model.ImageUrl, model.ImageFile);
        using var response = await _httpClient.PostAsync(BuildUrl("api/products"), content, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, EditProductViewModel model, CancellationToken cancellationToken = default)
    {
        using var content = BuildFormContent(
            model.CategoryId, null, model.ProductName, model.Price,
            model.Description, model.ImageUrl, model.ImageFile);
        using var response = await _httpClient.PutAsync(BuildUrl($"api/products/{id}"), content, cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
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

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl.TrimEnd('/')}/{relativePath}";
}
