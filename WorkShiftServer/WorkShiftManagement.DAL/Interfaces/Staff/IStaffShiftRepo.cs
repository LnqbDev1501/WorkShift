using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffShiftRepo
{
    Task<User?> GetStaffAsync(Guid staffId, CancellationToken ct);
    Task<(List<WorkShift> Items, int Total)> GetPagedAsync(Guid staffId, int branchId,
        int? employeeTypeId, bool availableOnly, DateTime? from, DateTime? to,
        int pageNumber, int pageSize, DateTime now, CancellationToken ct);
    Task<WorkShift?> GetDetailAsync(int shiftId, Guid staffId, int branchId, CancellationToken ct);
}
