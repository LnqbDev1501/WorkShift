using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UI_Admin.Models.Manager;

public class CreateContractViewModel : IValidatableObject
{
    public string ContractType { get; set; } = "NEW";

    public int? ReplacesContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhân viên.")]
    public Guid? StaffUserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập chức danh.")]
    [StringLength(100, ErrorMessage = "Chức danh tối đa 100 ký tự.")]
    public string EmployeePosition { get; set; } = string.Empty;

    public string? ContractTerms { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> StaffOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> NewStaffOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> RenewalStaffOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (StaffUserId == Guid.Empty) yield return new("Vui lòng chọn nhân viên.", [nameof(StaffUserId)]);
        if (StartDate == default(DateOnly)) yield return new("Ngày bắt đầu không hợp lệ.", [nameof(StartDate)]);
        if (EndDate <= StartDate) yield return new("Ngày kết thúc phải sau ngày bắt đầu.", [nameof(EndDate)]);
        if (string.IsNullOrWhiteSpace(EmployeePosition)) yield return new("Vui lòng nhập chức danh.", [nameof(EmployeePosition)]);
        if (string.Equals(ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase) && (!ReplacesContractId.HasValue || ReplacesContractId.Value <= 0))
        {
            yield return new("Vui lòng chọn hợp đồng cũ cần gia hạn hoặc thay thế.", [nameof(ReplacesContractId)]);
        }
    }
}

public record ContractStaffOption(
    Guid StaffUserId,
    string FullName,
    int? CurrentContractId = null,
    string? CurrentContractNumber = null,
    string? CurrentPosition = null
);

public record ContractCreatedResponse(int ContractId, string ContractNumber, Guid StaffUserId,
    string EmployeeFullName, int BranchId, string BranchName, int RegionId, string RegionName,
    DateOnly StartDate, DateOnly? EndDate, decimal HourlyRate, string EmployeePosition,
    string? ContractTerms, string Status, Guid CreatedBy, DateTime CreatedAt);
