using System.ComponentModel.DataAnnotations;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Rules;

namespace WorkShiftManagement.BLL.Services.Admin;

public class WorkingTimeRuleService : IWorkingTimeRuleService
{
    private readonly IWorkingTimeRuleRepo _workingTimeRuleRepo;
    public WorkingTimeRuleService(IWorkingTimeRuleRepo workingTimeRuleRepo) => _workingTimeRuleRepo = workingTimeRuleRepo;

    public async Task<List<WorkingTimeRuleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        await ActivateDueAsync(cancellationToken);
        return (await _workingTimeRuleRepo.GetAllAsync(cancellationToken)).Select(Map).ToList();
    }

    public async Task<WorkingTimeRuleResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await ActivateDueAsync(cancellationToken);
        return Map(await _workingTimeRuleRepo.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Working time version was not found."));
    }

    public async Task<WorkingTimeRuleResponse> CreateAsync(WorkingTimeRuleRequest request, Guid createdBy, CancellationToken cancellationToken)
    {
        if (createdBy == Guid.Empty) throw new ArgumentException("An authenticated administrator is required.");
        var rule = CreateValues(request);
        rule.Status = WorkingTimeRuleLifecycle.Draft;
        rule.CreatedBy = createdBy;
        rule.CreatedAt = rule.UpdatedAt = DateTime.UtcNow;
        return Map(await _workingTimeRuleRepo.CreateVersionAsync(rule, cancellationToken));
    }

    public async Task<WorkingTimeRuleResponse> UpdateAsync(int id, WorkingTimeRuleRequest request, CancellationToken cancellationToken) =>
        Map(await _workingTimeRuleRepo.UpdateDraftAsync(id, CreateValues(request), cancellationToken));

    public Task DeleteAsync(int id, CancellationToken cancellationToken) => _workingTimeRuleRepo.DeleteDraftAsync(id, cancellationToken);
    public async Task<WorkingTimeRuleResponse> PublishAsync(int id, CancellationToken cancellationToken) =>
        Map(await _workingTimeRuleRepo.PublishAsync(id, cancellationToken));
    public async Task<WorkingTimeRuleResponse> CancelAsync(int id, CancellationToken cancellationToken) =>
        Map(await _workingTimeRuleRepo.CancelAsync(id, cancellationToken));
    public Task ActivateDueAsync(CancellationToken cancellationToken) => _workingTimeRuleRepo.ActivateDueAsync(cancellationToken);

    private static WorkingTimeRule CreateValues(WorkingTimeRuleRequest request)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        WorkingTimeRuleLifecycle.ValidateEffectiveDate(request.EffectiveFrom, WorkingTimeRuleLifecycle.Today);
        return new WorkingTimeRule
        {
            RuleName = request.RuleName.Trim(),
            MaxDailyMinutes = (int)(request.MaxDailyHours!.Value * 60),
            MaxWeeklyMinutes = (int)(request.MaxWeeklyHours!.Value * 60),
            EffectiveFrom = request.EffectiveFrom
        };
    }

    private static WorkingTimeRuleResponse Map(WorkingTimeRule rule) => new()
    {
        WorkingTimeRuleId = rule.WorkingTimeRuleId,
        RuleName = rule.RuleName,
        MaxDailyHours = rule.MaxDailyMinutes / 60m,
        MaxWeeklyHours = rule.MaxWeeklyMinutes / 60m,
        Status = rule.Status,
        CreatedBy = rule.CreatedBy,
        CreatedAt = rule.CreatedAt,
        EffectiveFrom = rule.EffectiveFrom,
        EffectiveTo = rule.EffectiveTo
    };
}
