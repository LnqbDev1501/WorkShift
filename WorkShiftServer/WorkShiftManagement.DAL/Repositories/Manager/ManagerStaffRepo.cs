using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Models.Admin;
using WorkShiftManagement.DAL.Security;

namespace WorkShiftManagement.DAL.Repositories.Manager;

public class ManagerStaffRepo : IManagerStaffRepo
{
    private readonly AppDbContext _context;

    public ManagerStaffRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedStaffAsync(
        int managerBranchId,
        string? search,
        string? status,
        int pageNumber,
        int pageSize,
        bool? hasContract = null)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => (u.BranchId == managerBranchId ||
                         _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == managerBranchId && c.Status == "ACTIVE"))
                        && u.Role.RoleName == "STAFF");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FullName, pattern) ||
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.Phone, pattern));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            if (normalizedStatus == "ACTIVE")
                query = query.Where(u => u.Status == "ACTIVE" && u.DeletedAt == null);
            else if (normalizedStatus == "PENDING")
                query = query.Where(u => u.Status == "PENDING" && u.DeletedAt == null);
            else
                query = query.Where(u => u.Status == normalizedStatus);
        }

        if (hasContract == true)
        {
            query = query.Where(u => _context.EmploymentContracts.Any(c =>
                c.UserId == u.UserId &&
                (c.BranchId == managerBranchId || u.BranchId == managerBranchId) &&
                (c.Status == "ACTIVE" || c.Status == "SCHEDULED" ||
                 c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL")));
        }
        else if (hasContract == false)
        {
            query = query.Where(u => !_context.EmploymentContracts.Any(c =>
                c.UserId == u.UserId &&
                (c.BranchId == managerBranchId || u.BranchId == managerBranchId) &&
                (c.Status == "ACTIVE" || c.Status == "SCHEDULED" ||
                 c.Status == "DRAFT" || c.Status == "PENDING_STAFF" || c.Status == "PENDING_MANAGER_APPROVAL")));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .Include(u => u.EmploymentContracts)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<User?> GetStaffByIdAsync(Guid staffId, int managerBranchId)
    {
        return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                (u.BranchId == managerBranchId || _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == managerBranchId && c.Status == "ACTIVE")) &&
                u.Role.RoleName == "STAFF");
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == email.Trim().ToLowerInvariant() && u.DeletedAt == null);
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeUserId = null)
    {
        var trimmedPhone = phone.Trim();
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.Phone == trimmedPhone && u.DeletedAt == null);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.UserId != excludeUserId.Value);

        return await query.AnyAsync();
    }

    public async Task<bool> ExistsByIdentityNoAsync(string identityNo, Guid? excludeUserId = null)
    {
        var trimmed = identityNo.Trim();
        var encrypted = IdentityNoEncryption.Protect(trimmed);

        List<int> found;
        if (excludeUserId.HasValue)
        {
            found = await _context.Database
                .SqlQuery<int>($@"
                    SELECT 1 AS ""Value""
                    FROM public.users
                    WHERE (identity_no = {encrypted} OR identity_no = {trimmed})
                      AND deleted_at IS NULL
                      AND user_id <> {excludeUserId.Value}
                    LIMIT 1")
                .ToListAsync();
        }
        else
        {
            found = await _context.Database
                .SqlQuery<int>($@"
                    SELECT 1 AS ""Value""
                    FROM public.users
                    WHERE (identity_no = {encrypted} OR identity_no = {trimmed})
                      AND deleted_at IS NULL
                    LIMIT 1")
                .ToListAsync();
        }

        return found.Count > 0;
    }

    public async Task<User> CreateStaffAsync(User staff, Guid managerId)
    {
        _context.Users.Add(staff);
        await _context.SaveChangesAsync();
        return staff;
    }

    public async Task<User?> UpdateStaffAsync(Guid staffId, int managerBranchId, string fullName, string phone, string gender, DateOnly? dateOfBirth, string? address)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                (u.BranchId == managerBranchId || _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == managerBranchId && c.Status == "ACTIVE")) &&
                u.Role.RoleName == "STAFF" &&
                u.DeletedAt == null);

        if (staff == null)
            return null;

        if (staff.BranchId == null || staff.BranchId != managerBranchId)
        {
            staff.BranchId = managerBranchId;
        }

        staff.FullName = fullName.Trim();
        staff.Phone = phone.Trim();
        // IdentityNo is preserved and not modified by manager
        staff.Gender = gender.Trim().ToUpperInvariant();
        staff.DateOfBirth = dateOfBirth ?? default;
        staff.Address = string.IsNullOrWhiteSpace(address) ? string.Empty : address.Trim();
        staff.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return staff;
    }

    public async Task<bool> SoftDeleteStaffAsync(Guid staffId, int managerBranchId)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                (u.BranchId == managerBranchId || _context.EmploymentContracts.Any(c => c.UserId == u.UserId && c.BranchId == managerBranchId && c.Status == "ACTIVE")) &&
                u.Role.RoleName == "STAFF" &&
                (u.Status == "ACTIVE" || u.Status == "PENDING") &&
                u.DeletedAt == null);

        if (staff == null)
            return false;

        staff.PreviousStatus = staff.Status;
        staff.Status = "INACTIVE";
        staff.DeletedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;

        // Revoke active biometric face profile upon account deactivation/soft-delete
        var activeFaceProfiles = await _context.FaceProfiles
            .Where(f => f.UserId == staffId && f.Status == "ACTIVE")
            .ToListAsync();
        foreach (var fp in activeFaceProfiles)
        {
            fp.Status = "REVOKED";
            fp.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreStaffAsync(Guid staffId, int managerBranchId, int? branchId)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                u.DeletedAt != null &&
                u.Status == "INACTIVE");

        if (staff == null)
            return false;

        staff.Status = "ACTIVE";
        staff.DeletedAt = null;
        staff.UpdatedAt = DateTime.UtcNow;

        if (staff.BranchId != managerBranchId && staff.BranchId.HasValue)
        {
            // Keep original branch if not specified
        }

        await _context.SaveChangesAsync();
        return true;
    }
}