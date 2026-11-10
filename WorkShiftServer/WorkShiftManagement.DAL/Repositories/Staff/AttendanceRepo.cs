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

    public async Task<(FaceProfile Profile, float[] Embedding)?> GetActiveFaceProfileWithEmbeddingByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _context.FaceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.Status == "ACTIVE", cancellationToken);

        if (profile == null)
        {
            return null;
        }

        var connection = _context.Database.GetDbConnection();
        var currentTransaction = _context.Database.CurrentTransaction;
        bool openedHere = currentTransaction == null
            && connection.State != System.Data.ConnectionState.Open;

        if (openedHere)
        {
            await _context.Database.OpenConnectionAsync(cancellationToken);
        }

        object? embeddingRaw;
        try
        {
            await using var cmd = connection.CreateCommand();
            if (currentTransaction != null)
            {
                cmd.Transaction = currentTransaction.GetDbTransaction();
            }

            cmd.CommandText = "SELECT face_embedding::text FROM public.face_profiles WHERE face_profile_id = @id LIMIT 1;";
            var param = cmd.CreateParameter();
            param.ParameterName = "@id";
            param.Value = profile.FaceProfileId;
            cmd.Parameters.Add(param);

            embeddingRaw = await cmd.ExecuteScalarAsync(cancellationToken);
        }
        finally
        {
            if (openedHere)
            {
                await _context.Database.CloseConnectionAsync();
            }
        }

        if (embeddingRaw == null)
        {
            return null;
        }

        var str = embeddingRaw.ToString()?.Trim('[', ']');
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        var parts = str.Split(',');
        var embedding = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var val))
            {
                embedding[i] = val;
            }
        }

        return (profile, embedding);
    }

    public Task<List<ShiftAssignment>> GetEligibleShiftAssignmentsForDateAsync(Guid userId, DateOnly targetDate, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        // Target date is Vietnam local date (UTC+7). 
        // 00:00:00 in Vietnam = 17:00:00 (previous day) in UTC
        // 23:59:59 in Vietnam = 16:59:59 in UTC
        var startOfDayUtc = DateTime.SpecifyKind(targetDate.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
        var endOfDayUtc = DateTime.SpecifyKind(targetDate.ToDateTime(TimeOnly.MaxValue).AddHours(-7), DateTimeKind.Utc);

        IQueryable<ShiftAssignment> query = _context.ShiftAssignments;
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(a => a.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(a => a.User)
            .Include(a => a.Attendance)
                .ThenInclude(att => att!.AttendancePenaltyRecords)
            .Where(a =>
                a.UserId == userId &&
                (a.Status == "ASSIGNED" || a.Status == "IN_PROGRESS" || a.Status == "COMPLETED") &&
                a.Shift.Status != "CANCELLED" &&
                a.Shift.ScheduledStartAt >= startOfDayUtc &&
                a.Shift.ScheduledStartAt <= endOfDayUtc)
            .OrderBy(a => a.Shift.ScheduledStartAt)
            .ThenBy(a => a.AssignmentId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ShiftAssignment>> GetShiftAssignmentsByDateRangeAsync(Guid userId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var startUtc = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue).AddHours(-7), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(toDate.ToDateTime(TimeOnly.MaxValue).AddHours(-7), DateTimeKind.Utc);

        return _context.ShiftAssignments
            .Include(a => a.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(a => a.User)
            .Include(a => a.Attendance)
                .ThenInclude(att => att!.AttendancePenaltyRecords)
            .Where(a =>
                a.UserId == userId &&
                a.Shift.Status != "CANCELLED" &&
                a.Shift.ScheduledStartAt >= startUtc &&
                a.Shift.ScheduledStartAt <= endUtc)
            .OrderByDescending(a => a.Shift.ScheduledStartAt)
            .ThenByDescending(a => a.AssignmentId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ShiftAssignment>> GetExpiredAssignmentsPendingFinalizationAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return _context.ShiftAssignments
            .Include(sa => sa.User)
            .Include(sa => sa.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(sa => sa.Attendance)
                .ThenInclude(att => att!.AttendancePenaltyRecords)
            .Where(sa =>
                sa.Status != "CANCELLED" &&
                sa.Shift.Status != "CANCELLED" &&
                sa.Shift.ScheduleWeek.Status != "CANCELLED" &&
                sa.Shift.ScheduledEndAt < nowUtc &&
                !sa.LeaveRequestAssignments.Any(l => l.Request.RequestType == "LEAVE" && l.Request.Status == "APPROVED") &&
                (
                    sa.Attendance == null ||
                    (!sa.Attendance.CheckOutTime.HasValue &&
                     (sa.Attendance.Status == "IN_PROGRESS" || sa.Attendance.Status == "INCOMPLETE")) ||
                    (!sa.Attendance.CheckInTime.HasValue && sa.Attendance.Status == "INCOMPLETE") ||
                    (sa.Attendance.Status == "ABSENT" &&
                     (!sa.Attendance.CheckInTime.HasValue || !sa.Attendance.CheckOutTime.HasValue) &&
                     (sa.Attendance.ActualWorkingMinutes != 0 ||
                      sa.Attendance.TotalLateMinutes != 0 ||
                      sa.Attendance.TotalEarlyLeaveMinutes != 0 ||
                      sa.Attendance.AttendancePenaltyRecords.Any(p => p.Status == "APPLIED")))
                ) &&
                (sa.Shift.ScheduleWeek.PayrollPeriod == null ||
                 !sa.Shift.ScheduleWeek.PayrollPeriod.Timesheets.Any(t =>
                     t.UserId == sa.UserId &&
                     (t.Status == "REVIEWED" || t.Status == "LOCKED"))))
            .OrderBy(sa => sa.Shift.ScheduledEndAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetReviewedOrLockedScheduleWeekIdsForUserAsync(Guid userId, IEnumerable<int> scheduleWeekIds, CancellationToken cancellationToken = default)
    {
        var ids = scheduleWeekIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var lockedWeekIds = await _context.Timesheets
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                t.PayrollPeriod.ScheduleWeekId.HasValue &&
                ids.Contains(t.PayrollPeriod.ScheduleWeekId.Value) &&
                (t.Status == "REVIEWED" || t.Status == "LOCKED"))
            .Select(t => t.PayrollPeriod.ScheduleWeekId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return lockedWeekIds.ToHashSet();
    }

    public async Task RecalculateGeneratedTimesheetsAsync(IEnumerable<(int ScheduleWeekId, Guid UserId)> targetKeys, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var distinctKeys = targetKeys.Distinct().ToList();
        if (distinctKeys.Count == 0)
        {
            return;
        }

        var weekIds = distinctKeys.Select(k => k.ScheduleWeekId).Distinct().ToList();
        var userIds = distinctKeys.Select(k => k.UserId).Distinct().ToList();

        var timesheets = await _context.Timesheets
            .Include(t => t.PayrollPeriod)
            .Where(t =>
                t.Status == "GENERATED" &&
                t.PayrollPeriod.ScheduleWeekId.HasValue &&
                weekIds.Contains(t.PayrollPeriod.ScheduleWeekId.Value) &&
                userIds.Contains(t.UserId))
            .ToListAsync(cancellationToken);

        if (timesheets.Count == 0)
        {
            return;
        }

        var activeWeekIds = timesheets
            .Where(t => t.PayrollPeriod.ScheduleWeekId.HasValue)
            .Select(t => t.PayrollPeriod.ScheduleWeekId!.Value)
            .Distinct()
            .ToList();

        var activeUserIds = timesheets.Select(t => t.UserId).Distinct().ToList();

        var allAssignments = await _context.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.Shift)
            .Include(sa => sa.Attendance)
            .Where(sa =>
                activeWeekIds.Contains(sa.Shift.ScheduleWeekId) &&
                activeUserIds.Contains(sa.UserId) &&
                sa.Status != "CANCELLED" &&
                sa.Shift.Status != "CANCELLED")
            .ToListAsync(cancellationToken);

        bool changed = false;
        foreach (var sheet in timesheets)
        {
            var weekId = sheet.PayrollPeriod.ScheduleWeekId;
            if (!weekId.HasValue || !distinctKeys.Contains((weekId.Value, sheet.UserId)))
            {
                continue;
            }

            var userAssignments = allAssignments
                .Where(sa => sa.Shift.ScheduleWeekId == weekId.Value && sa.UserId == sheet.UserId)
                .ToList();

            int completedCount = 0;
            int absentCount = 0;
            int totalWorkMinutes = 0;
            int totalLateMinutes = 0;
            int excusedLateMinutes = 0;
            int chargeableLateMinutes = 0;
            int totalEarlyLeaveMinutes = 0;
            int excusedEarlyLeaveMinutes = 0;
            int chargeableEarlyLeaveMinutes = 0;

            foreach (var assign in userAssignments)
            {
                var att = assign.Attendance;
                if (att == null || !att.CheckInTime.HasValue || !att.CheckOutTime.HasValue)
                {
                    absentCount++;
                    continue;
                }

                var status = att.Status?.ToUpperInvariant();
                if (status is "COMPLETED" or "PRESENT" or "LATE" or "EARLY_LEAVE" or "LATE_AND_EARLY_LEAVE")
                {
                    completedCount++;
                }
                else
                {
                    absentCount++;
                    continue;
                }

                totalWorkMinutes += att.ActualWorkingMinutes;
                totalLateMinutes += att.TotalLateMinutes;
                excusedLateMinutes += att.ExcusedLateMinutes;
                chargeableLateMinutes += att.ChargeableLateMinutes;
                totalEarlyLeaveMinutes += att.TotalEarlyLeaveMinutes;
                excusedEarlyLeaveMinutes += att.ExcusedEarlyLeaveMinutes;
                chargeableEarlyLeaveMinutes += att.ChargeableEarlyLeaveMinutes;
            }

            sheet.TotalAssignedShifts = userAssignments.Count;
            sheet.CompletedShiftCount = completedCount;
            sheet.AbsentShiftCount = absentCount;
            sheet.IncompleteShiftCount = 0;
            sheet.TotalWorkMinutes = totalWorkMinutes;
            sheet.TotalLateMinutes = totalLateMinutes;
            sheet.ExcusedLateMinutes = excusedLateMinutes;
            sheet.ChargeableLateMinutes = chargeableLateMinutes;
            sheet.TotalEarlyLeaveMinutes = totalEarlyLeaveMinutes;
            sheet.ExcusedEarlyLeaveMinutes = excusedEarlyLeaveMinutes;
            sheet.ChargeableEarlyLeaveMinutes = chargeableEarlyLeaveMinutes;
            sheet.UpdatedAt = nowUtc;
            changed = true;
        }

        if (changed)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public void ClearTrackedChanges()
    {
        _context.ChangeTracker.Clear();
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

    public Task<ShiftAssignment?> GetStaffAssignmentAttendanceDetailAsync(
        int assignmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.ShiftAssignments
            .AsNoTracking()
            .Include(sa => sa.User)
                .ThenInclude(u => u.Role)
            .Include(sa => sa.Shift)
                .ThenInclude(s => s.ScheduleWeek)
                    .ThenInclude(w => w.Branch)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.CheckInFaceSession)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.CheckOutFaceSession)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.AttendancePenaltyRecords)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.LateArrivalRequest)
                    .ThenInclude(r => r!.Request)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.EarlyLeaveRequest)
                    .ThenInclude(r => r!.Request)
            .Include(sa => sa.Attendance)
                .ThenInclude(a => a!.TimesheetAdjustmentRequest)
            .Include(sa => sa.LeaveRequestAssignments)
                .ThenInclude(lra => lra.Request)
            .FirstOrDefaultAsync(sa => sa.AssignmentId == assignmentId && sa.UserId == userId, cancellationToken);
    }

    public Task<Attendance?> GetAttendanceWithDetailsByAssignmentIdAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.Attendances
            .Include(a => a.Assignment)
                .ThenInclude(s => s.Shift)
                    .ThenInclude(w => w.ScheduleWeek)
                        .ThenInclude(b => b.Branch)
            .Include(a => a.Assignment)
                .ThenInclude(s => s.User)
            .Include(a => a.AttendancePenaltyRecords)
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

    public async Task<Dictionary<int, LeaveRequestAssignment>> GetApprovedLeavesForAssignmentsAsync(IEnumerable<int> assignmentIds, CancellationToken cancellationToken = default)
    {
        var ids = assignmentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, LeaveRequestAssignment>();

        var list = await _context.LeaveRequestAssignments
            .AsNoTracking()
            .Include(l => l.Request)
            .Where(l => ids.Contains(l.AssignmentId) &&
                        l.Request.RequestType == "LEAVE" &&
                        l.Request.Status == "APPROVED")
            .ToListAsync(cancellationToken);

        return list
            .GroupBy(l => l.AssignmentId)
            .ToDictionary(g => g.Key, g => g.First());
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

    public async Task<Dictionary<int, TimeAdjustmentRequestDetail>> GetApprovedLateArrivalsForAssignmentsAsync(IEnumerable<int> assignmentIds, CancellationToken cancellationToken = default)
    {
        var ids = assignmentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, TimeAdjustmentRequestDetail>();

        var list = await _context.TimeAdjustmentRequestDetails
            .AsNoTracking()
            .Include(t => t.Request)
            .Where(t => ids.Contains(t.AssignmentId) &&
                        t.Request.RequestType == "LATE_ARRIVAL" &&
                        t.Request.Status == "APPROVED")
            .ToListAsync(cancellationToken);

        return list
            .GroupBy(t => t.AssignmentId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public Task<TimeAdjustmentRequestDetail?> GetApprovedEarlyLeaveForAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        return _context.TimeAdjustmentRequestDetails
            .AsNoTracking()
            .Include(t => t.Request)
            .FirstOrDefaultAsync(t =>
                t.AssignmentId == assignmentId &&
                t.Request.RequestType == "EARLY_LEAVE" &&
                t.Request.Status == "APPROVED",
                cancellationToken);
    }

    public Task<AttendanceRule?> GetActiveAttendanceRuleAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendanceRules
            .AsNoTracking()
            .Where(r => r.Status == "ACTIVE" || r.Status == "PUBLISHED")
            .OrderByDescending(r => r.Status == "ACTIVE")
            .ThenByDescending(r => r.PublishedAt ?? DateTime.MinValue)
            .ThenByDescending(r => r.AttendanceRuleId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AttendancePenaltyRule?> GetActiveAttendancePenaltyRuleWithLevelsAsync(CancellationToken cancellationToken = default)
    {
        return _context.AttendancePenaltyRules
            .AsNoTracking()
            .Include(r => r.AttendancePenaltyLevels)
            .Where(r => r.Status == "ACTIVE" || r.Status == "PUBLISHED")
            .OrderByDescending(r => r.Status == "ACTIVE")
            .ThenByDescending(r => r.PublishedAt ?? DateTime.MinValue)
            .ThenByDescending(r => r.PenaltyRuleId)
            .FirstOrDefaultAsync(cancellationToken);
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

    public void AddAttendance(Attendance attendance)
    {
        _context.Attendances.Add(attendance);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveChangesIgnoreDuplicateAttendanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Npgsql.PostgresException pgEx &&
            pgEx.SqlState == "23505" &&
            string.Equals(pgEx.ConstraintName, "attendances_assignment_id_key", StringComparison.OrdinalIgnoreCase))
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
