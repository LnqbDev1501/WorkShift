using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public static class StaffShiftContractPolicy
{
    public static IQueryable<WorkShift> EligibleShifts(
        IQueryable<WorkShift> shifts, IQueryable<EmploymentContract> contracts,
        Guid staffId, int branchId, DateTime now)
    {
        var today = DateOnly.FromDateTime(now.AddHours(7));
        var activeContracts = contracts.Where(c => c.UserId == staffId && c.BranchId == branchId &&
            c.Status == "ACTIVE" && c.StaffAcceptedAt != null && c.StaffAcceptedAt <= now &&
            c.StartDate <= today && (!c.EndDate.HasValue || c.EndDate >= today) &&
            (!c.TerminationEffectiveAt.HasValue || DateOnly.FromDateTime(c.TerminationEffectiveAt.Value) > today));

        // Contract dates are Vietnam calendar dates. EndDate is inclusive; the termination date is exclusive.
        // A shift ending exactly at midnight does not occupy the following calendar day.
        return shifts.Where(s => s.ScheduleWeek.BranchId == branchId &&
            s.ScheduledEndAt > s.ScheduledStartAt && activeContracts.Any(c =>
                c.StartDate <= DateOnly.FromDateTime(s.ScheduledStartAt.AddHours(7)) &&
                (!c.EndDate.HasValue || DateOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) <= c.EndDate.Value ||
                    (DateOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) == c.EndDate.Value.AddDays(1) &&
                        TimeOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) == TimeOnly.MinValue)) &&
                (!c.TerminationEffectiveAt.HasValue ||
                    DateOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) < DateOnly.FromDateTime(c.TerminationEffectiveAt.Value) ||
                    (DateOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) == DateOnly.FromDateTime(c.TerminationEffectiveAt.Value) &&
                        TimeOnly.FromDateTime(s.ScheduledEndAt.AddHours(7)) == TimeOnly.MinValue))));
    }
}
