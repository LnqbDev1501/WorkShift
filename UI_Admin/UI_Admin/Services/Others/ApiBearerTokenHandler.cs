using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net.Http.Headers;

namespace UI_Admin.Services;

public sealed class ApiBearerTokenHandler(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
            return await base.SendAsync(request, cancellationToken);

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        if (path.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase))
            return await base.SendAsync(request, cancellationToken);

        var accessToken = await context.GetTokenAsync("access_token");
        var expiresAt = await context.GetTokenAsync("access_token_expires_at");
        if (DateTimeOffset.TryParse(expiresAt, out var expiration) &&
            expiration <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            var refreshToken = await context.GetTokenAsync("refresh_token");
            try
            {
                var authApiService = serviceProvider.GetRequiredService<AuthApiService>();
                var refreshed = await authApiService.RefreshAsync(refreshToken ?? string.Empty);
                var now = DateTimeOffset.UtcNow;
                var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var properties = authResult.Properties ?? new AuthenticationProperties();
                properties.IssuedUtc = now;
                properties.ExpiresUtc = now.AddDays(7);
                properties.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = refreshed.AccessToken },
                    new AuthenticationToken { Name = "refresh_token", Value = refreshed.RefreshToken },
                    new AuthenticationToken { Name = "access_token_expires_at", Value = now.AddSeconds(refreshed.ExpiresIn).ToString("O") }
                });
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, refreshed.FullName),
                    new Claim(ClaimTypes.Email, refreshed.Email),
                    new Claim(ClaimTypes.Role, refreshed.Role)
                };
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                    properties);
                accessToken = refreshed.AccessToken;
            }
            catch (InvalidOperationException)
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
