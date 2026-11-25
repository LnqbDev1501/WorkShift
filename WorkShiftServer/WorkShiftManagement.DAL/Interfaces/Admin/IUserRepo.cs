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

        Task<IReadOnlyList<Role>> GetAssignableRolesAsync();

        Task<bool> ActiveBranchExistsAsync(int branchId);

        Task<bool> BranchHasRoleAsync(int branchId, int roleId);

        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByPhoneAsync(string phone);

        Task<User?> GetByIdentityNoAsync(string identityNo);

        Task<User?> GetByIdAsync(Guid userId);

        Task<User?> GetDetailByIdAsync(Guid userId);

        Task<User?> GetActiveProfileByIdAsync(Guid userId);

        Task<User?> GetByIdWithTrackingAsync(Guid userId);

        Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId = null);

        Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeUserId = null);

        Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? search,
            string? status,
            int? roleId,
            int? branchId);

        void Create(User user);

        void MarkIdentityNoModified(User user);

        Task SaveChangesAsync();

        Task<Role?> GetRoleByIdAsync(int roleId);

        Task<bool> HasPendingPayrollAsync(int branchId);

        Task<bool> IsRoleOccupiedInBranchAsync(int branchId, int roleId, Guid excludeUserId);

        Task<Guid> GetFirstActiveAdminIdAsync();

        void AddNotification(WorkShiftManagement.DAL.Entities.Notification notification);

        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();

        Task LoadRoleAndBranchAsync(User user);

        Task<bool> HasActiveAccountByEmailAsync(string email);

        Task<User?> GetStaffSetupUserAsync(Guid userId);

        Task ActivateStaffAccountAsync(Guid userId);
    }
}
