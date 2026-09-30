using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.AttendancePenaltyRule.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IAttendancePenaltyRuleService
{
    Task<AttendancePenaltyRuleListResponse> GetAttendancePenaltyRulesAsync(
        GetAttendancePenaltyRulesRequest request,
        CancellationToken cancellationToken);

    Task<AttendancePenaltyRuleDetailResponse> CreateAsync(
        CreateAttendancePenaltyRuleRequest request,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<AttendancePenaltyRuleDetailResponse> UpdateAsync(
        int id,
        UpdateAttendancePenaltyRuleRequest request,
        CancellationToken cancellationToken);

    Task<AttendancePenaltyRuleDetailResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}



