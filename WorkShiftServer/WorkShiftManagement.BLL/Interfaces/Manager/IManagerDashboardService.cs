using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerDashboardService
{
    Task<ManagerDashboardResponse> GetAsync(Guid managerId, CancellationToken ct = default);
}
