namespace UI_Admin.Models.PayrollCalculation;
public sealed class CalculationInputResponse
{
    public int PayrollPeriodId { get; set; }
    public string PeriodStatus { get; set; } = "";
    public string BranchName { get; set; } = "";
    public EmployeeReadiness Readiness { get; set; } = new();
    public List<CalculationSourceRow> Sources { get; set; } = [];
}
public sealed class CalculationSourceRow
{
    public string Type { get; set; } = "";
    public int SourceId { get; set; }
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly? WorkDate { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int? Minutes { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Multiplier { get; set; }
    public decimal? Amount { get; set; }
    public decimal? RemainingAmount { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}
