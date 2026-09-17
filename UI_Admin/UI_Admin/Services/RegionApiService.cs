using System.Net.Http.Json;
using UI_Admin.Models.Regions;

namespace UI_Admin.Services;

public class RegionApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public RegionApiService(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    public async Task<RegionListPagedViewModel> GetPagedAsync(
        string? search = null,
        string? status = "all",
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (normalizedStatus == "ACTIVE" || normalizedStatus == "INACTIVE")
            {
                queryParams.Add($"status={normalizedStatus}");
            }
        }

        var url = $"{_apiBaseUrl}/api/admin/regions?{string.Join("&", queryParams)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new RegionListPagedViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                StatusFilter = status ?? "all",
                ErrorMessage = "Failed to load regions from server."
            };
        }

        var apiResult = await response.Content.ReadFromJsonAsync<PagedApiResponse<RegionApiResponse>>(cancellationToken: cancellationToken);

        var items = apiResult?.Items?.Select(r => new RegionListItemViewModel
        {
            RegionId = r.RegionId,
            RegionCode = r.RegionCode,
            RegionName = r.RegionName,
            Description = r.Description,
            BranchCount = r.BranchCount,
            Status = r.Status ?? "ACTIVE"
        }).ToList() ?? [];

        return new RegionListPagedViewModel
        {
            Items = items,
            PageNumber = apiResult?.PageNumber ?? pageNumber,
            PageSize = apiResult?.PageSize ?? pageSize,
            TotalItems = apiResult?.TotalItems ?? items.Count,
            TotalPages = apiResult?.TotalPages ?? 1,
            Search = search,
            StatusFilter = status ?? "all"
        };
    }

    public async Task<RegionDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions/{id}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        var detail = await response.Content.ReadFromJsonAsync<RegionDetailApiResponse>(cancellationToken: cancellationToken);
        if (detail == null) return null;

        return new RegionDetailViewModel
        {
            RegionId = detail.RegionId,
            RegionCode = detail.RegionCode,
            RegionName = detail.RegionName,
            Description = detail.Description,
            Status = detail.Status ?? "ACTIVE",
            Branches = detail.Branches?.Select(b => new RegionBranchItemViewModel
            {
                BranchId = b.BranchId,
                BranchCode = b.BranchCode,
                BranchName = b.BranchName,
                Status = b.Status ?? "ACTIVE",
                Address = b.Address,
                Latitude = b.Latitude,
                Longitude = b.Longitude
            }).ToList() ?? []
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(CreateRegionViewModel model, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions";
        var payload = new
        {
            regionCode = model.RegionCode.Trim().ToUpperInvariant(),
            regionName = model.RegionName.Trim(),
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, EditRegionViewModel model, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions/{id}";
        var payload = new
        {
            regionName = model.RegionName.Trim(),
            description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
        };

        using var response = await _httpClient.PutAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions/{id}";
        using var response = await _httpClient.DeleteAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions/{id}/restore";
        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;
        }
        catch { }

        return $"Request failed with status code {(int)response.StatusCode}.";
    }

    private class PagedApiResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    private class RegionApiResponse
    {
        public int RegionId { get; set; }
        public string RegionCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BranchCount { get; set; }
        public string? Status { get; set; }
    }

    private class RegionDetailApiResponse
    {
        public int RegionId { get; set; }
        public string RegionCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Status { get; set; }
        public List<BranchApiResponse>? Branches { get; set; }
    }

    private class BranchApiResponse
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }

    private class ProblemDetailsResponse
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
    }
}
