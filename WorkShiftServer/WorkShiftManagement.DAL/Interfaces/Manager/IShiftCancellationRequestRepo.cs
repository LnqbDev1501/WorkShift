using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IShiftCancellationRequestRepo
{
    Task<(List<EmployeeRequest> Items, int Total)> GetListAsync(int branchId, string? status, int page, int size, CancellationToken ct);
    Task<EmployeeRequest?> GetByIdAsync(int id, int branchId, CancellationToken ct);
    Task<EmployeeRequest> ReviewAsync(int id, int branchId, Action<EmployeeRequest> review, CancellationToken ct);
}
