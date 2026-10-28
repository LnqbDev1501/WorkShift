namespace WorkShiftManagement.DAL.Entities;

/// <summary>
/// Manager de xuat TANG luong -&gt; Admin. Hien khong giam luong. Hop dong/don gia lay tu History nguon khong ghi de don gia.
/// </summary>
public partial class SalaryChangeRequest
{
    public int SalaryChangeRequestId { get; set; }

    public Guid UserId { get; set; }

    public int BranchId { get; set; }

    public int? EmployeeRequestId { get; set; }

    public int? PreviousRequestId { get; set; }

    /// <summary>
    /// Nguon don gia cu va contract. Backend kiem tra dung Staff, don gia de nghi lon hon nguon, va nguon con hop le luc duyet.
    /// </summary>
    public int CurrentSalaryHistoryId { get; set; }

    public decimal ProposedHourlyRate { get; set; }

    public DateOnly ProposedEffectiveFrom { get; set; }

    /// <summary>
    /// Ngay thuc te: chua den ngay de xuat thi giu ngay; da den/qua thi Admin chon thu Hai tu tuan sau va ghi ly do. Khong hoi to.
    /// </summary>
    public DateOnly? ApprovedEffectiveFrom { get; set; }

    public string? EffectiveDateChangeReason { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public Guid RequestedBy { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewNote { get; set; }

    public Guid? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    /// <summary>
    /// Tao la gui ngay PENDING, khong Draft; thoi diem nay thay requested_at.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual User? CancelledByNavigation { get; set; }

    public virtual StaffSalaryHistory CurrentSalaryHistory { get; set; } = null!;

    public virtual EmployeeRequest? EmployeeRequest { get; set; }

    public virtual ICollection<SalaryChangeRequest> InversePreviousRequest { get; set; } = new List<SalaryChangeRequest>();

    public virtual SalaryChangeRequest? PreviousRequest { get; set; }

    public virtual User RequestedByNavigation { get; set; } = null!;

    public virtual User? ReviewedByNavigation { get; set; }

    public virtual StaffSalaryHistory? StaffSalaryHistory { get; set; }

    public virtual User User { get; set; } = null!;
}
