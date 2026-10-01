using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IScheduleWeekService
{
    Task<List<ScheduleWeekSummaryResponse>> GetListAsync(Guid managerId, ScheduleWeekListQuery query);
    Task<ScheduleWeekDetailResponse> GetByIdAsync(Guid managerId, int scheduleWeekId);
    Task<ScheduleWeekDetailResponse> CreateAsync(Guid managerId, CreateScheduleWeekRequest request);
    Task<ScheduleWeekDetailResponse> UpdateAsync(Guid managerId, int scheduleWeekId, UpdateScheduleWeekRequest request);
    Task DeleteAsync(Guid managerId, int scheduleWeekId);
    Task<ScheduleWeekDetailResponse> OpenRegistrationAsync(Guid managerId, int scheduleWeekId);
    Task<ScheduleWeekDetailResponse> CloseRegistrationAsync(Guid managerId, int scheduleWeekId);
}
