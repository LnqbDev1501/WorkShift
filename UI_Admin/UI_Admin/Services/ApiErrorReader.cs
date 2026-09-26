using System.Net.Http.Json;
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
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken: cancellationToken);
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
