using WorkShiftManagement.BLL.DTOs.Accountant.Response;

namespace WorkShiftManagement.BLL.Interfaces.Accountant;

public interface IAccountantDashboardService
{
    Task<AccountantDashboardResponse> GetAsync(Guid accountantId, CancellationToken ct = default);
}
