using UI_Admin.Models;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Services;

public sealed class CategoryApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public CategoryApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<CategoryListPagedViewModel> GetPagedAsync(
        string? search,
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

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        if (normalizedStatus is "active" or "inactive")
            parameters.Add($"status={normalizedStatus.ToUpperInvariant()}");

        var result = await _httpClient.GetFromJsonAsync<CategoryListPagedViewModel>(
            BuildUrl($"api/categories?{string.Join('&', parameters)}"), cancellationToken);

        var paged = result ?? new CategoryListPagedViewModel();
        paged.Search = search;
        paged.StatusFilter = normalizedStatus is "active" or "inactive" ? normalizedStatus : "all";
        return paged;
    }

    public async Task<List<CategoryOptionViewModel>> GetActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        var paged = await GetPagedAsync(null, "active", 1, 100, cancellationToken);
        return paged.Items
            .Select(item => new CategoryOptionViewModel { Id = item.CategoryId, Name = item.CategoryName })
            .OrderBy(option => option.Name)
            .ToList();
    }

    public async Task<CategoryDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<CategoryDetailViewModel>(
            BuildUrl($"api/categories/{id}"), cancellationToken);
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(CreateCategoryViewModel model, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl("api/categories"),
            new { categoryName = model.CategoryName, description = model.Description },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, EditCategoryViewModel model, CancellationToken cancellationToken = default)
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

    private string BuildUrl(string relativePath) => $"{_apiBaseUrl.TrimEnd('/')}/{relativePath}";
}
