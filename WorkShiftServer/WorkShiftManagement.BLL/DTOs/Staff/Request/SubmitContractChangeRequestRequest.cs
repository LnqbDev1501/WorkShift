using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class SubmitContractChangeRequestRequest
{
    public string? RequestType { get; set; } = "CHANGE_REQUEST";

    public string? StaffComment { get; set; }

    [Required(ErrorMessage = "The list of requested changes is required.")]
    [MinLength(1, ErrorMessage = "At least one field change is required.")]
    public List<ContractChangeRequestItemInputDto> Items { get; set; } = new();
}

public class ContractChangeRequestItemInputDto
{
    [Required(ErrorMessage = "The field name is required.")]
    [MaxLength(100, ErrorMessage = "The field name cannot exceed 100 characters.")]
    public string FieldName { get; set; } = null!;

    [Required(ErrorMessage = "The proposed value is required.")]
    public string ProposedValue { get; set; } = null!;

    public string? Reason { get; set; }
}
