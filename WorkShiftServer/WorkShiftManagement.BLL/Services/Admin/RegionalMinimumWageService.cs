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

    /// <inheritdoc />
    public async Task<List<RegionalMinimumWageResponse>> GetCurrentWagesAsync()
    {
        var entities = await _repo.GetCurrentWagesAsync();

        return entities.Select(e => new RegionalMinimumWageResponse
        {
            RegionalMinimumWageId = e.RegionalMinimumWageId,
            WageName = e.WageName,
            MinimumHourlyRate = e.MinimumHourlyRate
        }).ToList();
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

        // Validate uniqueness of current active wage name
        if (await _repo.CurrentNameExistsAsync(normalizedName))
        {
            throw new InvalidOperationException("Regional minimum wage name already exists.");
        }

        var entity = new RegionalMinimumWage
        {
            WageName = normalizedName,
            MinimumHourlyRate = request.MinimumHourlyRate,
            Description = normalizedDesc,
            ReplacedWageId = null,
            CreatedBy = callerId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(entity);
        return MapDetail(created);
    }

    /// <inheritdoc />
    public async Task<RegionalMinimumWageDetailResponse> UpdateAsync(int id, UpdateRegionalMinimumWageRequest request, Guid callerId)
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

        // Check if the wage record has already been replaced
        if (await _repo.IsAlreadyReplacedAsync(id))
        {
            throw new InvalidOperationException("This regional minimum wage has already been replaced.");
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

        // Validate uniqueness of current active wage name excluding self
        if (await _repo.CurrentNameExistsAsync(normalizedName, excludeId: id))
        {
            throw new InvalidOperationException("Regional minimum wage name already exists.");
        }

        var newWageEntity = new RegionalMinimumWage
        {
            WageName = normalizedName,
            MinimumHourlyRate = request.MinimumHourlyRate,
            Description = normalizedDesc,
            ReplacedWageId = id,
            CreatedBy = callerId,
            CreatedAt = DateTime.UtcNow
        };

        // Transactionally insert new replacement record & update referencing regions
        var createdReplacement = await _repo.ReplaceWageAsync(id, newWageEntity);
        return MapDetail(createdReplacement);
    }

    private static RegionalMinimumWageDetailResponse MapDetail(RegionalMinimumWage entity) => new()
    {
        RegionalMinimumWageId = entity.RegionalMinimumWageId,
        WageName = entity.WageName,
        MinimumHourlyRate = entity.MinimumHourlyRate,
        Description = entity.Description,
        ReplacedWageId = entity.ReplacedWageId,
        CreatedBy = entity.CreatedBy,
        CreatedAt = entity.CreatedAt
    };
}
