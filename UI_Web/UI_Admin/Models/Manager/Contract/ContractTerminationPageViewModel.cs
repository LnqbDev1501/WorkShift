using System;
using System.Collections.Generic;

namespace UI_Admin.Models.Manager.Contract;

public class ContractTerminationPageViewModel
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeePosition { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal HourlyRate { get; set; }

    public int WarningCount { get; set; }
    public int NextWarningNo => Math.Min(3, WarningCount + 1);
    public bool IsStaffCurrentlyInShift { get; set; }
    public string? StaffInShiftMessage { get; set; }
    public bool CanTerminate => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) && WarningCount >= 3 && !IsStaffCurrentlyInShift;

    public List<ContractTerminationWarningViewModel> Warnings { get; set; } = [];

    // Fields for sending warning
    public string WarningTitle { get; set; } = string.Empty;
    public string WarningContent { get; set; } = string.Empty;

    // Fields for terminating
    public DateOnly? TerminationEffectiveDate { get; set; }
    public string? TerminationReason { get; set; }
}

