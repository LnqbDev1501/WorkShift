namespace WorkShiftManagement.DAL.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int OrderId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? ReferenceNumber { get; set; }

    public string? Note { get; set; }

    public Guid RecordedBy { get; set; }

    public DateTime RecordedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User RecordedByNavigation { get; set; } = null!;
}
