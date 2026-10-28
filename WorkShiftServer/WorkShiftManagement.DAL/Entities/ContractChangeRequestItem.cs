namespace WorkShiftManagement.DAL.Entities;

public partial class ContractChangeRequestItem
{
    public int ChangeRequestItemId { get; set; }

    public int ChangeRequestId { get; set; }

    public string FieldName { get; set; } = null!;

    public string? OriginalValue { get; set; }

    public string ProposedValue { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ContractChangeRequest ChangeRequest { get; set; } = null!;
}
