using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using WorkShiftManagement.BLL.Interfaces.Auth;

namespace WorkShiftManagement.BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _inviteRedirectUrl;

    public AuthService(HttpClient httpClient, string inviteRedirectUrl)
    {
        _httpClient = httpClient;
        _inviteRedirectUrl = string.IsNullOrWhiteSpace(inviteRedirectUrl)
            ? throw new ArgumentException("Password setup URL is required.", nameof(inviteRedirectUrl))
            : inviteRedirectUrl;
    }

    public async Task<Guid> CreateUserAndSendInviteAsync(string email)
    {
        var endpoint = $"auth/v1/invite?redirect_to={Uri.EscapeDataString(_inviteRedirectUrl)}";
        using var response = await _httpClient.PostAsJsonAsync(endpoint, new { email });
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
                throw new HttpRequestException("Email already exists in Supabase Auth.", null, response.StatusCode);

            throw new HttpRequestException(
                $"Unable to create Supabase Auth user: {GetErrorMessage(body)}", null, response.StatusCode);
        }

        return GetUserId(body);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        using var response = await _httpClient.DeleteAsync($"auth/v1/admin/users/{userId}");
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Unable to roll back Supabase Auth user: {GetErrorMessage(body)}", null, response.StatusCode);
    }

    public async Task<Guid> SetPasswordAsync(string accessToken, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "auth/v1/user")
        {
            Content = JsonContent.Create(new { password })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to set password: {GetErrorMessage(body)}", null, response.StatusCode);
        }

        return GetUserId(body);
    }

    private static Guid GetUserId(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("id", out var id) &&
                Guid.TryParse(id.GetString(), out var userId))
                return userId;
        }
        catch (JsonException)
        {
            // Converted to InvalidDataException below.
        }

        throw new InvalidDataException("Supabase Auth did not return a valid user ID.");
    }

    private static string GetErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            foreach (var name in new[] { "msg", "message", "error_description", "error" })
                if (document.RootElement.TryGetProperty(name, out var value))
                    return value.GetString() ?? body;
        }
        catch (JsonException)
        {
            // Preserve the original response when it is not JSON.
        }

        return body;
    }
}
