using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

/// <summary>
/// Business logic implementation for region management operations.
/// </summary>
public class RegionService : IRegionService
{
    private readonly IRegionRepo _regionRepo;
    private readonly IRegionalMinimumWageRepo _wageRepo;

    /// <summary>
    /// Initializes a new instance of <see cref="RegionService"/>.
    /// </summary>
    /// <param name="regionRepo">The region repository dependency.</param>
    /// <param name="wageRepo">The regional minimum wage repository dependency.</param>
    public RegionService(IRegionRepo regionRepo, IRegionalMinimumWageRepo wageRepo)
    {
        _regionRepo = regionRepo;
        _wageRepo = wageRepo;
    }

    /// <inheritdoc />
    public async Task<List<RegionResponse>> GetAllAsync()
    {
        var regions = await _regionRepo.GetAllAsync();
        return regions.Select(region => new RegionResponse
        {
            RegionId = region.RegionId,
            RegionCode = region.RegionCode,
            RegionName = region.RegionName,
            Description = region.Description,
            RegionalMinimumWageId = region.RegionalMinimumWageId,
            WageName = region.RegionalMinimumWage?.WageName,
            MinimumHourlyRate = region.RegionalMinimumWage?.MinimumHourlyRate,
            BranchCount = region.Branches.Count(),
            Status = region.IsActive ? "ACTIVE" : "INACTIVE"
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<PagedResponse<RegionResponse>> GetPagedAsync(RegionListQuery query)
    {
        // Normalize page number and page size parameters
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : (query.PageSize > 100 ? 100 : query.PageSize);

        // Normalize and validate status filter
        bool? isActiveFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var normalizedStatus = query.Status.Trim().ToUpperInvariant();
            if (normalizedStatus == "ACTIVE")
            {
                isActiveFilter = true;
            }
            else if (normalizedStatus == "INACTIVE")
            {
                isActiveFilter = false;
            }
            else
            {
                throw new ArgumentException("Status filter only accepts 'ACTIVE' or 'INACTIVE'.");
            }
        }

        // Fetch paged data and total count from DAL
        var (items, totalCount) = await _regionRepo.GetPagedAsync(
            query.Search,
            isActiveFilter,
            pageNumber,
            pageSize);

        // Map DAL models to response DTOs
        var mappedItems = items.Select(item => new RegionResponse
        {
            RegionId = item.RegionId,
            RegionCode = item.RegionCode,
            RegionName = item.RegionName,
            Description = item.Description,
            RegionalMinimumWageId = item.RegionalMinimumWageId,
            WageName = item.WageName,
            MinimumHourlyRate = item.MinimumHourlyRate,
            BranchCount = item.BranchCount,
            Status = item.IsActive ? "ACTIVE" : "INACTIVE"
        }).ToList();

        return new PagedResponse<RegionResponse>
        {
            Items = mappedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<RegionDetailResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        var data = await _regionRepo.GetByIdAsync(id);
        if (data == null)
        {
            return null;
        }

        return new RegionDetailResponse
        {
            RegionId = data.RegionId,
            RegionCode = data.RegionCode,
            RegionName = data.RegionName,
            Description = data.Description,
            Status = data.IsActive ? "ACTIVE" : "INACTIVE",
            MinimumWage = data.RegionalMinimumWageId.HasValue
                ? new RegionalMinimumWageResponse
                {
                    RegionalMinimumWageId = data.RegionalMinimumWageId.Value,
                    WageName = data.WageName ?? string.Empty,
                    MinimumHourlyRate = data.MinimumHourlyRate ?? 0m
                }
                : null,
            BranchCount = data.Branches.Count,
            Branches = data.Branches.Select(branch => new RegionBranchResponse
            {
                BranchId = branch.BranchId,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                Status = branch.Status
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<RegionResponse> CreateAsync(CreateRegionRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.RegionCode))
        {
            throw new ArgumentException("RegionCode is required.");
        }

        var normalizedCode = request.RegionCode.Trim().ToUpperInvariant();
        if (normalizedCode.Length > 20)
        {
            throw new ArgumentException("RegionCode cannot exceed 20 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.RegionName))
        {
            throw new ArgumentException("RegionName is required.");
        }

        var normalizedName = request.RegionName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("RegionName cannot exceed 150 characters.");
        }

        string? normalizedDescription = null;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            normalizedDescription = request.Description.Trim();
            if (normalizedDescription.Length > 500)
            {
                throw new ArgumentException("Description cannot exceed 500 characters.");
            }
        }

        // Validate RegionalMinimumWageId
        if (request.RegionalMinimumWageId <= 0)
        {
            throw new ArgumentException("RegionalMinimumWageId must be greater than 0.");
        }

        if (!await _wageRepo.ExistsAsync(request.RegionalMinimumWageId))
        {
            throw new ArgumentException("Regional minimum wage does not exist.");
        }

        // Validate uniqueness of RegionCode and RegionName
        if (await _regionRepo.ExistsByCodeAsync(normalizedCode))
        {
            throw new InvalidOperationException("Region code already exists.");
        }

        if (await _regionRepo.ExistsByNameAsync(normalizedName))
        {
            throw new InvalidOperationException("Region name already exists.");
        }

        var newRegion = new Region
        {
            RegionCode = normalizedCode,
            RegionName = normalizedName,
            Description = normalizedDescription,
            RegionalMinimumWageId = request.RegionalMinimumWageId,
            IsActive = true
        };

        var createdData = await _regionRepo.CreateAsync(newRegion);

        return new RegionResponse
        {
            RegionId = createdData.RegionId,
            RegionCode = createdData.RegionCode,
            RegionName = createdData.RegionName,
            Description = createdData.Description,
            RegionalMinimumWageId = createdData.RegionalMinimumWageId,
            WageName = createdData.WageName,
            MinimumHourlyRate = createdData.MinimumHourlyRate,
            BranchCount = createdData.BranchCount,
            Status = createdData.IsActive ? "ACTIVE" : "INACTIVE"
        };
    }

    /// <inheritdoc />
    public async Task<RegionResponse?> UpdateAsync(int id, UpdateRegionRequest request)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Region id must be greater than 0.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.RegionName))
        {
            throw new ArgumentException("RegionName is required.");
        }

        var normalizedName = request.RegionName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("RegionName cannot exceed 150 characters.");
        }

        string? normalizedDescription = null;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            normalizedDescription = request.Description.Trim();
            if (normalizedDescription.Length > 500)
            {
                throw new ArgumentException("Description cannot exceed 500 characters.");
            }
        }

        // Validate RegionalMinimumWageId
        if (request.RegionalMinimumWageId <= 0)
        {
            throw new ArgumentException("RegionalMinimumWageId must be greater than 0.");
        }

        if (!await _wageRepo.ExistsAsync(request.RegionalMinimumWageId))
        {
            throw new ArgumentException("Regional minimum wage does not exist.");
        }

        var existingData = await _regionRepo.GetByIdAsync(id);
        if (existingData == null)
        {
            return null;
        }

        // Validate uniqueness of RegionName excluding the current region
        if (await _regionRepo.ExistsByNameExceptIdAsync(normalizedName, id))
        {
            throw new InvalidOperationException("Region name already exists.");
        }

        var updatedData = await _regionRepo.UpdateAsync(id, normalizedName, normalizedDescription, request.RegionalMinimumWageId);
        if (updatedData == null)
        {
            return null;
        }

        return new RegionResponse
        {
            RegionId = updatedData.RegionId,
            RegionCode = updatedData.RegionCode,
            RegionName = updatedData.RegionName,
            Description = updatedData.Description,
            RegionalMinimumWageId = updatedData.RegionalMinimumWageId,
            WageName = updatedData.WageName,
            MinimumHourlyRate = updatedData.MinimumHourlyRate,
            BranchCount = updatedData.BranchCount,
            Status = updatedData.IsActive ? "ACTIVE" : "INACTIVE"
        };
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Region id must be greater than 0.");
        }

        var existingData = await _regionRepo.GetByIdAsync(id);
        if (existingData == null)
        {
            return false;
        }

        if (!existingData.IsActive)
        {
            throw new InvalidOperationException("Region is already inactive.");
        }

        return await _regionRepo.SoftDeleteAsync(id);
    }

    /// <inheritdoc />
    public async Task<bool> RestoreAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Region id must be greater than 0.");
        }

        var existingData = await _regionRepo.GetByIdAsync(id);
        if (existingData == null)
        {
            return false;
        }

        if (existingData.IsActive)
        {
            throw new InvalidOperationException("Region is already active.");
        }

        return await _regionRepo.RestoreAsync(id);
    }
}
