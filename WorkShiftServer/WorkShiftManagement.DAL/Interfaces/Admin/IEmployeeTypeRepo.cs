using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IEmployeeTypeRepo
{
    Task<(List<EmployeeType> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int pageNumber, int pageSize);
    Task<EmployeeType?> GetByIdAsync(int id);
    Task<bool> ExistsByNameExceptIdAsync(string typeName, int excludeId);
    Task CreateAsync(EmployeeType employeeType);
    // Save only changed properties of an entity tracked by GetByIdAsync in this repository scope.
    Task UpdateAsync(EmployeeType employeeType);
    Task<EmployeeTypeStatusChangeResult> ChangeStatusAsync(int id, bool restore);
}
