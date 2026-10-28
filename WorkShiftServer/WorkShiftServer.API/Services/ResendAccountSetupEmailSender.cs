using System.Net.Http.Headers;
using System.Text.Json;
using WorkShiftManagement.BLL.Interfaces.Auth;

namespace WorkShiftServer.API.Services;

public class ResendAccountSetupEmailSender : IAccountSetupEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ResendAccountSetupEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendAsync(string recipientEmail, string setupLink)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var templateId = _configuration["Resend:AccountSetupTemplateId"];
        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("Resend account setup email configuration is missing.");
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
                        ["SETUP_LINK"] = setupLink
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
            $"Resend could not send the account setup email: {GetErrorMessage(body)}",
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
