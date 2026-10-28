using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public static class ShiftSwapExpiryPolicy
{
    public static DateTime ExpiresAt(ShiftSwapRequest request) =>
        request.SourceAssignment.Shift.ScheduledStartAt <= request.TargetAssignment.Shift.ScheduledStartAt
            ? request.SourceAssignment.Shift.ScheduledStartAt
            : request.TargetAssignment.Shift.ScheduledStartAt;

    public static IQueryable<ShiftSwapRequest> Due(IQueryable<ShiftSwapRequest> requests, DateTime now) =>
        requests.Where(r => r.Status == "PENDING" &&
            (r.SourceAssignment.Shift.ScheduledStartAt <= now || r.TargetAssignment.Shift.ScheduledStartAt <= now));

    public static bool TryExpire(ShiftSwapRequest request, DateTime now)
    {
        if (request.Status != "PENDING" || now < ExpiresAt(request)) return false;
        request.Status = "EXPIRED";
        request.UpdatedAt = now;
        // Expiry is automatic, not a staff response. Preserve the response fields.
        return true;
    }
}
