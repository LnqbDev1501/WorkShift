using System.Net.Mail;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class BranchService : IBranchService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACTIVE",
        "INACTIVE",
        "CLOSED"
    };

    private readonly IBranchRepo _branchRepo;
    private readonly IRegionRepo _regionRepo;

    public BranchService(IBranchRepo branchRepo, IRegionRepo regionRepo)
    {
        _branchRepo = branchRepo;
        _regionRepo = regionRepo;
    }

    public async Task<PagedResponse<BranchResponse>> GetPagedAsync(BranchListQuery query)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : (query.PageSize > 100 ? 100 : query.PageSize);

        string? normalizedStatusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var trimmedStatus = query.Status.Trim().ToUpperInvariant();
            if (!ValidStatuses.Contains(trimmedStatus))
            {
                throw new ArgumentException("Status filter only accepts 'ACTIVE', 'INACTIVE', or 'CLOSED'.");
            }
            normalizedStatusFilter = trimmedStatus;
        }

        var (items, totalCount) = await _branchRepo.GetPagedAsync(
            query.Search,
            query.RegionId,
            normalizedStatusFilter,
            pageNumber,
            pageSize);

        var mappedItems = items.Select(item => new BranchResponse
        {
            BranchId = item.BranchId,
            BranchCode = item.BranchCode,
            BranchName = item.BranchName,
            RegionId = item.RegionId,
            RegionCode = item.RegionCode,
            RegionName = item.RegionName,
            Address = item.Address,
            EmployeeCount = item.EmployeeCount,
            Status = item.Status
        }).ToList();

        return new PagedResponse<BranchResponse>
        {
            Items = mappedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    public async Task<BranchDetailResponse?> GetByIdAsync(int id)
    {
        if (id <= 0)
        {
            return null;
        }

        var data = await _branchRepo.GetByIdAsync(id);
        if (data == null)
        {
            return null;
        }

        return new BranchDetailResponse
        {
            BranchId = data.BranchId,
            BranchCode = data.BranchCode,
            BranchName = data.BranchName,
            Region = new BranchRegionResponse
            {
                RegionId = data.RegionId,
                RegionCode = data.RegionCode,
                RegionName = data.RegionName,
                Status = data.RegionStatus
            },
            Address = data.Address,
            Latitude = data.Latitude,
            Longitude = data.Longitude,
            Phone = data.Phone,
            Email = data.Email,
            EmployeeCount = data.EmployeeCount,
            Status = data.Status
        };
    }

    public async Task<BranchResponse> CreateAsync(CreateBranchRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.BranchCode))
        {
            throw new ArgumentException("BranchCode is required.");
        }

        var normalizedCode = request.BranchCode.Trim().ToUpperInvariant();
        if (normalizedCode.Length > 30)
        {
            throw new ArgumentException("BranchCode cannot exceed 30 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.BranchName))
        {
            throw new ArgumentException("BranchName is required.");
        }

        var normalizedName = request.BranchName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("BranchName cannot exceed 150 characters.");
        }

        if (request.RegionId <= 0)
        {
            throw new ArgumentException("Valid RegionId is required.");
        }

        var region = await _regionRepo.GetByIdAsync(request.RegionId);
        if (region == null)
        {
            throw new ArgumentException("Region does not exist.");
        }

        if (!region.IsActive)
        {
            throw new ArgumentException("Cannot create branch in an inactive region.");
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new ArgumentException("Address is required.");
        }

        var normalizedAddress = request.Address.Trim();
        if (normalizedAddress.Length > 500)
        {
            throw new ArgumentException("Address cannot exceed 500 characters.");
        }

        if (request.Latitude.HasValue && (request.Latitude.Value < -90m || request.Latitude.Value > 90m))
        {
            throw new ArgumentException("Latitude must be between -90 and 90.");
        }

        if (request.Longitude.HasValue && (request.Longitude.Value < -180m || request.Longitude.Value > 180m))
        {
            throw new ArgumentException("Longitude must be between -180 and 180.");
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            normalizedPhone = request.Phone.Trim();
            if (normalizedPhone.Length > 20)
            {
                throw new ArgumentException("Phone cannot exceed 20 characters.");
            }
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            normalizedEmail = request.Email.Trim();
            if (normalizedEmail.Length > 254)
            {
                throw new ArgumentException("Email cannot exceed 254 characters.");
            }

            if (!IsValidEmail(normalizedEmail))
            {
                throw new ArgumentException("Invalid email format.");
            }
        }

        if (await _branchRepo.ExistsByCodeAsync(normalizedCode))
        {
            throw new InvalidOperationException("Branch code already exists.");
        }

        if (await _branchRepo.ExistsByNameAsync(normalizedName))
        {
            throw new InvalidOperationException("Branch name already exists.");
        }

        var now = DateTime.UtcNow;
        var newBranch = new Branch
        {
            BranchCode = normalizedCode,
            BranchName = normalizedName,
            RegionId = request.RegionId,
            Address = normalizedAddress,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Phone = normalizedPhone,
            Email = normalizedEmail,
            Status = "ACTIVE",
            CreatedAt = now,
            UpdatedAt = now
        };

        var createdData = await _branchRepo.CreateAsync(newBranch);

        return new BranchResponse
        {
            BranchId = createdData.BranchId,
            BranchCode = createdData.BranchCode,
            BranchName = createdData.BranchName,
            RegionId = createdData.RegionId,
            RegionCode = createdData.RegionCode,
            RegionName = createdData.RegionName,
            Address = createdData.Address,
            EmployeeCount = createdData.EmployeeCount,
            Status = createdData.Status
        };
    }

    public async Task<BranchDetailResponse?> UpdateAsync(int id, UpdateBranchRequest request)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Branch id must be greater than 0.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request body cannot be null.");
        }

        var branch = await _branchRepo.GetEntityByIdAsync(id);
        if (branch == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.BranchName))
        {
            throw new ArgumentException("BranchName is required.");
        }

        var normalizedName = request.BranchName.Trim();
        if (normalizedName.Length > 150)
        {
            throw new ArgumentException("BranchName cannot exceed 150 characters.");
        }

        if (request.RegionId <= 0)
        {
            throw new ArgumentException("Valid RegionId is required.");
        }

        var region = await _regionRepo.GetByIdAsync(request.RegionId);
        if (region == null)
        {
            throw new ArgumentException("Region does not exist.");
        }

        if (!region.IsActive)
        {
            throw new ArgumentException("Cannot move branch to an inactive region.");
        }

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new ArgumentException("Address is required.");
        }

        var normalizedAddress = request.Address.Trim();
        if (normalizedAddress.Length > 500)
        {
            throw new ArgumentException("Address cannot exceed 500 characters.");
        }

        if (request.Latitude.HasValue && (request.Latitude.Value < -90m || request.Latitude.Value > 90m))
        {
            throw new ArgumentException("Latitude must be between -90 and 90.");
        }

        if (request.Longitude.HasValue && (request.Longitude.Value < -180m || request.Longitude.Value > 180m))
        {
            throw new ArgumentException("Longitude must be between -180 and 180.");
        }

        string? normalizedPhone = null;
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            normalizedPhone = request.Phone.Trim();
            if (normalizedPhone.Length > 20)
            {
                throw new ArgumentException("Phone cannot exceed 20 characters.");
            }
        }

        string? normalizedEmail = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            normalizedEmail = request.Email.Trim();
            if (normalizedEmail.Length > 254)
            {
                throw new ArgumentException("Email cannot exceed 254 characters.");
            }

            if (!IsValidEmail(normalizedEmail))
            {
                throw new ArgumentException("Invalid email format.");
            }
        }

        if (await _branchRepo.ExistsByNameExceptIdAsync(normalizedName, id))
        {
            throw new InvalidOperationException("Branch name already exists.");
        }

        branch.BranchName = normalizedName;
        branch.RegionId = request.RegionId;
        branch.Address = normalizedAddress;
        branch.Latitude = request.Latitude;
        branch.Longitude = request.Longitude;
        branch.Phone = normalizedPhone;
        branch.Email = normalizedEmail;
        branch.UpdatedAt = DateTime.UtcNow;

        var updatedData = await _branchRepo.UpdateAsync(branch);
        if (updatedData == null)
        {
            return null;
        }

        return new BranchDetailResponse
        {
            BranchId = updatedData.BranchId,
            BranchCode = updatedData.BranchCode,
            BranchName = updatedData.BranchName,
            Region = new BranchRegionResponse
            {
                RegionId = updatedData.RegionId,
                RegionCode = updatedData.RegionCode,
                RegionName = updatedData.RegionName,
                Status = updatedData.RegionStatus
            },
            Address = updatedData.Address,
            Latitude = updatedData.Latitude,
            Longitude = updatedData.Longitude,
            Phone = updatedData.Phone,
            Email = updatedData.Email,
            EmployeeCount = updatedData.EmployeeCount,
            Status = updatedData.Status
        };
    }

    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Branch id must be greater than 0.");
        }

        var branch = await _branchRepo.GetByIdAsync(id);
        if (branch == null)
        {
            return false;
        }

        if (string.Equals(branch.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Branch is already inactive.");
        }

        return await _branchRepo.SoftDeleteAsync(id);
    }

    public async Task<bool> RestoreAsync(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("Branch id must be greater than 0.");
        }

        var branch = await _branchRepo.GetByIdAsync(id);
        if (branch == null)
        {
            return false;
        }

        if (string.Equals(branch.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Branch is already active.");
        }

        if (!branch.RegionIsActive)
        {
            throw new ArgumentException("Cannot restore branch because its region is inactive.");
        }

        return await _branchRepo.RestoreAsync(id);
    }

    public async Task<BranchEmployeesResponse?> GetEmployeesAsync(int branchId, BranchEmployeeListQuery query)
    {
        if (branchId <= 0)
        {
            return null;
        }

        var branchData = await _branchRepo.GetByIdAsync(branchId);
        if (branchData == null)
        {
            return null;
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : (query.PageSize > 100 ? 100 : query.PageSize);

        var (items, totalCount) = await _branchRepo.GetEmployeesPagedAsync(
            branchId,
            query.Search,
            query.Status,
            query.Role,
            pageNumber,
            pageSize);

        var mappedItems = items.Select(item => new BranchEmployeeResponse
        {
            UserId = item.UserId,
            FullName = item.FullName,
            Email = item.Email,
            Phone = item.Phone,
            Role = item.Role,
            Status = item.Status
        }).ToList();

        return new BranchEmployeesResponse
        {
            Branch = new BranchSummaryResponse
            {
                BranchId = branchData.BranchId,
                BranchCode = branchData.BranchCode,
                BranchName = branchData.BranchName,
                RegionCode = branchData.RegionCode,
                Status = branchData.Status
            },
            Items = mappedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
