using WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Request;
using WorkShiftManagement.BLL.DTOs.Accountant.SalaryHistory.Response;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;

namespace WorkShiftManagement.BLL.Services.Accountant;

public sealed class SalaryHistoryService : ISalaryHistoryService
{
    private readonly ISalaryHistoryRepo _repo;

    public SalaryHistoryService(ISalaryHistoryRepo repo) => _repo = repo;

    private async Task<int> RequireBranchAsync(
        Guid accountantId,
        CancellationToken ct)
    {
        var branchId = await _repo.GetActiveAccountantBranchIdAsync(
            accountantId,
            ct);

        if (branchId is null or <= 0)
        {
            throw new UnauthorizedAccessException(
                "Only an active Accountant assigned to a branch can view salary history.");
        }

        return branchId.Value;
    }

    private static string? ValidateFilter(
        string? value,
        string field,
        params string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value
            .Trim()
            .ToUpperInvariant();

        if (!allowed.Contains(normalized))
        {
            throw new ArgumentException(
                $"{field} must be one of: {string.Join(", ", allowed)}.");
        }

        return normalized;
    }

    public async Task<PagedResponse<SalaryHistoryResponse>> GetAllAsync(
        SalaryHistoryListRequest request,
        Guid accountantId,
        CancellationToken ct = default)
    {
        var branchId = await RequireBranchAsync(accountantId, ct);

        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1
            || request.PageSize is < 1 or > 100
            || (long)(request.Page - 1) * request.PageSize > int.MaxValue)
        {
            throw new ArgumentException(
                "Page must be positive and in range; PageSize must be between 1 and 100.");
        }

        if (request.UserId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId must not be an empty UUID.");
        }

        if (request.Keyword?.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Keyword cannot exceed 200 characters.");
        }

        if (request.EffectiveFrom.HasValue
            && request.EffectiveTo.HasValue
            && request.EffectiveFrom.Value > request.EffectiveTo.Value)
        {
            throw new ArgumentException(
                "EffectiveFrom must not be later than EffectiveTo.");
        }

        var status = ValidateFilter(
            request.Status,
            "Status",
            "SCHEDULED",
            "ACTIVE",
            "INACTIVE");

        var changeType = ValidateFilter(
            request.ChangeType,
            "ChangeType",
            "INITIAL",
            "INCREASE",
            "DECREASE");

        var (items, count) = await _repo.GetPagedAsync(
            branchId,
            request.Keyword?.Trim(),
            request.UserId,
            status,
            changeType,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.Page,
            request.PageSize,
            ct);

        return new PagedResponse<SalaryHistoryResponse>
        {
            Items = items
                .Select(Map)
                .ToList(),

            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = count
        };
    }

    public async Task<SalaryHistoryResponse> GetByIdAsync(
        int id,
        Guid accountantId,
        CancellationToken ct = default)
    {
        var branchId = await RequireBranchAsync(accountantId, ct);

        if (id <= 0)
        {
            throw new ArgumentException(
                "Salary history ID must be greater than 0.");
        }

        var history = await _repo.GetByIdAsync(
            id,
            branchId,
            ct)
            ?? throw new KeyNotFoundException(
                "Salary history was not found in your branch.");

        return Map(history);
    }

    private static SalaryHistoryResponse Map(
        StaffSalaryHistory history) => new()
        {
            SalaryHistoryId = history.SalaryHistoryId,
            UserId = history.UserId,
            EmployeeFullName = history.User.FullName,
            HourlyRate = history.HourlyRate,
            PreviousHourlyRate = history.PreviousHourlyRate,
            ChangeType = history.ChangeType,
            EffectiveFrom = history.EffectiveFrom,
            EffectiveTo = history.EffectiveTo,
            Status = history.Status,
            Reason = history.Reason,
            SalaryIncreaseRequestId = history.SalaryIncreaseRequestId,
            CreatedBy = history.CreatedBy,
            CreatedByFullName = history.CreatedByNavigation.FullName,
            CreatedAt = history.CreatedAt,

            Contract = new SalaryHistoryContractResponse
            {
                ContractId = history.ContractId,
                ContractNumber = history.Contract.ContractNumber,
                BranchId = history.Contract.BranchId,
                BranchName = history.Contract.BranchNameSnapshot,
                EmployeePosition = history.Contract.EmployeePositionSnapshot,
                StartDate = history.Contract.StartDate,
                EndDate = history.Contract.EndDate,
                Status = history.Contract.Status
            }
        };
}