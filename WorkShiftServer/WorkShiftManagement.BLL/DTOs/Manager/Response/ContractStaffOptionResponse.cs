using System;

namespace WorkShiftManagement.BLL.DTOs.Manager.Response;

public record ContractStaffOptionResponse(
    Guid StaffUserId,
    string FullName,
    int? CurrentContractId = null,
    string? CurrentContractNumber = null,
    string? CurrentPosition = null,
    DateOnly? CurrentStartDate = null,
    DateOnly? CurrentEndDate = null,
    decimal? CurrentHourlyRate = null,
    string? CurrentStatus = null,
    int? DaysUntilExpiration = null,
    string? ExpirationCategory = null
);
