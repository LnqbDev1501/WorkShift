using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Staff;

public interface IAttendanceRepo
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmploymentContract?> GetActiveContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FaceProfile?> GetActiveFaceProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<ShiftAssignment>> GetEligibleShiftAssignmentsForDateAsync(Guid userId, DateOnly targetDate, CancellationToken cancellationToken = default);
    Task<ShiftAssignment?> GetAssignmentWithDetailsAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<Attendance?> GetAttendanceByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default);

    Task<LeaveRequestAssignment?> GetApprovedLeaveForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);
    Task<TimeAdjustmentRequestDetail?> GetApprovedLateArrivalForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default);

    Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default);
    Task<AttendancePenaltyRule?> GetActiveAttendancePenaltyRuleWithLevelsAsync(CancellationToken cancellationToken = default);

    Task<FaceVerificationSession?> GetFaceVerificationSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<FaceVerificationSession?> GetLatestSessionByUserIdAsync(Guid userId, string actionType, CancellationToken cancellationToken = default);
    Task<FaceVerificationSession> CreateFaceVerificationSessionAsync(FaceVerificationSession session, CancellationToken cancellationToken = default);

    Task<Attendance> CreateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
