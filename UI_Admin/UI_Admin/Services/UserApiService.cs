using System.Net.Http.Json;
using UI_Admin.Models;

namespace UI_Admin.Services;

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

        await Task.WhenAll(listTask, activeTask, pendingTask, dashboardTask, branchesTask);

        var list = await listTask;
        var dashboard = await dashboardTask;
        var branches = await branchesTask;

        return new UserListViewModel
        {
            Query = query,
            Items = list.Items,
            Roles = list.Items
                .Where(user => user.RoleId > 0)
                .GroupBy(user => user.RoleId)
                .Select(group => new UserFilterOptionViewModel
                {
                    Id = group.Key,
                    Name = ToDisplayName(group.First().RoleName)
                })
                .OrderBy(role => role.Name)
                .ToList(),
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
        public string BranchName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }
}
