using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IDashboardRepo
{
    Task<DashboardData> GetAsync(DateTime todayStartUtc, DateTime tomorrowStartUtc);
}
