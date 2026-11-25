using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WorkShiftManagement.BLL.Caching;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Services;

public sealed class SupabaseRoleClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache? _memoryCache;

    public SupabaseRoleClaimsTransformation(AppDbContext context, IMemoryCache? memoryCache = null)
    {
        _context = context;
        _memoryCache = memoryCache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity?.IsAuthenticated != true || identity.HasClaim("workshift_role_loaded", "true"))
            return principal;

        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
            return principal;

        var cacheKey = WorkShiftCacheKeys.UserRole(userId);
        string? role = null;
        bool cacheHit = _memoryCache != null && _memoryCache.TryGetValue(cacheKey, out role);

        if (!cacheHit)
        {
            role = await _context.Users.AsNoTracking()
                .Where(user => user.UserId == userId && user.Status == "ACTIVE" && user.DeletedAt == null)
                .Select(user => user.Role.RoleName)
                .SingleOrDefaultAsync();

            if (_memoryCache != null && !string.IsNullOrWhiteSpace(role))
            {
                _memoryCache.Set(cacheKey, role.Trim().ToUpperInvariant(), WorkShiftCacheKeys.DefaultTtl);
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Trim().ToUpperInvariant()));
        identity.AddClaim(new Claim("workshift_role_loaded", "true"));

        return principal;
    }
}
