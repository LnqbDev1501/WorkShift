using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftServer.API.Services;

public sealed class SupabaseRoleClaimsTransformation : IClaimsTransformation
{
    private readonly AppDbContext _context;

    public SupabaseRoleClaimsTransformation(AppDbContext context) => _context = context;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity?.IsAuthenticated != true || identity.HasClaim("workshift_role_loaded", "true"))
            return principal;

        var subject = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
            return principal;

        var role = await _context.Users.AsNoTracking()
            .Where(user => user.UserId == userId && user.Status == "ACTIVE" && user.DeletedAt == null)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync();
        if (!string.IsNullOrWhiteSpace(role))
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToUpperInvariant()));
        identity.AddClaim(new Claim("workshift_role_loaded", "true"));

        return principal;
    }
}
