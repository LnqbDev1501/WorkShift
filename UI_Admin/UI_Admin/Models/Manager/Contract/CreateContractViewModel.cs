using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace UI_Admin.Models.Manager.Contract;

public class CreateContractViewModel : IValidatableObject
{
    public string ContractType { get; set; } = "NEW";

    public int? ReplacesContractId { get; set; }

    [Required(ErrorMessage = "Please select a staff member.")]
    public Guid? StaffUserId { get; set; }

    [Required(ErrorMessage = "Please enter a start date.")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Required(ErrorMessage = "Please enter a position/title.")]
    [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
    public string EmployeePosition { get; set; } = string.Empty;

    [Range(0, 100000000, ErrorMessage = "Hourly rate must be a positive number.")]
    [Display(Name = "Hourly Rate (VND/h)")]
    public decimal? HourlyRate { get; set; }

    public string? ContractTerms { get; set; }

    [BindNever, ValidateNever]
    public decimal RegionalMinimumWage { get; set; }

    [BindNever, ValidateNever]
    public IReadOnlyList<string> PositionOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> StaffOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> NewStaffOptions { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<ContractStaffOption> RenewalStaffOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StaffUserId == Guid.Empty) yield return new("Please select a staff member.", [nameof(StaffUserId)]);
        if (StartDate == default(DateOnly)) yield return new("Invalid start date.", [nameof(StartDate)]);
        
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        if (StartDate.HasValue && StartDate.Value < today)
        {
            yield return new("The contract start date cannot be in the past. It must be today or a future date.", [nameof(StartDate)]);
        }

        if (EndDate <= StartDate) yield return new("End date must be after start date.", [nameof(EndDate)]);
        if (string.IsNullOrWhiteSpace(EmployeePosition)) yield return new("Please enter a position/title.", [nameof(EmployeePosition)]);
        if (string.Equals(ContractType, "RENEWAL", StringComparison.OrdinalIgnoreCase) && (!ReplacesContractId.HasValue || ReplacesContractId.Value <= 0))
        {
            yield return new("Please select an existing contract to renew or replace.", [nameof(ReplacesContractId)]);
        }
    }
}

public record ContractStaffOption(
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
    string? ExpirationCategory = null,
    string? CurrentContractTerms = null
);

public record ContractCreatedResponse(int ContractId, string ContractNumber, Guid StaffUserId,
    string EmployeeFullName, int BranchId, string BranchName, int RegionId, string RegionName,
    DateOnly StartDate, DateOnly? EndDate, decimal HourlyRate, string EmployeePosition,
    string? ContractTerms, string Status, Guid CreatedBy, DateTime CreatedAt);
