namespace WorkShiftManagement.BLL.DTOs.Staff.Response;

public class StaffContractChangeRequestResponse
{
    public int ChangeRequestId { get; set; }
    public int ContractId { get; set; }
    public string RequestType { get; set; } = null!;
    public string? StaffComment { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ContractChangeRequestItemDetailDto> Items { get; set; } = new();
}

public class ContractChangeRequestItemDetailDto
{
    public int ChangeRequestItemId { get; set; }
    public string FieldName { get; set; } = null!;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = null!;
    public string? Reason { get; set; }
}
