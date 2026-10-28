using Microsoft.EntityFrameworkCore.Storage;

namespace WorkShiftManagement.DAL.Interfaces.Admin;
public interface IPayrollChangeTransaction
{
    Task<IDbContextTransaction> BeginPayrollChangeAsync(CancellationToken ct = default);
}
