using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;
using WorkShiftManagement.DAL.Models.Admin;

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
        int pageSize)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.BranchId == managerBranchId && u.Role.RoleName == "STAFF");

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
            else
                query = query.Where(u => u.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync();

        var items = await query
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
                u.BranchId == managerBranchId &&
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
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IdentityNo == trimmed && u.DeletedAt == null);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.UserId != excludeUserId.Value);

        return await query.AnyAsync();
    }

    public async Task<User> CreateStaffAsync(User staff, Guid managerId)
    {
        _context.Users.Add(staff);
        await _context.SaveChangesAsync();
        return staff;
    }

public async Task<User?> UpdateStaffAsync(Guid staffId, int managerBranchId, string fullName, string phone, string identityNo, string gender, DateOnly? dateOfBirth, string? address)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                u.BranchId == managerBranchId &&
                u.Role.RoleName == "STAFF" &&
                u.DeletedAt == null);

        if (staff == null)
            return null;

        staff.FullName = fullName.Trim();
        staff.Phone = phone.Trim();
        staff.IdentityNo = identityNo.Trim();
        staff.Gender = gender.Trim().ToUpperInvariant();
        staff.DateOfBirth = dateOfBirth ?? default;
        staff.Address = address?.Trim();
        staff.UpdatedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return staff;
    }

    public async Task<bool> SoftDeleteStaffAsync(Guid staffId, int managerBranchId)
    {
        var staff = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.UserId == staffId &&
                u.BranchId == managerBranchId &&
                u.Role.RoleName == "STAFF" &&
                u.Status == "ACTIVE" &&
                u.DeletedAt == null);

        if (staff == null)
            return false;

        staff.PreviousStatus = staff.Status;
        staff.Status = "INACTIVE";
        staff.DeletedAt = DateTime.UtcNow;
        staff.UpdatedAt = DateTime.UtcNow;

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