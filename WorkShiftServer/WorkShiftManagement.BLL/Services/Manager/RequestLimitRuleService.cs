using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.DTOs.Manager.Request;
using WorkShiftManagement.BLL.DTOs.Manager.Response;
using WorkShiftManagement.BLL.Interfaces.Manager;
using WorkShiftManagement.BLL.Interfaces.Realtime;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Manager;

namespace WorkShiftManagement.BLL.Services.Manager;

public class RequestLimitRuleService(
    IRequestLimitRuleRepo repo,
    IAdminRealtimePublisher? adminRealtimePublisher = null) : IRequestLimitRuleService
{
    private static readonly string[] Types = ["LEAVE", "LATE_ARRIVAL", "EARLY_LEAVE", "SALARY_INCREASE"];
    private static readonly string[] Statuses = ["DRAFT", "SCHEDULED", "ACTIVE", "INACTIVE"];
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

    public async Task<PagedResponse<RequestLimitRuleResponse>> GetPagedAsync(Guid managerId, RequestLimitRuleListQuery query, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        if (query.PageNumber < 1 || query.PageSize is < 1 or > 100 || (long)(query.PageNumber - 1) * query.PageSize > int.MaxValue)
            throw new ArgumentException("Invalid pagination. PageNumber must be positive and PageSize between 1 and 100.");
        var type = Normalize(query.RequestType, Types);
        var status = Normalize(query.Status, Statuses);
        var (items, total) = await repo.GetPagedAsync(branch, type, status, query.PageNumber, query.PageSize, ct);
        return new() { Items = items.Select(Map).ToList(), TotalItems = total, PageNumber = query.PageNumber, PageSize = query.PageSize };
    }

    public async Task<RequestLimitRuleResponse> GetAsync(Guid managerId, int id, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        ValidateId(id);
        return Map(await repo.GetAsync(branch, id, ct) ?? throw new KeyNotFoundException("Request limit rule not found in your branch."));
    }

    public async Task<RequestLimitRuleResponse> CreateAsync(Guid managerId, CreateRequestLimitRuleRequest request, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        Validate(request);
        var res = Map(await repo.MutateAsync(branch, _ => NewRule(branch, managerId, request), ct));
        if (adminRealtimePublisher != null)
        {
            try
            {
                await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new
                {
                    resource = "RequestLimitRules",
                    action = "CREATED",
                    ruleId = res.RequestLimitRuleId,
                    branchId = branch
                });
            }
            catch { }
        }
        return res;
    }

    public async Task<RequestLimitRuleResponse> UpdateAsync(Guid managerId, int id, UpdateRequestLimitRuleRequest request, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        Validate(request);
        var res = Map(await repo.MutateAsync(branch, rules =>
        {
            var rule = Find(rules, id);
            if (rule.Status != "DRAFT") throw new InvalidOperationException("Only DRAFT rules can be updated.");
            if (rule.SupersedesRuleId.HasValue)
            {
                var previous = Find(rules, rule.SupersedesRuleId.Value);
                if (request.RequestType != previous.RequestType || request.EffectiveFrom <= previous.EffectiveFrom)
                    throw new ArgumentException("A replacement must keep the request type and start after the previous version.");
            }
            Apply(rule, request);
            return rule;
        }, ct));
        if (adminRealtimePublisher != null)
        {
            try
            {
                await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new
                {
                    resource = "RequestLimitRules",
                    action = "UPDATED",
                    ruleId = id,
                    branchId = branch
                });
            }
            catch { }
        }
        return res;
    }

    public async Task<RequestLimitRuleResponse> ReplaceAsync(Guid managerId, int id, ReplaceRequestLimitRuleRequest request, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        Validate(request);
        var res = Map(await repo.MutateAsync(branch, rules =>
        {
            var previous = Find(rules, id);
            if (previous.Status != "ACTIVE") throw new InvalidOperationException("Only ACTIVE rules can be replaced.");
            if (request.RequestType != previous.RequestType || request.EffectiveFrom <= previous.EffectiveFrom)
                throw new ArgumentException("A replacement must keep the request type and start after the previous version.");
            if (rules.Any(x => x.SupersedesRuleId == id && x.Status is "DRAFT" or "SCHEDULED"))
                throw new InvalidOperationException("This rule already has a pending replacement.");
            var rule = NewRule(branch, managerId, request);
            rule.SupersedesRuleId = id;
            return rule;
        }, ct));
        if (adminRealtimePublisher != null)
        {
            try
            {
                await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new
                {
                    resource = "RequestLimitRules",
                    action = "REPLACED",
                    ruleId = res.RequestLimitRuleId,
                    branchId = branch
                });
            }
            catch { }
        }
        return res;
    }

    public async Task<RequestLimitRuleResponse> PublishAsync(Guid managerId, int id, CancellationToken ct)
    {
        var branch = await BranchAsync(managerId, ct);
        var res = Map(await repo.MutateAsync(branch, rules =>
        {
            var rule = Find(rules, id);
            if (rule.Status != "DRAFT") throw new InvalidOperationException("Only DRAFT rules can be published.");
            if (rule.SubmissionDeadlineDayOfWeek.HasValue && rule.RequestType != "SALARY_INCREASE")
                throw new InvalidOperationException("The staff request module does not yet support SubmissionDeadlineDayOfWeek. Clear this field before publishing.");
            if (rule.RequestType == "SALARY_INCREASE" && rule.MinimumAdvanceDays != 0)
                throw new InvalidOperationException("Salary increase requests do not select an effective date. Set MinimumAdvanceDays to 0 before publishing.");
            if (rule.EffectiveFrom < Today) throw new ArgumentException("EffectiveFrom cannot be in the past. Update the draft before publishing.");
            if (rules.Any(x => x.RequestType == rule.RequestType && x.Status == "SCHEDULED"))
                throw new InvalidOperationException("A rule for this request type is already scheduled.");
            var active = rules.SingleOrDefault(x => x.RequestType == rule.RequestType && x.Status == "ACTIVE");
            if (rule.SupersedesRuleId != active?.RequestLimitRuleId)
                throw new InvalidOperationException("Use Replace to create a successor to the current ACTIVE rule.");
            if (active != null && rule.EffectiveFrom <= active.EffectiveFrom)
                throw new ArgumentException("EffectiveFrom must be after the current ACTIVE version.");
            var now = DateTime.UtcNow;
            rule.Status = rule.EffectiveFrom == Today ? "ACTIVE" : "SCHEDULED";
            rule.PublishedBy = managerId;
            rule.PublishedAt = now;
            rule.UpdatedAt = now;
            if (active != null && rule.Status == "ACTIVE")
            {
                active.Status = "INACTIVE";
                active.EffectiveTo = rule.EffectiveFrom.AddDays(-1);
                active.UpdatedAt = now;
            }
            return rule;
        }, ct));
        if (adminRealtimePublisher != null)
        {
            try
            {
                await adminRealtimePublisher.PublishDataChangedAsync(AdminRealtimeEvents.DataChanged, new
                {
                    resource = "RequestLimitRules",
                    action = "PUBLISHED",
                    ruleId = id,
                    branchId = branch
                });
            }
            catch { }
        }
        return res;
    }

    private async Task<int> BranchAsync(Guid managerId, CancellationToken ct)
    {
        var manager = await repo.GetManagerAsync(managerId, ct);
        if (manager == null || manager.Status != "ACTIVE" || manager.Role?.RoleName != "MANAGER" || manager.Role.Status != "ACTIVE" ||
            !manager.BranchId.HasValue || manager.Branch?.Status != "ACTIVE")
            throw new UnauthorizedAccessException("An active Manager account assigned to an active branch is required.");
        return manager.BranchId.Value;
    }

    private static string? Normalize(string? value, string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        return allowed.Contains(normalized) ? normalized : throw new ArgumentException($"Allowed values: {string.Join(", ", allowed)}.");
    }

    private static void Validate(CreateRequestLimitRuleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.RequestType = Normalize(request.RequestType, Types) ?? throw new ArgumentException("RequestType is required.");
        if (!request.MaxRequestsPerMonth.HasValue || request.MaxRequestsPerMonth <= 0) throw new ArgumentException("MaxRequestsPerMonth must be greater than zero.");
        if (!request.MinimumAdvanceDays.HasValue || request.MinimumAdvanceDays < 0) throw new ArgumentException("MinimumAdvanceDays must be nonnegative.");
        if (request.SubmissionDeadlineDayOfWeek is < 1 or > 7) throw new ArgumentException("SubmissionDeadlineDayOfWeek must be between 1 (Monday) and 7 (Sunday).");
        if (!request.EffectiveFrom.HasValue || request.EffectiveFrom < Today) throw new ArgumentException("EffectiveFrom is required and cannot be in the past.");
    }

    private static void ValidateId(int id)
    {
        if (id <= 0) throw new ArgumentException("Rule ID must be positive.");
    }

    private static RequestLimitRule Find(List<RequestLimitRule> rules, int id)
    {
        ValidateId(id);
        return rules.SingleOrDefault(x => x.RequestLimitRuleId == id) ?? throw new KeyNotFoundException("Request limit rule not found in your branch.");
    }

    private static RequestLimitRule NewRule(int branch, Guid manager, CreateRequestLimitRuleRequest request)
    {
        var rule = new RequestLimitRule { BranchId = branch, CreatedBy = manager, CreatedAt = DateTime.UtcNow, Status = "DRAFT" };
        Apply(rule, request);
        return rule;
    }

    private static void Apply(RequestLimitRule rule, CreateRequestLimitRuleRequest request)
    {
        rule.RequestType = request.RequestType;
        rule.MaxRequestsPerMonth = request.MaxRequestsPerMonth!.Value;
        rule.MinimumAdvanceDays = request.MinimumAdvanceDays!.Value;
        rule.SubmissionDeadlineDayOfWeek = request.SubmissionDeadlineDayOfWeek;
        rule.EffectiveFrom = request.EffectiveFrom!.Value;
        rule.UpdatedAt = DateTime.UtcNow;
    }

    private static RequestLimitRuleResponse Map(RequestLimitRule rule) => new()
    {
        RequestLimitRuleId = rule.RequestLimitRuleId, BranchId = rule.BranchId, RequestType = rule.RequestType,
        MaxRequestsPerMonth = rule.MaxRequestsPerMonth, MinimumAdvanceDays = rule.MinimumAdvanceDays,
        SubmissionDeadlineDayOfWeek = rule.SubmissionDeadlineDayOfWeek, EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo, Status = rule.Status, SupersedesRuleId = rule.SupersedesRuleId,
        CreatedBy = rule.CreatedBy, CreatedAt = rule.CreatedAt, UpdatedAt = rule.UpdatedAt,
        PublishedBy = rule.PublishedBy, PublishedAt = rule.PublishedAt
    };
}
