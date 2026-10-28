using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;
public interface IPayrollRevisionRepo
{
    Task<Payroll?> GetAsync(int id, int? branchId, CancellationToken ct);
    Task<List<Payroll>> VersionsAsync(Guid userId, int periodId, CancellationToken ct);
}
