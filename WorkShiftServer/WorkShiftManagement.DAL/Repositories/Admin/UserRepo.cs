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
                .Where(user => user.DeletedAt == null);

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
                query = query.Where(user => user.BranchId == branchId.Value);

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
    }
}
