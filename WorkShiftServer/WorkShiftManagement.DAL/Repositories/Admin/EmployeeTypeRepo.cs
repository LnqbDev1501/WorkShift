using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class EmployeeTypeRepo : IEmployeeTypeRepo
{
    private readonly AppDbContext _context;

    public EmployeeTypeRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<EmployeeType> Items, int TotalCount)> GetPagedAsync(string? search, string? status, int pageNumber, int pageSize)
    {
        var query = _context.EmployeeTypes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.TypeName.ToLower().Contains(term));
        }
        if (status != null) query = query.Where(x => x.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.EmployeeTypeId).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<EmployeeType?> GetByIdAsync(int id) => _context.EmployeeTypes.FirstOrDefaultAsync(x => x.EmployeeTypeId == id);

    public Task<bool> ExistsByNameExceptIdAsync(string typeName, int excludeId)
    {
        var normalized = typeName.Trim().ToLowerInvariant();
        return _context.EmployeeTypes.AnyAsync(x => x.EmployeeTypeId != excludeId && x.TypeName.ToLower() == normalized);
    }

    public async Task CreateAsync(EmployeeType employeeType)
    {
        _context.EmployeeTypes.Add(employeeType);
        await SaveChangesAsync();
    }

    public Task UpdateAsync(EmployeeType employeeType)
    {
        _context.EmployeeTypes.Update(employeeType);
        return SaveChangesAsync();
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "employee_types_type_name_key" })
        {
            throw new InvalidOperationException("Employee type name already exists.", ex);
        }
    }
}

