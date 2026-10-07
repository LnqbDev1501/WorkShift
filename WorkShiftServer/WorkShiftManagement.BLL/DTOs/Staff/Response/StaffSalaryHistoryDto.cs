using System;

namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

/// <summary>
/// Data transfer object representing a staff salary history record.
/// </summary>
public class StaffSalaryHistoryDto
{
    public int SalaryHistoryId { get; set; }
    public Guid UserId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public decimal? PreviousHourlyRate { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
