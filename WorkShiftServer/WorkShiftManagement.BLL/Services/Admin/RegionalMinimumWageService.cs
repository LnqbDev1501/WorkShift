using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

/// <summary>
/// Service implementation for managing regional minimum wage business logic.
/// </summary>
public class RegionalMinimumWageService : IRegionalMinimumWageService
{
    private readonly IRegionalMinimumWageRepo _repo;

    /// <summary>
    /// Initializes a new instance of <see cref="RegionalMinimumWageService"/>.
    /// </summary>
    /// <param name="repo">The repository instance.</param>
    public RegionalMinimumWageService(IRegionalMinimumWageRepo repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Helper method to calculate the current business date according to Vietnam Timezone (Asia/Ho_Chi_Minh).
    /// </summary>
    public static DateOnly GetVietnamToday()
    {
        var utcNow = DateTimeOffset.UtcNow;
        TimeZoneInfo vnTimeZone;
        try
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        var vnTime = TimeZoneInfo.ConvertTime(utcNow, vnTimeZone);
        return DateOnly.FromDateTime(vnTime.DateTime);
    }

    private async Task ValidateEffectiveFromAgainstReplacedWageAsync(DateOnly? newEffectiveFrom, int? replacedWageId)
    {
        if (replacedWageId.HasValue && newEffectiveFrom.HasValue)
        {
            var replacedWage = await _repo.GetByIdAsync(replacedWageId.Value);
            if (replacedWage != null && replacedWage.EffectiveFrom.HasValue)
            {
                if (newEffectiveFrom.Value <= replacedWage.EffectiveFrom.Value)
                {
                    throw new ArgumentException("EffectiveFrom must be later than the replaced wage effective date.");
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWageResponse>> GetListAsync(string? status = null, string? search = null)
    {
        var entities = await _repo.GetListAsync(status, search);
        return entities.Select(MapResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWageResponse>> GetCurrentWagesAsync()
    {
        return await GetListAsync(status: "ACTIVE");
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        var entity = await _repo.GetByIdAsync(id);
        if (entity == null)
        {
            return null;
        }

        return MapDetail(entity);
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse> CreateAsync(CreateRegionalMinimumWageRequest request, Guid callerId)
    {
        if (callerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Admin user identifier is invalid.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.WageName))
        {
            throw new ArgumentException("WageName is required.");
        }

        var normalizedName = request.WageName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("WageName cannot exceed 150 characters.");
        }

        if (request.MinimumHourlyRate <= 0)
        {
            throw new ArgumentException("Minimum hourly rate must be greater than 0.");
        }

        string? normalizedDesc = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        if (request.ReplacedWageId.HasValue)
        {
            if (request.ReplacedWageId.Value <= 0)
            {
                throw new ArgumentException("ReplacedWageId must be greater than 0.");
            }

            if (!await _repo.IsActiveAsync(request.ReplacedWageId.Value))
            {
                throw new InvalidOperationException("The replaced regional minimum wage must be active.");
            }

            await ValidateEffectiveFromAgainstReplacedWageAsync(request.EffectiveFrom, request.ReplacedWageId.Value);
        }

        var entity = new RegionalMinimumWage
        {
            WageName = normalizedName,
            MinimumHourlyRate = request.MinimumHourlyRate,
            Description = normalizedDesc,
            ReplacedWageId = request.ReplacedWageId,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = null,
            Status = "DRAFT",
            CreatedBy = callerId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(entity);
        return MapDetail(created);
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse> UpdateDraftAsync(int id, UpdateRegionalMinimumWageRequest request, Guid callerId)
    {
        if (callerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Admin user identifier is invalid.");
        }

        if (id <= 0)
        {
            throw new ArgumentException("Wage id must be greater than 0.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException("Regional minimum wage not found.");
        }

        if (existing.Status != "DRAFT")
        {
            throw new InvalidOperationException("Only draft regional minimum wages can be edited.");
        }

        if (string.IsNullOrWhiteSpace(request.WageName))
        {
            throw new ArgumentException("WageName is required.");
        }

        var normalizedName = request.WageName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("WageName cannot exceed 150 characters.");
        }

        if (request.MinimumHourlyRate <= 0)
        {
            throw new ArgumentException("Minimum hourly rate must be greater than 0.");
        }

        string? normalizedDesc = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        if (request.ReplacedWageId.HasValue)
        {
            if (request.ReplacedWageId.Value <= 0)
            {
                throw new ArgumentException("ReplacedWageId must be greater than 0.");
            }

            if (request.ReplacedWageId.Value == id)
            {
                throw new ArgumentException("A regional minimum wage cannot replace itself.");
            }

            if (!await _repo.IsActiveAsync(request.ReplacedWageId.Value))
            {
                throw new InvalidOperationException("The replaced regional minimum wage must be active.");
            }

            await ValidateEffectiveFromAgainstReplacedWageAsync(request.EffectiveFrom, request.ReplacedWageId.Value);
        }

        existing.WageName = normalizedName;
        existing.MinimumHourlyRate = request.MinimumHourlyRate;
        existing.Description = normalizedDesc;
        existing.ReplacedWageId = request.ReplacedWageId;
        existing.EffectiveFrom = request.EffectiveFrom;

        var updated = await _repo.UpdateAsync(existing);
        return MapDetail(updated);
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse> PublishAsync(int id, Guid callerId)
    {
        if (callerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Admin user identifier is invalid.");
        }

        if (id <= 0)
        {
            throw new ArgumentException("Wage id must be greater than 0.");
        }

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException("Regional minimum wage not found.");
        }

        if (existing.Status != "DRAFT")
        {
            throw new InvalidOperationException("Only draft regional minimum wages can be published.");
        }

        if (!existing.EffectiveFrom.HasValue)
        {
            throw new ArgumentException("EffectiveFrom date is required before publishing.");
        }

        var today = GetVietnamToday();
        if (existing.EffectiveFrom.Value < today)
        {
            throw new ArgumentException("EffectiveFrom cannot be earlier than today.");
        }

        if (existing.MinimumHourlyRate <= 0)
        {
            throw new ArgumentException("Minimum hourly rate must be greater than 0.");
        }

        if (existing.ReplacedWageId.HasValue)
        {
            if (!await _repo.IsActiveAsync(existing.ReplacedWageId.Value))
            {
                throw new InvalidOperationException("The replaced regional minimum wage must be active.");
            }

            await ValidateEffectiveFromAgainstReplacedWageAsync(existing.EffectiveFrom, existing.ReplacedWageId.Value);

            if (await _repo.HasPublishedReplacementAsync(existing.ReplacedWageId.Value))
            {
                throw new InvalidOperationException("Another published wage is already scheduled to replace this wage.");
            }
        }

        existing.Status = "PUBLISH";
        var updated = await _repo.UpdateAsync(existing);
        return MapDetail(updated);
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse> CancelAsync(int id, Guid callerId)
    {
        if (callerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Admin user identifier is invalid.");
        }

        if (id <= 0)
        {
            throw new ArgumentException("Wage id must be greater than 0.");
        }

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
        {
            throw new KeyNotFoundException("Regional minimum wage not found.");
        }

        if (existing.Status != "DRAFT" && existing.Status != "PUBLISH")
        {
            throw new InvalidOperationException($"Cannot cancel regional minimum wage with status '{existing.Status}'. Only draft or published wages can be cancelled.");
        }

        existing.Status = "CANCEL";
        var updated = await _repo.UpdateAsync(existing);
        return MapDetail(updated);
    }

    /// <inheritdoc />
    public async Task<int> ActivateDueWagesAsync()
    {
        var today = GetVietnamToday();
        var dueWages = await _repo.GetDuePublishedWagesAsync(today);
        var activatedCount = 0;

        foreach (var wage in dueWages)
        {
            try
            {
                if (await _repo.ActivateDueWageTransactionAsync(wage.RegionalMinimumWageId, today))
                {
                    activatedCount++;
                }
            }
            catch
            {
                // Resilience: error on a single due wage should not prevent processing other due wages
            }
        }

        return activatedCount;
    }

    private static RegionalMinimumWageResponse MapResponse(RegionalMinimumWage entity) => new()
    {
        RegionalMinimumWageId = entity.RegionalMinimumWageId,
        WageName = entity.WageName,
        MinimumHourlyRate = entity.MinimumHourlyRate,
        Description = entity.Description,
        Status = entity.Status,
        EffectiveFrom = entity.EffectiveFrom,
        EffectiveTo = entity.EffectiveTo,
        ReplacedWageId = entity.ReplacedWageId
    };

    private static RegionalMinimumWageDetailResponse MapDetail(RegionalMinimumWage entity) => new()
    {
        RegionalMinimumWageId = entity.RegionalMinimumWageId,
        WageName = entity.WageName,
        MinimumHourlyRate = entity.MinimumHourlyRate,
        Description = entity.Description,
        ReplacedWageId = entity.ReplacedWageId,
        Status = entity.Status,
        EffectiveFrom = entity.EffectiveFrom,
        EffectiveTo = entity.EffectiveTo,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt
    };
}
