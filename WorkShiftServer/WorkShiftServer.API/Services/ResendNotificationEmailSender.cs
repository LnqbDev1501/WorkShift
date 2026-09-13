using System.Net.Http.Json;
using WorkShiftManagement.BLL.Interfaces.Admin;

namespace WorkShiftServer.API.Services;

public class ResendNotificationEmailSender : INotificationEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public ResendNotificationEmailSender(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task SendAsync(string recipientEmail, string title, string message)
    {
        var apiKey = _configuration["Resend:ApiKey"];
        var templateId = _configuration["Resend:NotificationTemplateId"];
        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(templateId))
        {
            throw new InvalidOperationException("Resend configuration is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                to = new[] { recipientEmail },
                subject = title,
                template = new
                {
                    id = templateId,
                    variables = new Dictionary<string, object>
                    {
                        ["NOTIFICATION_TITLE"] = title,
                        ["NOTIFICATION_MESSAGE"] = message
                    }
                }
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Resend returned HTTP {(int)response.StatusCode}.");
        }
    }
}
