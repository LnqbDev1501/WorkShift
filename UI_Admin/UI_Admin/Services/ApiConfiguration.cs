using Microsoft.Extensions.Configuration;

namespace UI_Admin.Services;

public static class ApiConfiguration
{
    public static string GetWorkShiftApiBaseUrl(this IConfiguration configuration)
    {
        var value = configuration["WorkShiftApi:BaseUrl"]?.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException("WorkShiftApi:BaseUrl must be an absolute HTTP or HTTPS URL without a query or fragment.");
        }

        return value!.TrimEnd('/');
    }
}
