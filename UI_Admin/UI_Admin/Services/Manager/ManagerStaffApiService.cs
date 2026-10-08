using System.Net.Http.Json;
using UI_Admin.Models;
using UI_Admin.Models.Catalog;

namespace UI_Admin.Services;

public sealed class ManagerStaffApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public ManagerStaffApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<StaffAccountPagedViewModel> GetStaffListAsync(
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"page={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            parameters.Add($"search={Uri.EscapeDataString(search.Trim())}");

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            parameters.Add($"status={Uri.EscapeDataString(status)}");

        var result = await _httpClient.GetFromJsonAsync<StaffAccountPagedViewModel>(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff?{string.Join('&', parameters)}",
            cancellationToken);

        var paged = result ?? new StaffAccountPagedViewModel();
        paged.Search = search;
        paged.StatusFilter = status;
        return paged;
    }

    public async Task<StaffAccountDetailViewModel?> GetStaffDetailAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff/{staffId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("The authentication service is temporarily unavailable. Please try again.");

        return await response.Content.ReadFromJsonAsync<StaffAccountDetailViewModel>(cancellationToken);
    }

    public async Task<List<EmployeeTypeOptionViewModel>> GetActiveEmployeeTypeOptionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<List<ActiveEmployeeTypeDto>>(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/employee-types/active",
            cancellationToken);

        return (result ?? [])
            .Select(item => new EmployeeTypeOptionViewModel { Id = item.EmployeeTypeId, Name = item.TypeName })
            .OrderBy(option => option.Name)
            .ToList();
    }

    public async Task<(bool Success, string? ErrorMessage)> CreateStaffAsync(
        CreateStaffAccountViewModel model,
        CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(model.FullName), "fullName" },
            { new StringContent(model.Email), "email" },
            { new StringContent(model.Phone), "phone" },
            { new StringContent(model.IdentityNo), "identityNo" },
            { new StringContent(model.Gender), "gender" },
            { new StringContent(model.DateOfBirth?.ToString("yyyy-MM-dd") ?? ""), "dateOfBirth" },
            { new StringContent(model.Address ?? ""), "address" }
        };

        if (model.EmployeeTypeId.HasValue)
            content.Add(new StringContent(model.EmployeeTypeId.Value.ToString()), "employeeTypeId");

        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var fileContent = new StreamContent(model.ImageFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "imageFile", model.ImageFile.FileName);
        }

        using var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff",
            content,
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateStaffAsync(
        Guid staffId,
        EditStaffAccountViewModel model,
        CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(model.FullName ?? ""), "fullName");
        content.Add(new StringContent(model.Phone ?? ""), "phone");
        content.Add(new StringContent(model.IdentityNo ?? ""), "identityNo");
        content.Add(new StringContent(model.Gender ?? ""), "gender");
        content.Add(new StringContent(model.DateOfBirth?.ToString("yyyy-MM-dd") ?? ""), "dateOfBirth");
        content.Add(new StringContent(model.Address ?? ""), "address");

        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            var fileContent = new StreamContent(model.ImageFile.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType ?? "application/octet-stream");
            content.Add(fileContent, "imageFile", model.ImageFile.FileName);
        }

        using var response = await _httpClient.PutAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff/{staffId}",
            content,
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

    public async Task<(bool Success, string? ErrorMessage)> DeactivateStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff/{staffId}",
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }

public async Task<(bool Success, string? ErrorMessage)> RestoreStaffAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl.TrimEnd('/')}/api/manager/staff/{staffId}/restore",
            JsonContent.Create(new { }),
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, await ApiErrorReader.ReadErrorMessageAsync(response, cancellationToken));
    }
}

public sealed class ActiveEmployeeTypeDto
{
    public int EmployeeTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}