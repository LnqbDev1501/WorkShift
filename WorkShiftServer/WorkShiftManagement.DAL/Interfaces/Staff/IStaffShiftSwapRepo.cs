using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IStaffShiftSwapRepo
{
    Task<ShiftAssignment?> FindAssignmentAsync(int assignmentId, CancellationToken ct);
    Task<List<ShiftAssignment>> GetCandidateAssignmentsAsync(Guid actor, int branchId, int employeeTypeId, CancellationToken ct);
    Task<(List<ShiftSwapRequest> Items, int Total)> GetPagedAsync(Guid actor, string? status, string direction,
        int pageNumber, int pageSize, CancellationToken ct);
    Task<ShiftSwapRequest?> FindAsync(Guid actor, int requestId, CancellationToken ct);
    Task<ShiftSwapRequest> ExecuteAsync(int sourceId, int targetId, int? requestId,
        Func<ShiftAssignment, ShiftAssignment, ShiftSwapRequest?, Task<ShiftSwapRequest>> action, CancellationToken ct);
    Task<bool> HasPendingAsync(int sourceId, int targetId, CancellationToken ct);
    void Add(ShiftSwapRequest request);
    void Audit(Guid actor, string action, ShiftSwapRequest request, string? oldValue);
}
