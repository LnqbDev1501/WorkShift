using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin
{
    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;

        public UserRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetActiveRoleByIdAsync(int roleId)
        {
            return await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(role =>
                    role.RoleId == roleId &&
                    role.Status == "ACTIVE");
        }

        public async Task<IReadOnlyList<Role>> GetAssignableRolesAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(role => role.Status == "ACTIVE" &&
                    (role.RoleName.ToUpper() == "MANAGER" || role.RoleName.ToUpper() == "ACCOUNTANT" || role.RoleName.ToUpper() == "ADMIN"))
                .OrderBy(role => role.RoleId)
                .ToListAsync();
        }

        public async Task<bool> ActiveBranchExistsAsync(int branchId)
        {
            return await _context.Branches
                .AsNoTracking()
                .AnyAsync(branch =>
                    branch.BranchId == branchId &&
                    branch.Status == "ACTIVE");
        }

        public async Task<bool> BranchHasRoleAsync(int branchId, int roleId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.BranchId == branchId &&
                    user.RoleId == roleId &&
                    user.Status != "INACTIVE");
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Phone == phone);
        }

        public async Task<User?> GetByIdentityNoAsync(string identityNo)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.IdentityNo == identityNo);
        }

        public void Create(User user)
        {
            _context.Users.Add(user);
        }

        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user => user.UserId == userId);
        }

        public async Task<User?> GetDetailByIdAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .Include(user => user.Branch)
                .Include(user => user.PreviousBranch)
                .Include(user => user.EmployeeType)
                .FirstOrDefaultAsync(user => user.UserId == userId);
        }

        public async Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            string? status,
            int? roleId,
            int? branchId)
        {
            var query = _context.Users
                .AsNoTracking()
                .Include(user => user.Role)
                .Include(user => user.Branch)
                .Include(user => user.PreviousBranch)
                .Where(user => user.Role.RoleName.ToUpper() == "MANAGER" ||
                               user.Role.RoleName.ToUpper() == "ACCOUNTANT" ||
                               user.Role.RoleName.ToUpper() == "ADMIN")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search}%";
                query = query.Where(user =>
                    EF.Functions.ILike(user.FullName, pattern) ||
                    EF.Functions.ILike(user.Email, pattern) ||
                    EF.Functions.ILike(user.Phone, pattern) ||
                    EF.Functions.ILike(user.IdentityNo, pattern));
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(user => user.Status == status);
            if (roleId.HasValue)
                query = query.Where(user => user.RoleId == roleId.Value);
            if (branchId.HasValue)
                query = query.Where(user =>
                    user.BranchId == branchId.Value ||
                    (user.DeletedAt != null && user.PreviousBranchId == branchId.Value));

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(user => user.CreatedAt)
                .ThenBy(user => user.UserId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalItems);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetActiveProfileByIdAsync(Guid userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId && u.Status == "ACTIVE" && u.DeletedAt == null)
                .Select(u => new User
                {
                    UserId = u.UserId,
                    RoleId = u.RoleId,
                    BranchId = u.BranchId,
                    EmployeeTypeId = u.EmployeeTypeId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IdentityNo = u.IdentityNo,
                    Gender = u.Gender,
                    DateOfBirth = u.DateOfBirth,
                    AvatarUrl = u.AvatarUrl,
                    Address = u.Address,
                    Status = u.Status,
                    HasPassword = u.HasPassword,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Role = u.Role,
                    Branch = u.Branch,
                    EmployeeType = u.EmployeeType
                })
                .SingleOrDefaultAsync();
        }

        public async Task<User?> GetByIdWithTrackingAsync(Guid userId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId = null)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Email.ToLower() == normalized && u.UserId != excludeUserId && u.DeletedAt == null);
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeUserId = null)
        {
            var normalized = phone.Trim();
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Phone == normalized && u.UserId != excludeUserId && u.DeletedAt == null);
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await _context.Roles.FindAsync(roleId);
        }

        public async Task<bool> HasPendingPayrollAsync(int branchId)
        {
            var hasPayrollPeriods = await _context.PayrollPeriods
                .AnyAsync(p => p.BranchId == branchId && (p.Status == "CALCULATED" || p.Status == "SUBMITTED" || p.Status == "REVISION_REQUIRED"));

            var hasPayrolls = await _context.Payrolls
                .AnyAsync(p => p.BranchId == branchId && (p.Status == "CALCULATED" || p.Status == "SUBMITTED" || p.Status == "REJECTED"));

            return hasPayrollPeriods || hasPayrolls;
        }

        public async Task<bool> IsRoleOccupiedInBranchAsync(int branchId, int roleId, Guid excludeUserId)
        {
            return await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserId != excludeUserId
                            && u.BranchId == branchId
                            && u.RoleId == roleId
                            && u.Status != "INACTIVE"
                            && u.DeletedAt == null);
        }

        public async Task<Guid> GetFirstActiveAdminIdAsync()
        {
            return await _context.Users
                .Where(u => u.Role.RoleName == "ADMIN" && u.DeletedAt == null)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
        }

        public void AddNotification(WorkShiftManagement.DAL.Entities.Notification notification)
        {
            _context.Notifications.Add(notification);
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task LoadRoleAndBranchAsync(User user)
        {
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();
            if (user.BranchId.HasValue)
            {
                await _context.Entry(user).Reference(u => u.Branch).LoadAsync();
            }
        }
    }
}

