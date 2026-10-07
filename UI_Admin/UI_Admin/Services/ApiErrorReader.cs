using System.Text.Json;

namespace UI_Admin.Services;

internal static class ApiErrorReader
{
    public static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string? rawContent = null;
        try
        {
            rawContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(rawContent))
            {
                using var doc = JsonDocument.Parse(rawContent);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    // 1. Check for "message" or "Message" property
                    if (TryGetPropertyCaseInsensitive(root, "message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                    {
                        var msg = messageProp.GetString();
                        if (!string.IsNullOrWhiteSpace(msg))
                            return msg;
                    }

                    // 2. Check for "detail" or "Detail" property (RFC ProblemDetails)
                    if (TryGetPropertyCaseInsensitive(root, "detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                    {
                        var detail = detailProp.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                            return detail;
                    }

                    // 3. Check for "error" or "Error" property
                    if (TryGetPropertyCaseInsensitive(root, "error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
                    {
                        var err = errorProp.GetString();
                        if (!string.IsNullOrWhiteSpace(err))
                            return err;
                    }

                    // 4. Check for "title" or "Title" property
                    if (TryGetPropertyCaseInsensitive(root, "title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                    {
                        var title = titleProp.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                            return title;
                    }

                    // 5. Check for "errors" dictionary (e.g. ASP.NET ModelState validation errors)
                    if (TryGetPropertyCaseInsensitive(root, "errors", out var errorsProp))
                    {
                        if (errorsProp.ValueKind == JsonValueKind.Object)
                        {
                            var errorList = new List<string>();
                            foreach (var prop in errorsProp.EnumerateObject())
                            {
                                if (prop.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in prop.Value.EnumerateArray())
                                    {
                                        var str = item.GetString();
                                        if (!string.IsNullOrWhiteSpace(str))
                                        {
                                            errorList.Add(str);
                                        }
                                    }
                                }
                                else if (prop.Value.ValueKind == JsonValueKind.String)
                                {
                                    var str = prop.Value.GetString();
                                    if (!string.IsNullOrWhiteSpace(str))
                                    {
                                        errorList.Add(str);
                                    }
                                }
                            }
                            if (errorList.Count > 0)
                                return string.Join("; ", errorList);
                        }
                        else if (errorsProp.ValueKind == JsonValueKind.Array)
                        {
                            var errorList = new List<string>();
                            foreach (var item in errorsProp.EnumerateArray())
                            {
                                var str = item.GetString();
                                if (!string.IsNullOrWhiteSpace(str))
                                {
                                    errorList.Add(str);
                                }
                            }
                            if (errorList.Count > 0)
                                return string.Join("; ", errorList);
                        }
                    }
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            if (!string.IsNullOrWhiteSpace(rawContent) && !rawContent.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                return rawContent.Trim();
            }
        }

        return $"Request failed with status code {(int)response.StatusCode}.";
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    result = prop.Value;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}
