using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync();
}
