using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IHolidayPayRuleService
{
    DateOnly Today { get; }

    Task<List<HolidayPayRuleResponse>> ListAsync(CancellationToken ct = default);

    Task<HolidayPayRuleResponse> GetByIdAsync(int id, CancellationToken ct = default);

    Task<HolidayPayRuleResponse> CreateAsync(HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default);

    Task<HolidayPayRuleResponse> UpdateAsync(int id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default);

    Task<bool> DeleteAsync(int id, Guid actor, CancellationToken ct = default);

    Task<HolidayPayRuleResponse> PublishAsync(int id, Guid actor, CancellationToken ct = default);

    Task<HolidayPayRuleResponse> CancelAsync(int id, Guid actor, CancellationToken ct = default);

    Task<bool> SynchronizeAsync(CancellationToken ct = default);
}

