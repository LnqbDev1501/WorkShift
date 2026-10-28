using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Repositories.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class HolidayPayRuleRequest
{
    [Required, StringLength(150)] public string RuleName { get; set; } = "";
    [Required, Range(typeof(decimal), "0.01", "999.99")] public decimal? PayMultiplier { get; set; }
    [Required] public DateOnly? EffectiveFrom { get; set; }
}

public sealed record HolidayPayRuleResponse(int HolidayPayRuleId, string RuleName, decimal PayMultiplier,
    DateOnly? EffectiveFrom, DateOnly? EffectiveTo, int? ReplacedRuleId, string Status, bool CanEdit,
    bool CanPublish, bool CanCancel, Guid CreatedBy, DateTime CreatedAt);

public sealed class HolidayPayRuleService(HolidayConfigurationRepo repo)
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
    private static object Snapshot(HolidayPayRule r) => new { r.HolidayPayRuleId, r.RuleName, r.PayMultiplier, r.EffectiveFrom, r.EffectiveTo, r.Status, r.ReplacedRuleId };
    private static HolidayPayRuleResponse Map(HolidayPayRule r) => new(r.HolidayPayRuleId, r.RuleName, r.PayMultiplier,
        r.EffectiveFrom, r.EffectiveTo, r.ReplacedRuleId, r.Status, r.Status == "DRAFT", r.Status == "DRAFT",
        r.Status == "PUBLISH" && r.EffectiveFrom > Today, r.CreatedBy, r.CreatedAt);
    private static void Actor(Guid actor)
    {
        if (actor == Guid.Empty) throw new ArgumentException("Unable to identify the authenticated administrator.");
    }
    private async Task<HolidayPayRule> Find(int id, CancellationToken ct) =>
        await repo.Rules.SingleOrDefaultAsync(r => r.HolidayPayRuleId == id, ct) ?? throw new KeyNotFoundException("Pay rule not found.");
    private async Task Draft(HolidayPayRule rule, CancellationToken ct)
    {
        if (rule.Status != "DRAFT" || await repo.Details.AnyAsync(d => d.HolidayPayRuleId == rule.HolidayPayRuleId, ct)
            || await repo.Rules.AnyAsync(r => r.ReplacedRuleId == rule.HolidayPayRuleId, ct))
            throw HolidayConfigurationRepo.Conflict("Only unused drafts can be edited or deleted.", "RULE_READ_ONLY");
    }

    public async Task<List<HolidayPayRuleResponse>> ListAsync(CancellationToken ct)
    {
        await SynchronizeAsync(ct);
        return (await repo.Rules.AsNoTracking().OrderByDescending(r => r.EffectiveFrom).ThenByDescending(r => r.HolidayPayRuleId).ToListAsync(ct)).Select(Map).ToList();
    }

    public Task<HolidayPayRuleResponse> SaveAsync(int? id, HolidayPayRuleRequest request, Guid actor, CancellationToken ct) =>
        repo.TransactionAsync(async () =>
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
            await repo.SaveAsync(ct);
            repo.Audit(actor, id.HasValue ? "UPDATE_HOLIDAY_PAY_RULE" : "CREATE_HOLIDAY_PAY_RULE", "holiday_pay_rules", rule.HolidayPayRuleId, before, Snapshot(rule));
            await repo.SaveAsync(ct);
            return Map(rule);
        }, ct);

    public Task<bool> DeleteAsync(int id, Guid actor, CancellationToken ct) => repo.TransactionAsync(async () =>
    {
        Actor(actor);
        var rule = await Find(id, ct);
        await Draft(rule, ct);
        repo.Audit(actor, "DELETE_HOLIDAY_PAY_RULE", "holiday_pay_rules", id, Snapshot(rule), null);
        repo.Remove(rule);
        await repo.SaveAsync(ct);
        return true;
    }, ct);

    public async Task<HolidayPayRuleResponse> PublishAsync(int id, Guid actor, CancellationToken ct)
    {
        await SynchronizeAsync(ct);
        return await repo.TransactionAsync(async () =>
        {
            Actor(actor);
            var rule = await Find(id, ct);
            await Draft(rule, ct);
            if (!rule.EffectiveFrom.HasValue || rule.EffectiveFrom == DateOnly.MinValue)
                throw HolidayConfigurationRepo.Conflict("Set an effective date before publishing this draft.", "RULE_DATE_REQUIRED");
            if (await repo.Rules.AnyAsync(r => r.Status == "PUBLISH", ct))
                throw HolidayConfigurationRepo.Conflict("Cancel the pending published rule before publishing another version.", "RULE_ALREADY_SCHEDULED");
            var previous = await repo.Rules.Where(r => r.Status == "ACTIVE" || r.Status == "INACTIVE").OrderByDescending(r => r.EffectiveFrom).FirstOrDefaultAsync(ct);
            HolidayWorkflowPolicy.RequireFuture(rule.EffectiveFrom.Value, Today);
            if (previous != null && rule.EffectiveFrom <= previous.EffectiveFrom)
                throw HolidayConfigurationRepo.Conflict("The effective date must be after today and after the latest published version.", "RULE_DATE_ORDER");
            await repo.InvalidateAsync(await repo.ActiveDatesFromAsync(rule.EffectiveFrom.Value, ct), null, actor, ct);
            var before = Snapshot(rule);
            rule.ReplacedRuleId = previous?.HolidayPayRuleId;
            rule.Status = "PUBLISH";
            repo.Audit(actor, "PUBLISH_HOLIDAY_PAY_RULE", "holiday_pay_rules", id, before, Snapshot(rule));
            await repo.SaveAsync(ct);
            await ActivateDueAsync(ct);
            return Map(rule);
        }, ct);
    }

    public async Task<HolidayPayRuleResponse> CancelAsync(int id, Guid actor, CancellationToken ct)
    {
        await SynchronizeAsync(ct);
        return await repo.TransactionAsync(async () =>
        {
            Actor(actor);
            var rule = await Find(id, ct);
            if (rule.Status != "PUBLISH" || !rule.EffectiveFrom.HasValue || rule.EffectiveFrom <= Today)
                throw HolidayConfigurationRepo.Conflict("Only published rules before their effective date can be cancelled.", "RULE_CANNOT_CANCEL");
            await repo.InvalidateAsync(await repo.ActiveDatesFromAsync(rule.EffectiveFrom.Value, ct), null, actor, ct);
            var before = Snapshot(rule);
            rule.Status = "CANCEL";
            repo.Audit(actor, "CANCEL_HOLIDAY_PAY_RULE", "holiday_pay_rules", id, before, Snapshot(rule));
            await repo.SaveAsync(ct);
            return Map(rule);
        }, ct);
    }

    public Task<bool> SynchronizeAsync(CancellationToken ct) => repo.TransactionAsync(async () =>
    {
        await ActivateDueAsync(ct);
        return true;
    }, ct);

    private async Task ActivateDueAsync(CancellationToken ct)
    {
        var due = await repo.Rules.Where(r => r.Status == "PUBLISH" && r.EffectiveFrom <= Today).OrderBy(r => r.EffectiveFrom).ToListAsync(ct);
        foreach (var rule in due)
        {
            var active = await repo.Rules.Where(r => r.Status == "ACTIVE").ToListAsync(ct);
            if (active.Any(r => r.EffectiveFrom >= rule.EffectiveFrom))
                throw HolidayConfigurationRepo.Conflict("Published rule dates conflict with the active rule. Review the configuration.", "RULE_DATE_ORDER");
            foreach (var old in active)
            {
                var before = Snapshot(old);
                old.EffectiveTo = rule.EffectiveFrom;
                old.Status = "INACTIVE";
                repo.Audit(null, "DEACTIVATE_HOLIDAY_PAY_RULE", "holiday_pay_rules", old.HolidayPayRuleId, before, Snapshot(old));
            }
            // Persist deactivation first to respect a unique ACTIVE partial index.
            await repo.SaveAsync(ct);
            var previousValue = Snapshot(rule);
            rule.Status = "ACTIVE";
            repo.Audit(null, "ACTIVATE_HOLIDAY_PAY_RULE", "holiday_pay_rules", rule.HolidayPayRuleId, previousValue, Snapshot(rule));
            await repo.SaveAsync(ct);
        }
    }
}
