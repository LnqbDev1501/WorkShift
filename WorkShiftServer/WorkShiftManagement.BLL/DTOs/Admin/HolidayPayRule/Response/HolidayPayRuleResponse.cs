using System;

namespace WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Response;

public sealed record HolidayPayRuleResponse(
    int HolidayPayRuleId,
    string RuleName,
    decimal PayMultiplier,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    int? ReplacedRuleId,
    string Status,
    bool CanEdit,
    bool CanPublish,
    bool CanCancel,
    Guid CreatedBy,
    DateTime CreatedAt
);

