namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public record ContractStaffOptionResponse(
    Guid StaffUserId,
    string FullName,
    int? CurrentContractId = null,
    string? CurrentContractNumber = null,
    string? CurrentPosition = null
);
