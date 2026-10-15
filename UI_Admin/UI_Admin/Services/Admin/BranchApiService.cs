using System.Net.Http.Json;
using UI_Admin.Models.Branches;

namespace UI_Admin.Services;

public class BranchApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public BranchApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = (configuration["WorkShiftApi:BaseUrl"] ?? "http://localhost:5267").TrimEnd('/');
    }

    public async Task<BranchListPagedViewModel> GetPagedAsync(
        string? search = null,
        int? regionId = null,
        string? status = "ALL",
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

        if (regionId.HasValue && regionId.Value > 0)
        {
            queryParams.Add($"regionId={regionId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (normalizedStatus is "ACTIVE" or "INACTIVE" or "CLOSED")
            {
                queryParams.Add($"status={normalizedStatus}");
            }
        }

        var url = $"{_apiBaseUrl}/api/admin/branches?{string.Join("&", queryParams)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new BranchListPagedViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                RegionId = regionId,
                Status = status ?? "ALL",
                ErrorMessage = "Failed to load branches from server."
            };
        }

        var apiResult = await response.Content.ReadFromJsonAsync<PagedApiResponse<BranchApiResponse>>(cancellationToken: cancellationToken);

        var items = apiResult?.Items?.Select(b => new BranchListItemViewModel
        {
            BranchId = b.BranchId,
            BranchCode = b.BranchCode,
            BranchName = b.BranchName,
            RegionId = b.RegionId,
            RegionCode = b.RegionCode,
            RegionName = b.RegionName,
            Address = b.Address,
            EmployeeCount = b.EmployeeCount,
            Status = b.Status ?? "ACTIVE"
        }).ToList() ?? [];

        return new BranchListPagedViewModel
        {
            Items = items,
            PageNumber = apiResult?.PageNumber ?? pageNumber,
            PageSize = apiResult?.PageSize ?? pageSize,
            TotalItems = apiResult?.TotalItems ?? items.Count,
            Search = search,
            RegionId = regionId,
            Status = status ?? "ALL"
        };
    }

    public async Task<List<RegionOptionViewModel>> GetRegionsAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/regions?pageSize=100";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<RegionApiResponse>>(cancellationToken: cancellationToken);
            return result?.Items?.Select(r => new RegionOptionViewModel
            {
                RegionId = r.RegionId,
                RegionCode = r.RegionCode,
                RegionName = r.RegionName,
                Status = r.Status ?? "ACTIVE"
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<BranchOptionViewModel>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches?pageSize=100&status=ACTIVE";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var result = await response.Content.ReadFromJsonAsync<PagedApiResponse<BranchApiResponse>>(cancellationToken: cancellationToken);
            return result?.Items?.Select(b => new BranchOptionViewModel
            {
                Id = b.BranchId,
                Name = b.BranchName
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<BranchDetailViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches/{id}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var detail = await response.Content.ReadFromJsonAsync<BranchDetailApiResponse>(cancellationToken: cancellationToken);
        if (detail == null) return null;

        var employees = await GetEmployeesAsync(id, cancellationToken);

        return new BranchDetailViewModel
        {
            BranchId = detail.BranchId,
            BranchCode = detail.BranchCode,
            BranchName = detail.BranchName,
            RegionId = detail.Region?.RegionId ?? 0,
            RegionCode = detail.Region?.RegionCode ?? string.Empty,
            RegionName = detail.Region?.RegionName ?? string.Empty,
            RegionStatus = detail.Region?.Status ?? "ACTIVE",
            Address = detail.Address,
            Latitude = detail.Latitude,
            Longitude = detail.Longitude,
            Phone = detail.Phone,
            Email = detail.Email,
            EmployeeCount = detail.EmployeeCount > 0 ? detail.EmployeeCount : employees.Count,
            Status = detail.Status ?? "ACTIVE",
            Employees = employees
        };
    }

    public async Task<List<BranchEmployeeItemViewModel>> GetEmployeesAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches/{branchId}/employees?pageSize=100";
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var result = await response.Content.ReadFromJsonAsync<BranchEmployeesApiResponse>(cancellationToken: cancellationToken);
            return result?.Items?.Select(e => new BranchEmployeeItemViewModel
            {
                UserId = e.UserId,
                FullName = e.FullName,
                Email = e.Email,
                Phone = e.Phone,
                Role = e.Role,
                Status = e.Status
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateAsync(CreateBranchViewModel model, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches";
        var payload = new
        {
            branchCode = model.BranchCode.Trim().ToUpperInvariant(),
            branchName = model.BranchName.Trim(),
            regionId = model.RegionId,
            address = model.Address.Trim(),
            latitude = model.Latitude,
            longitude = model.Longitude,
            phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim()
        };

        using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(int id, EditBranchViewModel model, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches/{id}";
        var payload = new
        {
            branchName = model.BranchName.Trim(),
            regionId = model.RegionId,
            address = model.Address.Trim(),
            latitude = model.Latitude,
            longitude = model.Longitude,
            phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
            email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim()
        };

        using var response = await _httpClient.PutAsJsonAsync(url, payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches/{id}";
        using var response = await _httpClient.DeleteAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    public async Task<(bool Success, string? ErrorMessage)> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"{_apiBaseUrl}/api/admin/branches/{id}/restore";
        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken);
        return (false, error);
    }

    private class PagedApiResponse<T>
    {
        public List<T> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }

    private class BranchApiResponse
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public int RegionId { get; set; }
        public string RegionCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public string? Status { get; set; }
    }

    private class BranchDetailApiResponse
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public BranchRegionApiResponse? Region { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int EmployeeCount { get; set; }
        public string? Status { get; set; }
    }

    private class BranchRegionApiResponse
    {
        public int RegionId { get; set; }
        public string RegionCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string? Status { get; set; }
    }

    private class RegionApiResponse
    {
        public int RegionId { get; set; }
        public string RegionCode { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string? Status { get; set; }
    }


    private class BranchEmployeesApiResponse
    {
        public List<BranchEmployeeApiResponse> Items { get; set; } = [];
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
    }

    private class BranchEmployeeApiResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
