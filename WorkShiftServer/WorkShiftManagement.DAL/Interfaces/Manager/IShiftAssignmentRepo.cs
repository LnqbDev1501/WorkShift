using WorkShiftManagement.DAL.Entities;
namespace WorkShiftManagement.DAL.Interfaces.Manager;

public interface IShiftAssignmentRepo
{
    Task<ShiftAssignment?> FindAsync(int assignmentId, int branchId, CancellationToken ct);
    Task<ShiftAssignment> ExecuteAsync(int shiftId, Guid staffId, int branchId,
        Func<WorkShift, User, Task<ShiftAssignment>> action, CancellationToken ct);
    Task<List<WorkShift>> GetStaffShiftsAsync(Guid staffId, DateTime from, DateTime to, CancellationToken ct);
    Task<List<WorkingTimeRule>> GetRulesAsync(CancellationToken ct);
    Task<bool> HasApprovedLeaveAsync(Guid staffId, DateTime start, DateTime end, CancellationToken ct);
    Task CancelPendingSwapsAsync(int assignmentId, Guid actor, DateTime cancelledAt, string reason, CancellationToken ct);
    void Add(ShiftAssignment assignment);
    void AddNotification(Notification notification);
    void Audit(Guid actor, string action, ShiftAssignment assignment, string? oldValue);
}
