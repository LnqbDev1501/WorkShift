namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public class StaffContractHistoryItemResponse
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }
    public string EmployeePosition { get; set; } = null!;
    public int? ReplacesContractId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentToStaffAt { get; set; }
    public DateTime? StaffAcceptedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? TerminationEffectiveAt { get; set; }
}
