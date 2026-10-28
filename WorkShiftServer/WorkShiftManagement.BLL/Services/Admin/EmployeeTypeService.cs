using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.BLL.Services.Admin;

public class EmployeeTypeService : IEmployeeTypeService
{
    private readonly IEmployeeTypeRepo _employeeTypeRepo;

    public EmployeeTypeService(IEmployeeTypeRepo employeeTypeRepo)
    {
        _employeeTypeRepo = employeeTypeRepo;
    }

    public async Task<PagedResponse<EmployeeTypeResponse>> GetPagedAsync(EmployeeTypeListQuery query)
    {
        var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();
        if (status != null && status != "ACTIVE" && status != "INACTIVE")
            throw new ArgumentException("Status must be ACTIVE or INACTIVE.");
        var pageNumber = Math.Clamp(query.PageNumber, 1, int.MaxValue / 100);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _employeeTypeRepo.GetPagedAsync(query.Search, status, pageNumber, pageSize);
        return new() { Items = items.Select(Map).ToList(), TotalItems = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<EmployeeTypeResponse?> GetByIdAsync(int id)
    {
        var entity = await _employeeTypeRepo.GetByIdAsync(id);
        return entity == null ? null : Map(entity);
    }

    public async Task<EmployeeTypeResponse> CreateAsync(CreateEmployeeTypeRequest request)
    {
        await ValidateAsync(request.TypeName, request.Description, 0);
        var entity = new EmployeeType { CreatedAt = DateTime.UtcNow, Status = "ACTIVE" };
        Apply(entity, request.TypeName, request.Description);
        await _employeeTypeRepo.CreateAsync(entity);
        return Map(entity);
    }

    public async Task<EmployeeTypeResponse?> UpdateAsync(int id, UpdateEmployeeTypeRequest request)
    {
        var entity = await _employeeTypeRepo.GetByIdAsync(id);
        if (entity == null) return null;
        await ValidateAsync(request.TypeName, request.Description, id);
        Apply(entity, request.TypeName, request.Description);
        await _employeeTypeRepo.UpdateAsync(entity);
        return Map(entity);
    }

    public Task<EmployeeTypeStatusChangeResult> DeleteAsync(int id) =>
        _employeeTypeRepo.ChangeStatusAsync(id, restore: false);

    public Task<EmployeeTypeStatusChangeResult> RestoreAsync(int id) =>
        _employeeTypeRepo.ChangeStatusAsync(id, restore: true);

    private async Task ValidateAsync(string typeName, string? description, int id)
    {
        if (string.IsNullOrWhiteSpace(typeName) || typeName.Trim().Length > 100)
            throw new ArgumentException("Employee type name must contain between 1 and 100 characters.");
        if (description?.Trim().Length > 500)
            throw new ArgumentException("Description cannot exceed 500 characters.");
        if (await _employeeTypeRepo.ExistsByNameExceptIdAsync(typeName.Trim(), id))
            throw new InvalidOperationException("Employee type name already exists.");
    }

    private static void Apply(EmployeeType entity, string typeName, string? description)
    {
        entity.TypeName = typeName.Trim();
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        entity.UpdatedAt = DateTime.UtcNow;
    }

    private static EmployeeTypeResponse Map(EmployeeType entity) => new()
    {
        EmployeeTypeId = entity.EmployeeTypeId,
        TypeName = entity.TypeName,
        Description = entity.Description,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}


