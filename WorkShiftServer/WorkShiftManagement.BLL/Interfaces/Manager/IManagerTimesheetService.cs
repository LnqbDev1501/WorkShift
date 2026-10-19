using System;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IManagerTimesheetService
{
    Task<ManagerTimesheetPagedResponse<ManagerWeeklyTimesheetSummaryResponse>> GetWeeklyTimesheetsAsync(
        Guid managerId,
        ManagerTimesheetListQuery query,
        CancellationToken cancellationToken = default);

    Task<ManagerWeeklyTimesheetDetailResponse?> GetWeeklyTimesheetDetailAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default);

    Task<ManagerWeeklyTimesheetDetailResponse> GenerateWeeklyTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default);

    Task<ManagerWeeklyTimesheetDetailResponse> LockWeeklyTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default);

    Task<ManagerWeeklyTimesheetDetailResponse> ReopenStaffTimesheetAsync(
        Guid managerId,
        int scheduleWeekId,
        int timesheetId,
        CancellationToken cancellationToken = default);

    Task<ManagerTimesheetAdjustmentPagedResponse> GetTimesheetAdjustmentsAsync(
        Guid managerId,
        ManagerTimesheetAdjustmentListQuery query,
        CancellationToken cancellationToken = default);

    Task<ManagerTimesheetAdjustmentDto?> GetAdjustmentDetailAsync(
        Guid managerId,
        int adjustmentRequestId,
        CancellationToken cancellationToken = default);

    Task<ManagerTimesheetAdjustmentDto> ApproveAdjustmentRequestAsync(
        Guid managerId,
        int adjustmentRequestId,
        CancellationToken cancellationToken = default);

    Task<ManagerTimesheetAdjustmentDto> RejectAdjustmentRequestAsync(
        Guid managerId,
        int adjustmentRequestId,
        RejectAdjustmentRequestDto dto,
        CancellationToken cancellationToken = default);

    Task AutoLockExpiredTimesheetsAsync(CancellationToken cancellationToken = default);

    Task<(byte[] FileBytes, string FileName)> ExportWeeklyTimesheetExcelAsync(
        Guid managerId,
        int scheduleWeekId,
        CancellationToken cancellationToken = default);
}
