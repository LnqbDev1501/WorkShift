namespace WorkShiftManagement.BLL.DTOs.Staff.Response;
public sealed class StaffSalaryIncreaseDetailResponse
{
    public int CurrentContractId { get; set; }
    public string? ContractNumber { get; set; }
    public decimal CurrentHourlyRate { get; set; }
    public decimal ProposedHourlyRate { get; set; }
}
