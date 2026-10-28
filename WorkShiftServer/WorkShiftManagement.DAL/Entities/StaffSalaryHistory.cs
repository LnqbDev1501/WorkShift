namespace WorkShiftManagement.DAL.Entities;

public partial class StaffSalaryHistory
{
    public int SalaryHistoryId { get; set; }

    public Guid UserId { get; set; }

    public int ContractId { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal? PreviousHourlyRate { get; set; }

    public string ChangeType { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string Status { get; set; } = null!;

    public string? Reason { get; set; }

    public int? SalaryIncreaseRequestId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// UNIQUE: mot proposal toi da mot history. Ban ghi moi dung lien ket nay; salary_increase_request_id cu de NULL, giu cho legacy.
    /// </summary>
    public int? SalaryChangeRequestId { get; set; }

    public virtual EmploymentContract Contract { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();

    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    public virtual SalaryChangeRequest? SalaryChangeRequest { get; set; }

    public virtual ICollection<SalaryChangeRequest> SalaryChangeRequests { get; set; } = new List<SalaryChangeRequest>();

    public virtual SalaryIncreaseRequestDetail? SalaryIncreaseRequest { get; set; }

    public virtual User User { get; set; } = null!;
}
