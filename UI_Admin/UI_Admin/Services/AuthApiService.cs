using System.Text.Json;

namespace UI_Admin.Services
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AuthApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task SetPasswordAsync(string accessToken, string password)
        {
            var apiBaseUrl = _configuration["WorkShiftApi:BaseUrl"]
                ?? throw new InvalidOperationException("Thiếu cấu hình WorkShiftApi:BaseUrl.");
            using var response = await _httpClient.PostAsJsonAsync(
                $"{apiBaseUrl.TrimEnd('/')}/api/auth/set-password",
                new { accessToken, password });
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(GetErrorMessage(body));
        }

        private static string GetErrorMessage(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                foreach (var key in new[] { "msg", "message", "error_description", "error" })
                    if (document.RootElement.TryGetProperty(key, out var value))
                        return value.GetString() ?? "Không thể đặt mật khẩu.";
            }
            catch (JsonException) { }

            return "Link thiết lập mật khẩu không hợp lệ hoặc đã hết hạn.";
        }
    }
}
