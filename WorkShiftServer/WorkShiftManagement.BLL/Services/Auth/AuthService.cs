using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.BLL.DTOs.Auth.Request;
using WorkShiftManagement.BLL.DTOs.Auth.Response;

namespace WorkShiftManagement.BLL.Services.Auth;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _inviteRedirectUrl;
    private readonly string _passwordRecoveryRedirectUrl;

    public AuthService(HttpClient httpClient, string inviteRedirectUrl, string passwordRecoveryRedirectUrl)
    {
        _httpClient = httpClient;
        _inviteRedirectUrl = string.IsNullOrWhiteSpace(inviteRedirectUrl)
            ? throw new ArgumentException("Password setup URL is required.", nameof(inviteRedirectUrl))
            : inviteRedirectUrl;
        _passwordRecoveryRedirectUrl = string.IsNullOrWhiteSpace(passwordRecoveryRedirectUrl)
            ? throw new ArgumentException("Password recovery URL is required.", nameof(passwordRecoveryRedirectUrl))
            : passwordRecoveryRedirectUrl;
    }

    public async Task<Guid> CreateUserAndSendInviteAsync(string email)
    {
        return await SendInvitationAsync(email, "create");
    }

    public async Task<string?> ResendSetupLinkAsync(Guid userId, string email)
    {
        using var userResponse = await _httpClient.GetAsync($"auth/v1/admin/users/{userId}");
        var userBody = await userResponse.Content.ReadAsStringAsync();
        if (!userResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to read Supabase Auth user: {GetErrorMessage(userBody)}", null, userResponse.StatusCode);
        }

        using var document = JsonDocument.Parse(userBody);
        var root = document.RootElement;
        var authEmail = root.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString()
            : null;
        if (!string.Equals(authEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Supabase Auth email does not match the local user profile.");

        var emailIsConfirmed = root.TryGetProperty("email_confirmed_at", out var confirmedAt) &&
                               confirmedAt.ValueKind is not JsonValueKind.Null &&
                               !string.IsNullOrWhiteSpace(confirmedAt.GetString());

        if (!emailIsConfirmed)
        {
            var invitedUserId = await SendInvitationAsync(email, "resend");
            if (invitedUserId != userId)
                throw new InvalidDataException("Supabase Auth returned a user that does not match the local profile.");
            return null;
        }

        using var linkResponse = await _httpClient.PostAsJsonAsync("auth/v1/admin/generate_link", new
        {
            type = "recovery",
            email,
            redirect_to = _inviteRedirectUrl
        });
        var linkBody = await linkResponse.Content.ReadAsStringAsync();
        if (!linkResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Unable to generate Supabase recovery link: {GetErrorMessage(linkBody)}",
                null,
                linkResponse.StatusCode);
        }

        return GetActionLink(linkBody);
    }

    private async Task<Guid> SendInvitationAsync(string email, string operation)
    {
        var endpoint = $"auth/v1/invite?redirect_to={Uri.EscapeDataString(_inviteRedirectUrl)}";
        using var response = await _httpClient.PostAsJsonAsync(endpoint, new { email });
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
                throw new HttpRequestException("Email already exists in Supabase Auth.", null, response.StatusCode);

            throw new HttpRequestException(
                $"Unable to {operation} Supabase invitation: {GetErrorMessage(body)}", null, response.StatusCode);
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

    public async Task BanUserAsync(Guid userId)
    {
        await UpdateBanDurationAsync(userId, "876000h", "ban");
    }

    public async Task UnbanUserAsync(Guid userId)
    {
        await UpdateBanDurationAsync(userId, "none", "unban");
    }

    private async Task UpdateBanDurationAsync(Guid userId, string banDuration, string operation)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"auth/v1/admin/users/{userId}",
            new { ban_duration = banDuration });
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Unable to {operation} Supabase Auth user: {GetErrorMessage(body)}",
            null,
            response.StatusCode);
    }

    public async Task<Guid> SetPasswordAsync(string accessToken, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "auth/v1/user")
        {
            Content = JsonContent.Create(new { password = password.Trim() })
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

    public async Task UpdateEmailAsync(Guid userId, string email)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"auth/v1/admin/users/{userId}",
            new { email = email, email_confirm = true });

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Unable to update email in Supabase Auth: {GetErrorMessage(body)}",
            null,
            response.StatusCode);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=password",
            new { email = request.Email.Trim(), password = request.Password.Trim() });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(GetErrorMessage(body), null, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var user = root.GetProperty("user");
        if (!Guid.TryParse(user.GetProperty("id").GetString(), out var userId))
            throw new InvalidDataException("Supabase Auth did not return a valid user ID.");

        return new LoginResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            UserId = userId,
            Email = user.GetProperty("email").GetString() ?? string.Empty
        };
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "auth/v1/token?grant_type=refresh_token",
            new { refresh_token = refreshToken });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(GetErrorMessage(body), null, response.StatusCode);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var user = root.GetProperty("user");
        if (!Guid.TryParse(user.GetProperty("id").GetString(), out var userId))
            throw new InvalidDataException("Supabase Auth did not return a valid user ID.");

        return new LoginResponse
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            UserId = userId,
            Email = user.GetProperty("email").GetString() ?? string.Empty
        };
    }

    public async Task SendPasswordRecoveryAsync(string email)
    {
        var endpoint = $"auth/v1/recover?redirect_to={Uri.EscapeDataString(_passwordRecoveryRedirectUrl)}";
        using var response = await _httpClient.PostAsJsonAsync(endpoint, new { email = email.Trim() });
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Unable to send password recovery email: {GetErrorMessage(body)}",
            null,
            response.StatusCode);
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

    private static string GetActionLink(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("action_link", out var actionLink) &&
                Uri.TryCreate(actionLink.GetString(), UriKind.Absolute, out var uri))
                return uri.AbsoluteUri;
        }
        catch (JsonException)
        {
            // Converted to InvalidDataException below.
        }

        throw new InvalidDataException("Supabase Auth did not return a valid account setup link.");
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
