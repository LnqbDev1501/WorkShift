using System.Net.Http.Json;
using System.Text.Json;

namespace UI_Admin.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AuthApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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
