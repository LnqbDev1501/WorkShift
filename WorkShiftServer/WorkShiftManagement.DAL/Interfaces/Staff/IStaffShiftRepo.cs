using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.ReadModels.Staff;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffShiftRepo
{
    Task<User?> GetStaffAsync(Guid staffId, CancellationToken ct);
    Task<bool> HasActiveContractAsync(Guid staffId, int branchId, DateTime now, CancellationToken ct);
    Task<bool> HasRegistrationContractAsync(Guid staffId, int branchId, int shiftId, DateTime now, CancellationToken ct);
    Task<(List<StaffShiftReadModel> Items, int Total)> GetPagedAsync(Guid staffId, int branchId,
        int? employeeTypeId, bool availableOnly, DateTime? from, DateTime? to,
        int pageNumber, int pageSize, DateTime now, CancellationToken ct);
    Task<StaffShiftReadModel?> GetDetailAsync(int shiftId, Guid staffId, int branchId, CancellationToken ct);
}
