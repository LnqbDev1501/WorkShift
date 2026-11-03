using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Request;
using WorkShiftManagement.BLL.DTOs.Admin.HolidayPayRule.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class HolidayPayRuleService(IHolidayPayRuleRepo repo, IAdminRealtimePublisher? realtimePublisher = null) : IHolidayPayRuleService
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
    DateOnly IHolidayPayRuleService.Today => Today;

    private static object Snapshot(HolidayPayRule r) => new { r.HolidayPayRuleId, r.RuleName, r.PayMultiplier, r.EffectiveFrom, r.EffectiveTo, r.Status, r.ReplacedRuleId };

    private static HolidayPayRuleResponse Map(HolidayPayRule r) => new(
        r.HolidayPayRuleId,
        r.RuleName,
        r.PayMultiplier,
        r.EffectiveFrom,
        r.EffectiveTo,
        r.ReplacedRuleId,
        r.Status,
        r.Status == "DRAFT",
        r.Status == "DRAFT",
        r.Status == "PUBLISH" && r.EffectiveFrom > Today,
        r.CreatedBy,
        r.CreatedAt);

    private static void Actor(Guid actor)
    {
        if (actor == Guid.Empty) throw new ArgumentException("Unable to identify the authenticated administrator.");
    }

    public static InvalidOperationException Conflict(string message, string code)
    {
        var error = new InvalidOperationException(message);
        error.Data["code"] = code;
        return error;
    }

    private async Task<HolidayPayRule> Find(int id, CancellationToken ct) =>
        await repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Pay rule not found.");

    private async Task Draft(HolidayPayRule rule, CancellationToken ct)
    {
        if (rule.Status != "DRAFT"
            || await repo.IsReferencedInPayrollAsync(rule.HolidayPayRuleId, ct)
            || await repo.IsReferencedAsReplacedRuleAsync(rule.HolidayPayRuleId, ct))
            throw Conflict("Only unused drafts can be edited or deleted.", "RULE_READ_ONLY");
    }

    public async Task<List<HolidayPayRuleResponse>> ListAsync(CancellationToken ct = default)
    {
        await SynchronizeAsync(ct);
        var rules = await repo.GetAllOrderedAsync(ct);
        return rules.Select(Map).ToList();
    }

    public async Task<HolidayPayRuleResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var rule = await Find(id, ct);
        return Map(rule);
    }

    public Task<HolidayPayRuleResponse> CreateAsync(HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default) =>
        SaveInternalAsync(null, request, actor, ct);

    public Task<HolidayPayRuleResponse> UpdateAsync(int id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default) =>
        SaveInternalAsync(id, request, actor, ct);

    public Task<HolidayPayRuleResponse> SaveAsync(int? id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct = default) =>
        SaveInternalAsync(id, request, actor, ct);

    private Task<HolidayPayRuleResponse> SaveInternalAsync(int? id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct) =>
        repo.ExecuteTransactionAsync(async () =>
        {
            Actor(actor);
            if (string.IsNullOrWhiteSpace(request.RuleName) || request.RuleName.Trim().Length > 150
                || request.EffectiveFrom == null || request.EffectiveFrom == DateOnly.MinValue
                || request.PayMultiplier is null or <= 0 or > 999.99m
                || decimal.Round(request.PayMultiplier.Value, 2) != request.PayMultiplier.Value)
                throw new ArgumentException("Enter a valid rule name, effective date and multiplier with up to two decimal places.");
            HolidayWorkflowPolicy.RequireFuture(request.EffectiveFrom.Value, Today);
            var rule = id.HasValue ? await Find(id.Value, ct) : new HolidayPayRule { CreatedBy = actor, CreatedAt = DateTime.UtcNow, Status = "DRAFT" };
            if (id.HasValue) await Draft(rule, ct);
            var before = id.HasValue ? Snapshot(rule) : null;
            rule.RuleName = request.RuleName.Trim();
            rule.PayMultiplier = request.PayMultiplier.Value;
            rule.EffectiveFrom = request.EffectiveFrom.Value;
            if (!id.HasValue) repo.Add(rule);
            await repo.SaveChangesAsync(ct);
            repo.WriteAudit(actor, id.HasValue ? "UPDATE_HOLIDAY_PAY_RULE" : "CREATE_HOLIDAY_PAY_RULE", rule.HolidayPayRuleId, before, Snapshot(rule));
            await repo.SaveChangesAsync(ct);
            return Map(rule);
        }, ct);

    public Task<bool> DeleteAsync(int id, Guid actor, CancellationToken ct = default) => repo.ExecuteTransactionAsync(async () =>
    {
        Actor(actor);
        var rule = await Find(id, ct);
        await Draft(rule, ct);
        repo.WriteAudit(actor, "DELETE_HOLIDAY_PAY_RULE", id, Snapshot(rule), null);
        repo.Remove(rule);
        await repo.SaveChangesAsync(ct);
        return true;
    }, ct);

    public async Task<HolidayPayRuleResponse> PublishAsync(int id, Guid actor, CancellationToken ct = default)
    {
        await SynchronizeAsync(ct);
        return await repo.ExecuteTransactionAsync(async () =>
        {
            Actor(actor);
            var rule = await Find(id, ct);
            await Draft(rule, ct);
            if (!rule.EffectiveFrom.HasValue || rule.EffectiveFrom == DateOnly.MinValue)
                throw Conflict("Set an effective date before publishing this draft.", "RULE_DATE_REQUIRED");
            if (await repo.HasScheduledPublishRuleAsync(ct))
                throw Conflict("Cancel the pending published rule before publishing another version.", "RULE_ALREADY_SCHEDULED");
            var previous = await repo.GetLatestActiveOrInactiveRuleAsync(ct);
            HolidayWorkflowPolicy.RequireFuture(rule.EffectiveFrom.Value, Today);
            if (previous != null && rule.EffectiveFrom <= previous.EffectiveFrom)
                throw Conflict("The effective date must be after today and after the latest published version.", "RULE_DATE_ORDER");
            var before = Snapshot(rule);
            rule.ReplacedRuleId = previous?.HolidayPayRuleId;
            rule.Status = "PUBLISH";
            repo.WriteAudit(actor, "PUBLISH_HOLIDAY_PAY_RULE", id, before, Snapshot(rule));
            await repo.SaveChangesAsync(ct);
            await ActivateDueAsync(ct);
            return Map(rule);
        }, ct);
    }

    public async Task<HolidayPayRuleResponse> CancelAsync(int id, Guid actor, CancellationToken ct = default)
    {
        await SynchronizeAsync(ct);
        return await repo.ExecuteTransactionAsync(async () =>
        {
            Actor(actor);
            var rule = await Find(id, ct);
            if (rule.Status != "PUBLISH" || !rule.EffectiveFrom.HasValue || rule.EffectiveFrom <= Today)
                throw Conflict("Only published rules before their effective date can be cancelled.", "RULE_CANNOT_CANCEL");
            var before = Snapshot(rule);
            rule.Status = "CANCEL";
            repo.WriteAudit(actor, "CANCEL_HOLIDAY_PAY_RULE", id, before, Snapshot(rule));
            await repo.SaveChangesAsync(ct);
            return Map(rule);
        }, ct);
    }

    public async Task<bool> SynchronizeAsync(CancellationToken ct = default)
    {
        var activated = await repo.ExecuteTransactionAsync(() => ActivateDueAsync(ct), ct);
        if (activated > 0 && realtimePublisher != null)
            await realtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged,
                new { resource = "HolidayPayRules", action = "ACTIVATED" });
        return true;
    }

    private async Task<int> ActivateDueAsync(CancellationToken ct)
    {
        var due = await repo.GetDuePublishedRulesAsync(Today, ct);
        foreach (var rule in due)
        {
            var active = await repo.GetActiveRulesAsync(ct);
            if (active.Any(r => r.EffectiveFrom >= rule.EffectiveFrom))
                throw Conflict("Published rule dates conflict with the active rule. Review the configuration.", "RULE_DATE_ORDER");
            foreach (var old in active)
            {
                var before = Snapshot(old);
                old.EffectiveTo = rule.EffectiveFrom;
                old.Status = "INACTIVE";
                repo.WriteAudit(null, "DEACTIVATE_HOLIDAY_PAY_RULE", old.HolidayPayRuleId, before, Snapshot(old));
            }
            // Persist deactivation first to respect a unique ACTIVE partial index.
            await repo.SaveChangesAsync(ct);
            var previousValue = Snapshot(rule);
            rule.Status = "ACTIVE";
            repo.WriteAudit(null, "ACTIVATE_HOLIDAY_PAY_RULE", rule.HolidayPayRuleId, previousValue, Snapshot(rule));
            await repo.SaveChangesAsync(ct);
        }
        return due.Count;
    }
}
