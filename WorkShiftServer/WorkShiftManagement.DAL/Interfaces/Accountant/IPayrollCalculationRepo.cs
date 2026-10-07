using Microsoft.EntityFrameworkCore.Storage;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Accountant;

public sealed class PayrollCalculationInputs
{
    public required PayrollPeriod Period { get; init; }
    public List<Guid> EmployeeIds { get; init; } = [];
    public List<Timesheet> Timesheets { get; init; } = [];
    public List<ShiftAssignment> Assignments { get; init; } = [];
    public List<StaffSalaryHistory> Salaries { get; init; } = [];
    public List<EmployeeRewardsPenalty> Adjustments { get; init; } = [];
    public List<PublicHolidayConfig> Holidays { get; init; } = [];
    public List<HolidayPayRule> HolidayRules { get; init; } = [];
    public List<PayrollCarryForwardDeduction> CarryForwards { get; init; } = [];
    public HashSet<Guid> UnsettledPriorEmployees { get; init; } = [];
    public List<Payroll> Payrolls { get; init; } = [];
}

public interface IPayrollCalculationRepo
{
    Task<User?> GetActorAsync(Guid id, CancellationToken ct);
    Task<PayrollPeriod?> GetPeriodAsync(int id, int? branchId, CancellationToken ct);
    Task<PayrollCalculationInputs> GetInputsAsync(PayrollPeriod period, CancellationToken ct);
    Task<IDbContextTransaction> BeginCalculationAsync(CancellationToken ct);
    Task<Payroll?> GetPayrollAsync(int id, int? branchId, CancellationToken ct);
    Task<(List<Payroll> Items, int Count)> GetPayrollsAsync(int periodId, string? keyword, Guid? userId,
        string? status, int page, int pageSize, CancellationToken ct);
    void Add(Payroll payroll);
    void Audit(Guid actor, Payroll payroll);
    Task SaveAsync(CancellationToken ct);
}
