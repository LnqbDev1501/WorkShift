namespace WorkShiftManagement.BLL.DTOs.Admin.Response;

public sealed class AdminSalaryIncreaseDetailResponse
{
    public int CurrentContractId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
}
