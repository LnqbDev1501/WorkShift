using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Auth.Response;
using WorkShiftManagement.BLL.Interfaces.Auth;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftManagement.BLL.Services.Auth;

public sealed class ProfileService : IProfileService
{
    private readonly AppDbContext _context;

    public ProfileService(AppDbContext context) => _context = context;

    public async Task<ProfileResponse?> GetMyProfileAsync(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(subject, out var userId))
        {
            return null;
        }

        var entity = await _context.Users.AsNoTracking()
            .Where(u => u.UserId == userId && u.Status == "ACTIVE" && u.DeletedAt == null)
            .Select(u => new ProfileResponse
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                IdentityNo = u.IdentityNo,
                Gender = u.Gender,
                DateOfBirth = u.DateOfBirth,
                AvatarUrl = u.AvatarUrl,
                Address = u.Address,
                RoleId = u.RoleId,
                RoleName = u.Role.RoleName,
                BranchId = u.BranchId,
                BranchCode = u.Branch == null ? null : u.Branch.BranchCode,
                BranchName = u.Branch == null ? null : u.Branch.BranchName,
                EmployeeTypeId = u.EmployeeTypeId,
                EmployeeTypeName = u.EmployeeType == null ? null : u.EmployeeType.TypeName,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .SingleOrDefaultAsync();

        return entity;
    }
}
