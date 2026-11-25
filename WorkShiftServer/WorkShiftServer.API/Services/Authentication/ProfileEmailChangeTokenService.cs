using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace WorkShiftServer.API.Services;

/// <summary>
/// Creates and validates short-lived, tamper-proof email-change verification tokens.
/// No database storage is required.
/// </summary>
public sealed class ProfileEmailChangeTokenService
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(3);

    private readonly ITimeLimitedDataProtector _protector;

    public ProfileEmailChangeTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ProfileEmailChange").ToTimeLimitedDataProtector();
    }

    public string CreateToken(Guid userId, string newEmail)
    {
        var payload = $"{userId}|{newEmail.Trim().ToLowerInvariant()}";
        var protectedPayload = _protector.Protect(Encoding.UTF8.GetBytes(payload), TokenLifetime);
        return WebEncoders.Base64UrlEncode(protectedPayload);
    }

    public (Guid UserId, string NewEmail)? ReadToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var payload = Encoding.UTF8.GetString(_protector.Unprotect(WebEncoders.Base64UrlDecode(token)));
            var separator = payload.IndexOf('|');
            if (separator <= 0)
                return null;

            if (!Guid.TryParse(payload[..separator], out var userId))
                return null;

            var email = payload[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return (userId, email);
        }
        catch
        {
            // Tampered or expired tokens are treated as invalid.
            return null;
        }
    }
}
