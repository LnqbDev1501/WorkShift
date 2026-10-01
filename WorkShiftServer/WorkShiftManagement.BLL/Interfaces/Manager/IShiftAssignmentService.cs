using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
namespace WorkShiftManagement.BLL.Interfaces.Manager;

public interface IShiftAssignmentService
{
    Task<ShiftAssignmentResponse> AssignAsync(Guid actor, int shiftId, AssignShiftRequest request, CancellationToken ct = default);
    Task<ShiftAssignmentResponse> CancelAsync(Guid actor, int assignmentId, CancelShiftAssignmentRequest request, CancellationToken ct = default);
}
