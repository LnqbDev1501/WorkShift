using System.ComponentModel.DataAnnotations;

namespace UI_Admin.Models.Manager.Contract;

public class ManagerContractChangeRequestListViewModel
{
    public List<ManagerContractChangeRequestListItemViewModel> Items { get; set; } = [];
    public string? StatusFilter { get; set; } = "SUBMITTED";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalItems / (double)PageSize) : 0;
}

public class ManagerContractChangeRequestListItemViewModel
{
    public int ChangeRequestId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffFullName { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string? StaffComment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ManagerContractChangeRequestItemDetailViewModel> Items { get; set; } = [];

    public string FieldsSummary => Items.Count switch
    {
        0 => "No fields",
        1 => Items[0].FieldNameDisplay,
        _ => $"{Items[0].FieldNameDisplay} +{Items.Count - 1} other(s)"
    };
}

public class ManagerContractChangeRequestDetailViewModel
{
    public int ChangeRequestId { get; set; }
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public string StaffFullName { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string? StaffComment { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Current contract info
    public string? ContractStatus { get; set; }
    public DateOnly? CurrentStartDate { get; set; }
    public DateOnly? CurrentEndDate { get; set; }
    public decimal? CurrentHourlyRate { get; set; }
    public string? CurrentEmployeePosition { get; set; }
    public string? CurrentContractTerms { get; set; }
    public string? BranchName { get; set; }
    public string? ContractTemplateVersion { get; set; }

    public List<ManagerContractChangeRequestItemDetailViewModel> Items { get; set; } = [];
}

public class ManagerContractChangeRequestItemDetailViewModel
{
    public int ChangeRequestItemId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string ProposedValue { get; set; } = string.Empty;
    public string? Reason { get; set; }

    /// <summary>
    /// Final value decided by Manager upon approval (defaults to ProposedValue from Staff).
    /// </summary>
    public string FinalValue { get; set; } = string.Empty;

    public string FieldNameDisplay => FieldName.Trim().ToLowerInvariant() switch
    {
        "startdate" => "Contract Start Date",
        "enddate" => "Contract End Date",
        "hourlyrate" => "Hourly Rate (VND/hr)",
        "employeeposition" or "position" => "Position / Title",
        "contractterms" or "terms" => "Contract Terms",
        _ => FieldName
    };
}

public class ApproveContractChangeRequestViewModel
{
    public int ChangeRequestId { get; set; }

    [MaxLength(1000, ErrorMessage = "Feedback note cannot exceed 1000 characters.")]
    public string? ReviewComment { get; set; }

    public List<ApproveContractChangeRequestItemInputViewModel> Items { get; set; } = [];
}

public class ApproveContractChangeRequestItemInputViewModel
{
    public string FieldName { get; set; } = string.Empty;
    public string FinalValue { get; set; } = string.Empty;
}

public class RejectContractChangeRequestViewModel
{
    public int ChangeRequestId { get; set; }

    [Required(ErrorMessage = "Please provide a reason for rejecting the change request.")]
    [MaxLength(1000, ErrorMessage = "Rejection reason cannot exceed 1000 characters.")]
    public string Reason { get; set; } = string.Empty;
}
