using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WorkShiftManagement.BLL.Interfaces.Auth;

namespace WorkShiftServer.API.Services;

public class ResendProfileEmailChangeSender : IProfileEmailChangeSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ResendProfileEmailChangeSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendAsync(string recipientEmail, string fullName, string verifyLink, int expiresMinutes)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var templateId = _configuration["Resend:EmailChangeTemplateId"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("Resend email-change verification configuration is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                to = new[] { recipientEmail },
                template = new
                {
                    id = templateId,
                    variables = new Dictionary<string, object>
                    {
                        ["FULL_NAME"] = fullName,
                        ["NEW_EMAIL"] = recipientEmail,
                        ["VERIFY_LINK"] = verifyLink,
                        ["EXPIRES_MINUTES"] = expiresMinutes.ToString()
                    }
                }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Resend could not send the email-change verification email: {GetErrorMessage(body)}",
            null,
            response.StatusCode);
    }

    private static string GetErrorMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? body;
        }
        catch (JsonException)
        {
            // Preserve the original response when it is not JSON.
        }

        return body;
    }
}
