namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public record CreateEmploymentContractResponse(
    int ContractId, string ContractNumber, Guid StaffUserId, string EmployeeFullName,
    int BranchId, string BranchName, int RegionId, string RegionName,
    DateOnly StartDate, DateOnly? EndDate, decimal HourlyRate, string EmployeePosition,
    string? ContractTerms, string Status, Guid CreatedBy, DateTime CreatedAt);
