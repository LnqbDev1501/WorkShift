using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Staff;

namespace WorkShiftManagement.DAL.Repositories.Staff;

public class AttendanceRepo : IAttendanceRepo
{
    private readonly AppDbContext _context;

    public AttendanceRepo(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == "ACTIVE", cancellationToken);
    }

    public Task<EmploymentContract?> GetActiveContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        return _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch)
            .FirstOrDefaultAsync(c =>
                c.UserId == userId &&
                c.Status == "ACTIVE" &&
                c.StartDate <= today &&
                (c.EndDate == null || c.EndDate >= today),
                cancellationToken);
    }

    public Task<EmploymentContract?> GetLatestContractByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.EmploymentContracts
            .AsNoTracking()
            .Include(c => c.Branch)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<FaceProfile?> GetActiveFaceProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.FaceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.Status == "ACTIVE", cancellationToken);
    }

    public Task<List<ShiftAssignment>> GetEligibleShiftAssignmentsForDateAsync(Guid userId, DateOnly targetDate, CancellationToken cancellationToken = default)
    {
        var startOfDay = DateTime.SpecifyKind(targetDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endOfDay = DateTime.SpecifyKind(targetDate.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        return _context.ShiftAssignments
            .AsNoTracking()
            .Include(a => a.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(a => a.User)
            .Include(a => a.Attendance)
            .Where(a =>
                a.UserId == userId &&
                a.Status == "ASSIGNED" &&
                a.Shift.Status != "CANCELLED" &&
                a.Shift.ScheduledStartAt >= startOfDay &&
                a.Shift.ScheduledStartAt <= endOfDay)
            .OrderBy(a => a.Shift.ScheduledStartAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ShiftAssignment?> GetAssignmentWithDetailsAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.ShiftAssignments
            .Include(a => a.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(a => a.User)
            .Include(a => a.Attendance)
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);
    }

    public Task<Attendance?> GetAttendanceByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.Attendances
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId, cancellationToken);
    }

    public Task<LeaveRequestAssignment?> GetApprovedLeaveForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.LeaveRequestAssignments
            .AsNoTracking()
            .Include(l => l.Request)
            .FirstOrDefaultAsync(l =>
                l.AssignmentId == assignmentId &&
                l.Request.RequestType == "LEAVE" &&
                l.Request.Status == "APPROVED",
                cancellationToken);
    }

    public Task<TimeAdjustmentRequestDetail?> GetApprovedLateArrivalForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.TimeAdjustmentRequestDetails
            .AsNoTracking()
            .Include(t => t.Request)
            .FirstOrDefaultAsync(t =>
                t.AssignmentId == assignmentId &&
                t.Request.RequestType == "LATE_ARRIVAL" &&
                t.Request.Status == "APPROVED",
                cancellationToken);
    }

    public Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendanceRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Status == "ACTIVE" || r.Status == "PUBLISHED", cancellationToken);
    }

    public Task<AttendancePenaltyRule?> GetActiveAttendancePenaltyRuleWithLevelsAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendancePenaltyRules
            .AsNoTracking()
            .Include(r => r.AttendancePenaltyLevels)
            .FirstOrDefaultAsync(r => r.Status == "ACTIVE" || r.Status == "PUBLISHED", cancellationToken);
    }

    public Task<FaceVerificationSession?> GetFaceVerificationSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return _context.FaceVerificationSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public Task<FaceVerificationSession?> GetLatestSessionByUserIdAsync(Guid userId, string actionType, CancellationToken cancellationToken = default)
    {
        return _context.FaceVerificationSessions
            .Where(s => s.UserId == userId && s.ActionType == actionType)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FaceVerificationSession> CreateFaceVerificationSessionAsync(FaceVerificationSession session, CancellationToken cancellationToken = default)
    {
        _context.FaceVerificationSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<Attendance> CreateAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default)
    {
        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync(cancellationToken);
        return attendance;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
