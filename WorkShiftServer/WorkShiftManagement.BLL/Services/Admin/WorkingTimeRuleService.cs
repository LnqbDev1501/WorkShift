using System.ComponentModel.DataAnnotations;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class WorkingTimeRuleService : IWorkingTimeRuleService
{
    private readonly IWorkingTimeRuleRepo _workingTimeRuleRepo;

    public WorkingTimeRuleService(IWorkingTimeRuleRepo workingTimeRuleRepo)
        => _workingTimeRuleRepo = workingTimeRuleRepo;

    public async Task<List<WorkingTimeRuleResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await _workingTimeRuleRepo.GetAllAsync(cancellationToken)).Select(Map).ToList();

    public async Task<WorkingTimeRuleResponse> CreateAsync(CreateWorkingTimeRuleRequest request, Guid createdBy, CancellationToken cancellationToken)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (createdBy == Guid.Empty) throw new ArgumentException("An authenticated administrator is required.");
        var now = DateTime.UtcNow;
        return Map(await _workingTimeRuleRepo.CreateVersionAsync(new WorkingTimeRule
        {
            RuleName = request.RuleName.Trim(),
            MaxDailyMinutes = (int)(request.MaxDailyHours!.Value * 60),
            MaxWeeklyMinutes = (int)(request.MaxWeeklyHours!.Value * 60),
            Status = "ACTIVE",
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken));
    }

    private static WorkingTimeRuleResponse Map(WorkingTimeRule rule) => new()
    {
        WorkingTimeRuleId = rule.WorkingTimeRuleId,
        RuleName = rule.RuleName,
        MaxDailyHours = rule.MaxDailyMinutes / 60m,
        MaxWeeklyHours = rule.MaxWeeklyMinutes / 60m,
        Status = rule.Status,
        CreatedBy = rule.CreatedBy,
        CreatedAt = rule.CreatedAt
    };
}
