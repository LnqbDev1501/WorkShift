namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public class AdminEmploymentContractDetailResponse
{
    // Thông tin hợp đồng
    public int ContractId { get; set; }

    public string ContractNumber { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal HourlyRate { get; set; }

    public string? ContractTerms { get; set; }

    public int? ReplacesContractId { get; set; }

    // Thông tin Staff (Snapshot ưu tiên, Fallback dữ liệu hiện tại)
    public Guid StaffId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string IdentityNo { get; set; } = null!;

    public string EmployeePosition { get; set; } = null!;

    // Thông tin tổ chức tại thời điểm lập hợp đồng
    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public string BranchAddress { get; set; } = null!;

    public int RegionId { get; set; }

    public string RegionName { get; set; } = null!;

    // Thông tin vòng đời
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SentToStaffAt { get; set; }

    public DateTime? StaffRespondedAt { get; set; }

    public DateTime? StaffAcceptedAt { get; set; }

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? ManagerRejectionComment { get; set; }

    public Guid? TerminatedBy { get; set; }

    public DateTime? TerminatedAt { get; set; }

    public DateTime? TerminationEffectiveAt { get; set; }

    public string? TerminationReason { get; set; }

    // Thông tin PDF
    public bool HasPdf { get; set; }

    public string? ContractTemplateVersion { get; set; }

    public DateTime? PdfGeneratedAt { get; set; }
}
