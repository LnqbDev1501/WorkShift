using System;
using System.Collections.Generic;

namespace WorkShiftManagement.DAL.Entities;

public partial class EmploymentContract
{
    public int ContractId { get; set; }

    public Guid UserId { get; set; }

    public int? ReplacesContractId { get; set; }

    public string ContractNumber { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal HourlyRate { get; set; }

    public string EmployeePositionSnapshot { get; set; } = null!;

    public int BranchId { get; set; }

    public int RegionId { get; set; }

    public string EmployeeFullNameSnapshot { get; set; } = null!;

    public string EmployeePhoneSnapshot { get; set; } = null!;

    public string EmployeeAddressSnapshot { get; set; } = null!;

    public string EmployeeIdentityNoSnapshot { get; set; } = null!;

    public string BranchNameSnapshot { get; set; } = null!;

    public string BranchAddressSnapshot { get; set; } = null!;

    public string RegionNameSnapshot { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid CreatedBy { get; set; }

    public DateTime? SentToStaffAt { get; set; }

    public DateTime? StaffRespondedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? ManagerRejectionComment { get; set; }

    public Guid? TerminatedBy { get; set; }

    public DateTime? TerminatedAt { get; set; }

    public string? TerminationReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? StaffAcceptedAt { get; set; }

    public string? ContractTerms { get; set; }

    public string? ContractFilePath { get; set; }

    public string? ContractTemplateVersion { get; set; }

    public DateTime? PdfGeneratedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ContractChangeRequest? ContractChangeRequest { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<EmploymentContract> InverseReplacesContract { get; set; } = new List<EmploymentContract>();

    public virtual Region Region { get; set; } = null!;

    public virtual User? RejectedByNavigation { get; set; }

    public virtual EmploymentContract? ReplacesContract { get; set; }

    public virtual ICollection<SalaryIncreaseRequestDetail> SalaryIncreaseRequestDetails { get; set; } = new List<SalaryIncreaseRequestDetail>();

    public virtual ICollection<StaffSalaryHistory> StaffSalaryHistories { get; set; } = new List<StaffSalaryHistory>();

    public virtual User? TerminatedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;
}
