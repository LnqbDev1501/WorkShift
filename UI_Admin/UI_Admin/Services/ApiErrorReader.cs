
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Services;

internal static class ApiErrorReader
{
    public static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(message.GetString()))
                return message.GetString()!;
            var problem = JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Non-JSON error responses use the status fallback; cancellation must propagate.
        }

        return $"Request failed with status code {(int)response.StatusCode}.";
    }
}
