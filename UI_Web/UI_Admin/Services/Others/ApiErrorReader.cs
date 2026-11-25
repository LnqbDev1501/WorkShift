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
                    string? msg = null;
                    if (TryGetPropertyCaseInsensitive(root, "message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                    {
                        msg = messageProp.GetString();
                    }

                    if (TryGetPropertyCaseInsensitive(root, "detail", out var detailProp) && detailProp.ValueKind == JsonValueKind.String)
                    {
                        var detail = detailProp.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                        {
                            var combined = !string.IsNullOrWhiteSpace(msg) && !msg.Equals(detail, StringComparison.OrdinalIgnoreCase)
                                ? $"{msg} ({detail})"
                                : detail;
                            return LocalizeErrorMessage(combined);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(msg))
                        return LocalizeErrorMessage(msg);

                    // 3. Check for "error" or "Error" property
                    if (TryGetPropertyCaseInsensitive(root, "error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
                    {
                        var err = errorProp.GetString();
                        if (!string.IsNullOrWhiteSpace(err))
                            return LocalizeErrorMessage(err);
                    }

                    // 4. Check for "title" or "Title" property
                    if (TryGetPropertyCaseInsensitive(root, "title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                    {
                        var title = titleProp.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                            return LocalizeErrorMessage(title);
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
                                return LocalizeErrorMessage(string.Join("; ", errorList));
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
                                return LocalizeErrorMessage(string.Join("; ", errorList));
                        }
                    }
                }
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Ignored: Fall back to raw content
        }

        var finalMessage = !string.IsNullOrWhiteSpace(rawContent) && !rawContent.Contains("<html", StringComparison.OrdinalIgnoreCase)
            ? rawContent.Trim()
            : $"Request failed with status code {(int)response.StatusCode}.";
        return LocalizeErrorMessage(finalMessage);
    }

    public static string LocalizeErrorMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return msg;

        // Pattern: Requested (late arrival|early leave) time (2026-10-27 15:03:00 UTC) must be between shift start (2026-10-26 23:00:00 UTC) and shift end (2026-10-27 04:30:00 UTC).
        var timeMatch = System.Text.RegularExpressions.Regex.Match(
            msg,
            @"Requested (late arrival|early leave) time \(([^)]+)\) must be between shift start \(([^)]+)\) and shift end \(([^)]+)\)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (timeMatch.Success)
        {
            var isLate = timeMatch.Groups[1].Value.Equals("late arrival", StringComparison.OrdinalIgnoreCase);
            var reqStr = timeMatch.Groups[2].Value.Replace(" UTC", "").Trim();
            var startStr = timeMatch.Groups[3].Value.Replace(" UTC", "").Trim();
            var endStr = timeMatch.Groups[4].Value.Replace(" UTC", "").Trim();

            string reqVn = reqStr;
            string startVn = startStr;
            string endVn = endStr;

            if (DateTime.TryParse(reqStr, System.Globalization.CultureInfo.InvariantCulture, out var reqDt))
                reqVn = reqDt.AddHours(7).ToString("HH:mm dd/MM/yyyy");
            if (DateTime.TryParse(startStr, System.Globalization.CultureInfo.InvariantCulture, out var startDt))
                startVn = startDt.AddHours(7).ToString("HH:mm dd/MM/yyyy");
            if (DateTime.TryParse(endStr, System.Globalization.CultureInfo.InvariantCulture, out var endDt))
                endVn = endDt.AddHours(7).ToString("HH:mm dd/MM/yyyy");

            var action = isLate ? "late arrival" : "early leave";
            return $"The requested {action} time ({reqVn}) must be between {startVn} and {endVn} (UTC+7).";
        }

        // Pattern: must be submitted at least X day(s) in advance
        var advanceMatch = System.Text.RegularExpressions.Regex.Match(
            msg,
            @"must be submitted at least (\d+) day\(s\) in advance",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (advanceMatch.Success)
        {
            var days = advanceMatch.Groups[1].Value;
            return $"Requests must be submitted at least {days} day(s) in advance under branch policy.";
        }

        if (msg.Contains("Monthly limit", StringComparison.OrdinalIgnoreCase) || msg.Contains("limit of", StringComparison.OrdinalIgnoreCase))
        {
            return "You have reached the monthly limit for this request type under branch policy.";
        }

        if (msg.Contains("has already started or ended", StringComparison.OrdinalIgnoreCase))
        {
            return "This shift has already started or ended. Requests can no longer be submitted.";
        }

        if (msg.Contains("already exists for assignment", StringComparison.OrdinalIgnoreCase))
        {
            return "You already have a pending or approved request for this shift.";
        }

        return msg;
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
