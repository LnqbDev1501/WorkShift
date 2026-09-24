using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IEmployeeTypeRepo
{
    Task<(List<EmployeeType> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int pageNumber, int pageSize);
    Task<EmployeeType?> GetByIdAsync(int id);
    Task<bool> ExistsByNameExceptIdAsync(string typeName, int excludeId);
    Task CreateAsync(EmployeeType employeeType);
    Task UpdateAsync(EmployeeType employeeType);
}
