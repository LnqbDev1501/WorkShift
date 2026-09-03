using System.Net;
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
        var fromEmail = _configuration["Resend:FromEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Resend configuration is missing.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = fromEmail,
                to = new[] { recipientEmail },
                subject = title,
                html = $"<p>{WebUtility.HtmlEncode(message).Replace("\n", "<br />")}</p>"
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
