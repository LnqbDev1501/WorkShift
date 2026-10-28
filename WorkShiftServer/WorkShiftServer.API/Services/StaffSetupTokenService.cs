using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace WorkShiftServer.API.Services;

public sealed class StaffSetupTokenService
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly ITimeLimitedDataProtector _protector;

    public StaffSetupTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("StaffSetup").ToTimeLimitedDataProtector();
    }

    public string CreateToken(Guid userId)
    {
        var payload = userId.ToString();
        var protectedPayload = _protector.Protect(Encoding.UTF8.GetBytes(payload), TokenLifetime);
        return WebEncoders.Base64UrlEncode(protectedPayload);
    }

    public Guid? ReadToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var payload = Encoding.UTF8.GetString(_protector.Unprotect(WebEncoders.Base64UrlDecode(token)));
            if (Guid.TryParse(payload, out var userId))
                return userId;
        }
        catch
        {
            // Tampered or expired tokens are treated as invalid.
        }
        return null;
    }
}
