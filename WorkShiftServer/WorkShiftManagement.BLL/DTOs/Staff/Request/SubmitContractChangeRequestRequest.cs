using System.ComponentModel.DataAnnotations;

namespace WorkShiftManagement.BLL.DTOs.Staff.Request;

public class SubmitContractChangeRequestRequest
{
    public string? RequestType { get; set; } = "CHANGE_REQUEST";

    public string? StaffComment { get; set; }

    [Required(ErrorMessage = "Danh sách nội dung yêu cầu thay đổi không được để trống.")]
    [MinLength(1, ErrorMessage = "Phải có ít nhất một trường thông tin yêu cầu thay đổi.")]
    public List<ContractChangeRequestItemInputDto> Items { get; set; } = new();
}

public class ContractChangeRequestItemInputDto
{
    [Required(ErrorMessage = "Tên trường thay đổi (FieldName) là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Tên trường không được vượt quá 100 ký tự.")]
    public string FieldName { get; set; } = null!;

    [Required(ErrorMessage = "Giá trị đề xuất (ProposedValue) là bắt buộc.")]
    public string ProposedValue { get; set; } = null!;

    public string? Reason { get; set; }
}
