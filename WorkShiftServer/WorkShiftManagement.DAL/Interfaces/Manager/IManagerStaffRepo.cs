using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IManagerStaffRepo
{
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedStaffAsync(
        int managerBranchId,
        string? search,
        string? status,
        int pageNumber,
        int pageSize);

    Task<User?> GetStaffByIdAsync(Guid staffId, int managerBranchId);

    Task<bool> ExistsByEmailAsync(string email);

    Task<bool> ExistsByPhoneAsync(string phone, Guid? excludeUserId = null);

    Task<bool> ExistsByIdentityNoAsync(string identityNo, Guid? excludeUserId = null);

    Task<User> CreateStaffAsync(User staff, Guid managerId);

    Task<User?> UpdateStaffAsync(Guid staffId, int managerBranchId, string fullName, string phone, string identityNo, string gender, DateOnly? dateOfBirth, string? address);

    Task<bool> SoftDeleteStaffAsync(Guid staffId, int managerBranchId);

    Task<bool> RestoreStaffAsync(Guid staffId, int managerBranchId, int? branchId);
}