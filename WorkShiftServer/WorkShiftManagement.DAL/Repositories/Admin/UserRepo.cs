using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Admin
{
    public class UserRepo
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

        public async Task CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
