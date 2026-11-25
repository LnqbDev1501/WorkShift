using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.ReadModels.Staff;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Policies.Shifts;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public sealed class StaffShiftRepo(AppDbContext context) : IStaffShiftRepo
{
    public Task<User?> GetStaffAsync(Guid staffId, CancellationToken ct) =>
        context.Users.AsNoTracking().Include(x => x.Role).Include(x => x.Branch)
            .Include(x => x.EmployeeType).SingleOrDefaultAsync(x => x.UserId == staffId, ct);

    // Only the caller's registration is materialized. Capacity and history stay in SQL.
    private static IQueryable<StaffShiftReadModel> WithDetails(IQueryable<WorkShift> query, Guid staffId) =>
        query.AsNoTracking().AsSingleQuery().Select(s => new StaffShiftReadModel
        {
            ShiftId = s.ShiftId, ScheduleWeekId = s.ScheduleWeekId,
            ShiftNameSnapshot = s.ShiftNameSnapshot, ScheduledStartAt = s.ScheduledStartAt,
            ScheduledEndAt = s.ScheduledEndAt, Status = s.Status, Note = s.Note,
            BranchId = s.ScheduleWeek.BranchId, BranchName = s.ScheduleWeek.Branch.BranchName,
            ScheduleWeekStatus = s.ScheduleWeek.Status, RegistrationDeadline = s.ScheduleWeek.RegistrationDeadline,
            Registrations = s.ShiftAssignments.Where(a => a.UserId == staffId)
                .Select(a => new StaffRegistrationReadModel
                {
                    AssignmentId = a.AssignmentId, ShiftId = a.ShiftId,
                    ShiftRequirementId = a.ShiftRequirementId, AssignmentSource = a.AssignmentSource,
                    Status = a.Status, AssignedAt = a.AssignedAt,
                    CancelledAt = a.CancelledAt, CancelReason = a.CancelReason,
                    HasAttendanceOrPayroll = a.Attendance != null || a.PayslipWorkDetails.Any(),
                    HasBlockingRequest = a.LeaveRequestAssignments.Any(l => l.Request.Status == "PENDING" || l.Request.Status == "APPROVED") ||
                        a.TimeAdjustmentRequestDetails.Any(d => d.Request.Status == "PENDING" || d.Request.Status == "APPROVED")
                }).ToList(),
            EmployeeRequirements = s.ShiftEmployeeRequirements.OrderBy(r => r.EmployeeTypeId)
                .Select(r => new StaffRequirementReadModel
                {
                    ShiftRequirementId = r.ShiftRequirementId, EmployeeTypeId = r.EmployeeTypeId,
                    EmployeeTypeName = r.EmployeeType.TypeName, RequiredCount = r.RequiredCount,
                    AssignedCount = s.ShiftAssignments.Count(a => a.ShiftRequirementId == r.ShiftRequirementId &&
                        (a.Status == "ASSIGNED" || a.Status == "COMPLETED"))
                }).ToList()
        });

    public async Task<(List<StaffShiftReadModel> Items, int Total)> GetPagedAsync(Guid staffId, int branchId,
        int? employeeTypeId, bool availableOnly, DateTime? from, DateTime? to,
        int pageNumber, int pageSize, DateTime now, CancellationToken ct)
    {
        var query = context.WorkShifts.Where(s => s.ScheduleWeek.Status != "DRAFT");
        if (availableOnly)
        {
            query = StaffShiftContractPolicy.EligibleShifts(query, context.EmploymentContracts, staffId, branchId, now);
            query = query.Where(s => s.ScheduleWeek.BranchId == branchId && s.Status == "SCHEDULED" &&
                s.ScheduleWeek.Status == "OPEN_REGISTRATION" && s.ScheduleWeek.RegistrationDeadline > now &&
                s.ScheduledStartAt > now && s.ScheduledEndAt > s.ScheduledStartAt &&
                !s.ShiftAssignments.Any(a => a.UserId == staffId && a.Status != "CANCELLED") &&
                s.ShiftEmployeeRequirements.Any(r => r.EmployeeTypeId == employeeTypeId &&
                    r.EmployeeType.Status == "ACTIVE" && r.RequiredCount >
                    s.ShiftAssignments.Count(a => a.ShiftRequirementId == r.ShiftRequirementId &&
                        (a.Status == "ASSIGNED" || a.Status == "COMPLETED"))));
        }
        else
        {
            // Preserve the employee's own schedule after a branch transfer.
            query = query.Where(s => s.ShiftAssignments.Any(a => a.UserId == staffId &&
                (a.Status == "ASSIGNED" || a.Status == "COMPLETED")));
        }
        if (from.HasValue) query = query.Where(s => s.ScheduledStartAt >= from.Value);
        if (to.HasValue) query = query.Where(s => s.ScheduledStartAt < to.Value);
        var total = await query.CountAsync(ct);
        var items = await WithDetails(query.OrderBy(s => s.ScheduledStartAt).ThenBy(s => s.ShiftId)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize), staffId).ToListAsync(ct);
        return (items, total);
    }

    public Task<bool> HasActiveContractAsync(Guid staffId, int branchId, DateTime now, CancellationToken ct) =>
        StaffShiftContractPolicy.ActiveContracts(context.EmploymentContracts, branchId, now)
            .AnyAsync(c => c.UserId == staffId, ct);

    public Task<bool> HasRegistrationContractAsync(Guid staffId, int branchId, int shiftId, DateTime now,
        CancellationToken ct) => StaffShiftContractPolicy.EligibleShifts(
            context.WorkShifts.Where(s => s.ShiftId == shiftId), context.EmploymentContracts,
            staffId, branchId, now).AnyAsync(ct);

    public Task<StaffShiftReadModel?> GetDetailAsync(int shiftId, Guid staffId, int branchId, CancellationToken ct) =>
        WithDetails(context.WorkShifts.Where(s => s.ShiftId == shiftId && s.ScheduleWeek.Status != "DRAFT" &&
            (s.ScheduleWeek.BranchId == branchId || s.ShiftAssignments.Any(a => a.UserId == staffId))), staffId)
            .SingleOrDefaultAsync(ct);
}
