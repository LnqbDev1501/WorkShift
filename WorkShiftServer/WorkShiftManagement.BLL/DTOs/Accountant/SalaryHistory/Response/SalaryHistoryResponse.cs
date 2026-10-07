namespace WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Response;

public sealed class SalaryHistoryResponse
{
    public int SalaryHistoryId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public decimal? PreviousHourlyRate { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int? SalaryIncreaseRequestId { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatedByFullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public SalaryHistoryContractResponse Contract { get; set; } = new();
}

public sealed class SalaryHistoryContractResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
