using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using UI_Admin.Models;

namespace UI_Admin.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthApiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SetPasswordAsync(
            string accessToken,
            string password)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException(
                    "The WorkShiftApi:BaseUrl configuration is missing.");

            using var response = await _httpClient.PostAsJsonAsync(
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/set-password",
                new { accessToken, password = password.Trim() });

            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(GetErrorMessage(body));
        }

        public async Task<LoginApiResponse> LoginAsync(string email, string password)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

            using var response = await _httpClient.PostAsJsonAsync(
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/login",
                new { email = email.Trim(), password = password.Trim() });
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(GetErrorMessage(body));

            return JsonSerializer.Deserialize<LoginApiResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("The authentication service returned an invalid response.");
        }

        public async Task<LoginApiResponse> RefreshAsync(string refreshToken)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

            using var response = await _httpClient.PostAsJsonAsync(
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/refresh",
                new { refreshToken });
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(GetErrorMessage(body));

            return JsonSerializer.Deserialize<LoginApiResponse>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new InvalidOperationException("The authentication service returned an invalid response.");
        }

        public async Task RequestPasswordRecoveryAsync(string email)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

            using var response = await _httpClient.PostAsJsonAsync(
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/forgot-password",
                new { email = email.Trim() });
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("The authentication service is temporarily unavailable. Please try again.");
        }

        public async Task ChangePasswordAsync(string newPassword, CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

        using var response = await _httpClient.PutAsJsonAsync(
            $"{apiBaseUrl.TrimEnd('/')}/api/profile/password",
            new { password = newPassword.Trim() },
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(GetErrorMessage(body));
    }

    private async Task AttachBearerTokenAsync(HttpRequestMessage request)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return;

        var accessToken = await context.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<UserDetailViewModel> UpdateProfileAsync(
        string fullName,
        string phone,
        string gender,
        DateOnly? dateOfBirth,
        string address,
        CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{apiBaseUrl.TrimEnd('/')}/api/profile/me")
        {
            Content = JsonContent.Create(new
            {
                fullName = fullName.Trim(),
                phone = phone.Trim(),
                gender = gender.Trim(),
                dateOfBirth,
                address = address.Trim()
            })
        };
        await AttachBearerTokenAsync(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserDetailViewModel>(cancellationToken)
                ?? throw new InvalidOperationException("The authentication service returned an invalid response.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(GetErrorMessage(body));
    }

    public async Task<string> UpdateAvatarAsync(IFormFile avatarFile, CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

        using var content = new MultipartFormDataContent();
        await using var stream = avatarFile.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(avatarFile.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "avatarFile", avatarFile.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl.TrimEnd('/')}/api/profile/me/avatar")
        {
            Content = content
        };
        await AttachBearerTokenAsync(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken);
            if (result != null && result.TryGetValue("avatarUrl", out var avatarUrl))
                return avatarUrl;
            throw new InvalidOperationException("The authentication service returned an invalid response.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(GetErrorMessage(body));
    }

    public async Task RequestEmailChangeAsync(string email, CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl.TrimEnd('/')}/api/profile/email/change-request")
        {
            Content = JsonContent.Create(new { email = email.Trim() })
        };
        await AttachBearerTokenAsync(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(GetErrorMessage(body));
    }

    public async Task<UserDetailViewModel> ConfirmEmailChangeAsync(string token, CancellationToken cancellationToken = default)
    {
        var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
            ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

        using var response = await _httpClient.PostAsJsonAsync(
            $"{apiBaseUrl.TrimEnd('/')}/api/profile/email/confirm",
            new { token },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<UserDetailViewModel>(cancellationToken)
                ?? throw new InvalidOperationException("The authentication service returned an invalid response.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(GetErrorMessage(body));
    }

    public async Task<UserDetailViewModel?> GetMyProfileAsync(CancellationToken cancellationToken = default)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException("The WorkShiftApi:BaseUrl configuration is missing.");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl.TrimEnd('/')}/api/profile/me");

            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                var accessToken = await context.GetTokenAsync("access_token");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("The authentication service is temporarily unavailable. Please try again.");

            return await response.Content.ReadFromJsonAsync<UserDetailViewModel>(cancellationToken);
        }

        private static string GetErrorMessage(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);

                foreach (var key in new[]
                         {
                             "msg",
                             "message",
                             "error_description",
                             "error"
                         })
                {
                    if (document.RootElement.TryGetProperty(key, out var value))
                    {
                        return value.GetString()
                            ?? "The password could not be set.";
                    }
                }
            }
            catch (JsonException)
            {
                // Return the default message below.
            }

            return "The password setup link is invalid or has expired.";
        }
    }

    public sealed class LoginApiResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
