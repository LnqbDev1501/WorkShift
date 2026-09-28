using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendanceRule.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IAttendanceRuleService
{
    Task<AttendanceRuleResponse> CreateAsync(CreateAttendanceRuleRequest request, Guid createdBy, CancellationToken cancellationToken);
    Task<AttendanceRuleResponse> UpdateAsync(int id, UpdateAttendanceRuleRequest request, CancellationToken cancellationToken);
    Task<AttendanceRuleResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<AttendanceRuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<AttendanceRuleListResponse> GetAttendanceRulesAsync(GetAttendanceRulesRequest request, CancellationToken cancellationToken);
}
