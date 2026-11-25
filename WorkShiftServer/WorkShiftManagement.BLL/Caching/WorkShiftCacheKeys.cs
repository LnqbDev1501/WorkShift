using System;
using System.Collections.Generic;

namespace WorkShiftManagement.BLL.Caching;

public static class WorkShiftCacheKeys
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromSeconds(60);

    public static string UserRole(Guid userId) => $"auth:role:{userId:N}";

    public const string ActiveAttendanceRule = "rules:attendance:active";

    public const string ActiveAttendancePenaltyRule = "rules:attendance_penalty:active";
}

public sealed record CachedAttendanceRule(
    int AttendanceRuleId,
    string RuleName,
    int CheckInBeforeMinutes,
    int CheckInGraceMinutes,
    int CheckOutGraceMinutes,
    int CheckOutAfterMinutes,
    int MaxAttendanceDistanceMeters,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status);

public sealed record CachedAttendancePenaltyLevel(
    int PenaltyLevelId,
    string ViolationType,
    int FromMinutes,
    int? ToMinutes,
    decimal PenaltyAmount);

public sealed record CachedAttendancePenaltyRule(
    int PenaltyRuleId,
    string RuleName,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    IReadOnlyList<CachedAttendancePenaltyLevel> Levels);

