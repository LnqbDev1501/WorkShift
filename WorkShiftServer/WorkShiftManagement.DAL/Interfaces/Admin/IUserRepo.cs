using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin
{
    public interface IUserRepo
    {
        Task<Role?> GetActiveRoleByIdAsync(int roleId);

        Task<bool> ActiveBranchExistsAsync(int branchId);

        Task<bool> BranchHasRoleAsync(int branchId, int roleId);

        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByPhoneAsync(string phone);

        Task<User?> GetByIdentityNoAsync(string identityNo);

        Task<User?> GetByIdAsync(Guid userId);

        Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            string? status,
            int? roleId,
            int? branchId);

        void Create(User user);

        Task SaveChangesAsync();
    }
}
