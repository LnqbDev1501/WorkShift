using System.Net.Http.Json;
using UI_Admin.Models.Admin.UserAdmin;

namespace UI_Admin.Services.Admin;

public sealed class UserApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;

    public UserApiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiBaseUrl = configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("WorkShiftApi:BaseUrl is missing.");
    }

    public async Task<UserListViewModel> GetListAsync(
        UserListQueryViewModel query,
        CancellationToken cancellationToken = default)
    {
        var listTask = GetUsersAsync(query, cancellationToken);
        var activeTask = GetUsersAsync(new UserListQueryViewModel { Status = "ACTIVE" }, cancellationToken);
        var pendingTask = GetUsersAsync(new UserListQueryViewModel { Status = "PENDING" }, cancellationToken);
        var dashboardTask = GetDashboardAsync(cancellationToken);
        var branchesTask = GetBranchesAsync(cancellationToken);
        var rolesTask = GetOperationalRolesAsync(cancellationToken);

        await Task.WhenAll(listTask, activeTask, pendingTask, dashboardTask, branchesTask, rolesTask);

        var list = await listTask;
        var dashboard = await dashboardTask;
        var branches = await branchesTask;
        var roles = await rolesTask;

        return new UserListViewModel
        {
            Query = query,
            Items = list.Items,
            Roles = roles,
            Branches = branches.Items
                .Where(branch => branch.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                .Select(branch => new UserFilterOptionViewModel { Id = branch.BranchId, Name = branch.BranchName })
                .OrderBy(branch => branch.Name)
                .ToList(),
            PageNumber = list.PageNumber,
            PageSize = list.PageSize,
            TotalItems = list.TotalItems,
            TotalPages = list.TotalPages,
            ActiveUsers = (await activeTask).TotalItems,
            PendingUsers = (await pendingTask).TotalItems,
            ElevatedUsers = dashboard.ManagerCount + dashboard.AccountantCount,
            ActiveBranches = dashboard.ActiveBranches,
            TotalBranches = dashboard.TotalBranches
        };
    }

    public async Task<UserDetailViewModel> GetDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<UserDetailViewModel>(
            BuildUrl($"api/admin/users/{userId}"), cancellationToken);

        return response ?? throw new InvalidOperationException("User detail response is empty.");
    }

    public Task ResendSetupLinkAsync(Guid userId, CancellationToken cancellationToken = default) =>
        SendWithoutBodyAsync(HttpMethod.Post, $"api/admin/users/{userId}/resend-setup-link", cancellationToken);

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default) =>
        SendWithoutBodyAsync(HttpMethod.Delete, $"api/admin/users/{userId}", cancellationToken);

    public async Task RestoreAsync(Guid userId, int? branchId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl($"api/admin/users/{userId}/restore"),
            new { BranchId = branchId },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<UserFilterOptionViewModel>> GetOperationalRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var roles = await _httpClient.GetFromJsonAsync<List<RoleResponseItem>>(
                BuildUrl("api/admin/users/roles"), cancellationToken) ?? [];

            if (roles.Count > 0)
            {
                return roles.Select(role => new UserFilterOptionViewModel
                {
                    Id = role.RoleId,
                    Name = ToDisplayName(role.RoleName)
                }).OrderBy(role => role.Name).ToList();
            }
        }
        catch
        {
            // fallback if offline
        }

        return
        [
            new UserFilterOptionViewModel { Id = 2, Name = "Manager" },
            new UserFilterOptionViewModel { Id = 3, Name = "Accountant" }
        ];
    }

    public async Task<List<BranchOptionViewModel>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var branches = await _httpClient.GetFromJsonAsync<List<BranchAssignableResponseItem>>(
                BuildUrl("api/admin/branches/assignable"), cancellationToken) ?? [];

            if (branches.Count > 0)
            {
                return branches.Select(b => new BranchOptionViewModel
                {
                    Id = b.BranchId,
                    Name = b.BranchName,
                    BranchCode = b.BranchCode,
                    ManagerCount = b.ManagerCount,
                    AccountantCount = b.AccountantCount
                }).OrderBy(b => b.Name).ToList();
            }
        }
        catch
        {
            // fallback to general branches API if endpoint unavailable
        }

        try
        {
            var pagedBranches = await GetBranchesAsync(cancellationToken);
            return pagedBranches.Items
                .Where(branch => branch.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
                .Select(branch => new BranchOptionViewModel
                {
                    Id = branch.BranchId,
                    Name = branch.BranchName,
                    BranchCode = branch.BranchCode,
                    ManagerCount = 0,
                    AccountantCount = 0
                })
                .OrderBy(branch => branch.Name)
                .ToList();
        }
        catch
        {
            // Non-admin roles may not access the general branches API.
            return [];
        }
    }

    public async Task CreateAsync(UserCreateViewModel model, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model.FullName,
            model.Email,
            model.Phone,
            model.IdentityNo,
            model.RoleId,
            model.BranchId,
            model.Gender,
            model.DateOfBirth,
            model.Address
        };

        using var response = await _httpClient.PostAsJsonAsync(
            BuildUrl("api/admin/users"),
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateAsync(Guid userId, UserEditViewModel model, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(model.FullName ?? string.Empty), nameof(model.FullName));
        content.Add(new StringContent(model.Email ?? string.Empty), nameof(model.Email));
        content.Add(new StringContent(model.Phone ?? string.Empty), nameof(model.Phone));
        content.Add(new StringContent(model.IdentityNo ?? string.Empty), nameof(model.IdentityNo));
        content.Add(new StringContent(model.Gender ?? string.Empty), nameof(model.Gender));
        content.Add(new StringContent(model.Address ?? string.Empty), nameof(model.Address));
        content.Add(new StringContent(model.RoleId.ToString()), nameof(model.RoleId));

        if (model.BranchId.HasValue)
        {
            content.Add(new StringContent(model.BranchId.Value.ToString()), nameof(model.BranchId));
        }

        if (model.DateOfBirth.HasValue)
        {
            content.Add(new StringContent(model.DateOfBirth.Value.ToString("yyyy-MM-dd")), nameof(model.DateOfBirth));
        }

        if (model.AvatarFile != null && model.AvatarFile.Length > 0)
        {
            var streamContent = new StreamContent(model.AvatarFile.OpenReadStream());
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.AvatarFile.ContentType ?? "image/jpeg");
            content.Add(streamContent, nameof(model.AvatarFile), model.AvatarFile.FileName);
        }

        using var response = await _httpClient.PutAsync(
            BuildUrl($"api/admin/users/{userId}"),
            content,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<PagedUsersResponse> GetUsersAsync(
        UserListQueryViewModel query,
        CancellationToken cancellationToken)
    {
        var parameters = new List<string> { $"pageNumber={Math.Max(1, query.PageNumber)}" };
        Add(parameters, "search", query.Search);
        Add(parameters, "status", query.Status);
        if (query.RoleId.HasValue) parameters.Add($"roleId={query.RoleId.Value}");
        if (query.BranchId.HasValue) parameters.Add($"branchId={query.BranchId.Value}");

        return await _httpClient.GetFromJsonAsync<PagedUsersResponse>(
            BuildUrl($"api/admin/users?{string.Join('&', parameters)}"), cancellationToken)
            ?? new PagedUsersResponse();
    }

    private async Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken) =>
        await _httpClient.GetFromJsonAsync<DashboardResponse>(BuildUrl("api/admin/dashboard"), cancellationToken)
        ?? new DashboardResponse();

    private async Task<PagedBranchesResponse> GetBranchesAsync(CancellationToken cancellationToken) =>
        await _httpClient.GetFromJsonAsync<PagedBranchesResponse>(
            BuildUrl("api/admin/branches?pageNumber=1&pageSize=1000"), cancellationToken)
        ?? new PagedBranchesResponse();

    private async Task SendWithoutBodyAsync(
        HttpMethod method,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, BuildUrl(relativeUrl));
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
        throw new HttpRequestException(error?.Message ?? error?.Detail ?? "Backend API request failed.", null, response.StatusCode);
    }

    private string BuildUrl(string relativeUrl) =>
        $"{_apiBaseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";

    private static void Add(ICollection<string> parameters, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private static string ToDisplayName(string value) =>
        System.Globalization.CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase(value.ToLowerInvariant());

    private sealed class PagedUsersResponse
    {
        public List<UserListItemViewModel> Items { get; set; } = [];
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class DashboardResponse
    {
        public int ManagerCount { get; set; }
        public int AccountantCount { get; set; }
        public int ActiveBranches { get; set; }
        public int TotalBranches { get; set; }
    }

    private sealed class PagedBranchesResponse
    {
        public List<BranchResponse> Items { get; set; } = [];
    }

    private sealed class BranchResponse
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class RoleResponseItem
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    private sealed class BranchAssignableResponseItem
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public int ManagerCount { get; set; }
        public int AccountantCount { get; set; }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }
}
