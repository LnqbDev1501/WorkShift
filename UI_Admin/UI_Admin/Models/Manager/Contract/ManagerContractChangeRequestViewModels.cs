using System;
using System.Collections.Generic;
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
        0 => "Không có trường",
        1 => Items[0].FieldNameDisplay,
        _ => $"{Items[0].FieldNameDisplay} +{Items.Count - 1} mục khác"
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

    // Thông tin hợp đồng hiện tại
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
    /// Giá trị cuối cùng Manager duyệt áp dụng (mặc định lấy ProposedValue của Staff để Manager có thể sửa).
    /// </summary>
    public string FinalValue { get; set; } = string.Empty;

    public string FieldNameDisplay => FieldName.Trim().ToLowerInvariant() switch
    {
        "startdate" => "Ngày bắt đầu hợp đồng",
        "enddate" => "Ngày kết thúc hợp đồng",
        "hourlyrate" => "Mức lương theo giờ (VNĐ/giờ)",
        "employeeposition" or "position" => "Chức danh / Vị trí làm việc",
        "contractterms" or "terms" => "Điều khoản hợp đồng",
        _ => FieldName
    };
}

public class ApproveContractChangeRequestViewModel
{
    public int ChangeRequestId { get; set; }

    [MaxLength(1000, ErrorMessage = "Ghi chú phản hồi tối đa 1000 ký tự.")]
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

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối yêu cầu thay đổi.")]
    [MaxLength(1000, ErrorMessage = "Lý do từ chối tối đa 1000 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
