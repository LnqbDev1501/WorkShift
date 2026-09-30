using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public sealed class ShiftCancellationRequestService(
    IShiftCancellationRequestRepo repo, IScheduleWeekRepo weekRepo) : IShiftCancellationRequestService
{
    public async Task<PagedResponse<ShiftCancellationRequestResponse>> GetListAsync(
        Guid actor, ShiftCancellationRequestListQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var branchId = await GetBranchAsync(actor);
        if (query.PageNumber < 1 || query.PageNumber > int.MaxValue / 100 || query.PageSize is < 1 or > 100)
            throw new ArgumentException("Page number must be positive and page size must be between 1 and 100.");
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();
        if (status != null && status is not ("PENDING" or "APPROVED" or "REJECTED" or "CANCELLED"))
            throw new ArgumentException("Status must be PENDING, APPROVED, REJECTED or CANCELLED.");
        var (items, total) = await repo.GetListAsync(branchId, status, query.PageNumber, query.PageSize, ct);
        return new()
        {
            Items = items.Select(Map).ToList(), TotalItems = total,
            PageNumber = query.PageNumber, PageSize = query.PageSize
        };
    }

    public async Task<ShiftCancellationRequestResponse> GetByIdAsync(Guid actor, int id, CancellationToken ct = default)
    {
        ValidateId(id);
        var branchId = await GetBranchAsync(actor);
        return Map(await repo.GetByIdAsync(id, branchId, ct)
            ?? throw new KeyNotFoundException("Shift cancellation request not found."));
    }

    public Task<ShiftCancellationRequestResponse> ApproveAsync(
        Guid actor, int id, ReviewShiftCancellationRequest request, CancellationToken ct = default) =>
        ReviewAsync(actor, id, request, true, ct);

    public Task<ShiftCancellationRequestResponse> RejectAsync(
        Guid actor, int id, ReviewShiftCancellationRequest request, CancellationToken ct = default) =>
        ReviewAsync(actor, id, request, false, ct);

    private async Task<ShiftCancellationRequestResponse> ReviewAsync(
        Guid actor, int id, ReviewShiftCancellationRequest input, bool approve, CancellationToken ct)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(input);
        if (input.ReviewNote?.Length > 500)
            throw new ArgumentException("Review note cannot exceed 500 characters.");
        var note = string.IsNullOrWhiteSpace(input.ReviewNote) ? null : input.ReviewNote.Trim();
        if (!approve && note == null)
            throw new ArgumentException("A reason is required when rejecting a request.");
        var branchId = await GetBranchAsync(actor);
        var result = await repo.ReviewAsync(id, branchId, request =>
        {
            // Validate the entire request before mutating any assignment.
            var now = DateTime.UtcNow;
            ValidateReview(request, branchId, now, approve);
            if (approve)
            {
                foreach (var link in request.LeaveRequestAssignments)
                {
                    var assignment = link.Assignment;
                    assignment.Status = "CANCELLED";
                    assignment.CancelledBy = actor;
                    assignment.CancelledAt = now;
                    assignment.CancelReason = request.Reason ?? note ?? "Approved shift cancellation request.";
                    assignment.UpdatedAt = now;
                }
            }
            request.Status = approve ? "APPROVED" : "REJECTED";
            request.ReviewedBy = actor;
            request.ReviewedAt = now;
            request.ReviewNote = note;
            request.UpdatedAt = now;
        }, ct);
        return Map(result);
    }

    private static void ValidateReview(EmployeeRequest request, int branchId, DateTime now, bool approve)
    {
        if (request.RequestType != "LEAVE" || request.BranchId != branchId)
            throw new KeyNotFoundException("Shift cancellation request not found.");
        if (request.Status != "PENDING" || request.ReviewedAt.HasValue || request.ReviewedBy.HasValue)
            throw new InvalidOperationException("Only an unreviewed PENDING request can be processed.");
        if (request.LeaveRequestAssignments.Count == 0 ||
            request.LeaveRequestAssignments.Select(x => x.AssignmentId).Distinct().Count() != request.LeaveRequestAssignments.Count)
            throw new InvalidOperationException("The request must contain distinct assignments.");
        foreach (var link in request.LeaveRequestAssignments)
        {
            var assignment = link.Assignment;
            var shift = assignment.Shift;
            if (assignment.UserId != request.UserId || shift.ScheduleWeek.BranchId != branchId)
                throw new InvalidOperationException("An assignment does not belong to the request's staff member or branch.");
            if (assignment.Status != "ASSIGNED")
                throw new InvalidOperationException($"Assignment {assignment.AssignmentId} is no longer ASSIGNED.");
            if (shift.Status != "SCHEDULED" ||
                shift.ScheduleWeek.Status is not ("OPEN_REGISTRATION" or "REGISTRATION_CLOSED"))
                throw new InvalidOperationException($"Shift {shift.ShiftId} is not available for request review.");
            if (approve && (shift.ScheduledStartAt <= now || assignment.Attendance != null || assignment.PayslipWorkDetails.Count != 0))
                throw new InvalidOperationException($"Assignment {assignment.AssignmentId} has started or has attendance/payroll data and cannot be cancelled.");
        }
    }

    private async Task<int> GetBranchAsync(Guid actor)
    {
        if (actor == Guid.Empty) throw new UnauthorizedAccessException("The authenticated user ID is invalid.");
        var manager = await weekRepo.GetActiveManagerAsync(actor)
            ?? throw new UnauthorizedAccessException("The account is not an active manager.");
        if (manager.Role.Status != "ACTIVE" || manager.Branch?.Status != "ACTIVE" || manager.BranchId is not int branchId)
            throw new UnauthorizedAccessException("An active manager role and active assigned branch are required.");
        return branchId;
    }

    private static void ValidateId(int id)
    {
        if (id <= 0) throw new ArgumentException("Request ID must be greater than zero.");
    }

    private static ShiftCancellationRequestResponse Map(EmployeeRequest request) => new()
    {
        RequestId = request.RequestId, BranchId = request.BranchId,
        UserId = request.UserId, FullName = request.User.FullName,
        Reason = request.Reason, Status = request.Status,
        CreatedAt = request.CreatedAt, UpdatedAt = request.UpdatedAt,
        ReviewedBy = request.ReviewedBy, ReviewedAt = request.ReviewedAt, ReviewNote = request.ReviewNote,
        Assignments = request.LeaveRequestAssignments.OrderBy(x => x.Assignment.Shift.ScheduledStartAt)
            .ThenBy(x => x.AssignmentId).Select(link => new ShiftCancellationAssignmentResponse
            {
                AssignmentId = link.AssignmentId, AssignmentStatus = link.Assignment.Status,
                ShiftId = link.Assignment.ShiftId, ScheduleWeekId = link.Assignment.Shift.ScheduleWeekId,
                ShiftName = link.Assignment.Shift.ShiftNameSnapshot,
                ScheduledStartAt = link.Assignment.Shift.ScheduledStartAt,
                ScheduledEndAt = link.Assignment.Shift.ScheduledEndAt,
                ShiftStatus = link.Assignment.Shift.Status,
                EmployeeTypeId = link.Assignment.ShiftEmployeeRequirement?.EmployeeTypeId,
                EmployeeTypeName = link.Assignment.ShiftEmployeeRequirement?.EmployeeType.TypeName
            }).ToList()
    };
}
