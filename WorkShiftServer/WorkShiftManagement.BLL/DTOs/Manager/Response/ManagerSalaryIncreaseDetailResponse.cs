namespace WorkShiftManagement.BLL.DTOs.Manager.Response;
public sealed class ManagerSalaryIncreaseDetailResponse
{
    public int CurrentContractId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
}
