using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class EmployeeTypeRepo : IEmployeeTypeRepo
{
    public const string NormalizedNameUniqueIndex = "ux_employee_types_normalized_type_name";
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

    public Task<EmployeeType?> GetByIdAsync(int id) => _context.EmployeeTypes.AsTracking().FirstOrDefaultAsync(x => x.EmployeeTypeId == id);

    public Task<bool> ExistsByNameExceptIdAsync(string typeName, int excludeId)
    {
        var normalized = typeName.Trim().ToLowerInvariant();
        return _context.EmployeeTypes.AnyAsync(x => x.EmployeeTypeId != excludeId && x.TypeName.Trim().ToLower() == normalized);
    }

    public async Task CreateAsync(EmployeeType employeeType)
    {
        _context.EmployeeTypes.Add(employeeType);
        await SaveChangesAsync();
    }

    public Task UpdateAsync(EmployeeType employeeType)
    {
        // GetByIdAsync returns a tracked entity. Preserve its original property values so EF
        // only writes the fields changed by edit or status actions, not a stale whole row.
        return SaveChangesAsync();
    }

    public async Task<EmployeeTypeStatusChangeResult> ChangeStatusAsync(int id, bool restore)
    {
        var expectedStatus = restore ? "INACTIVE" : "ACTIVE";
        var targetStatus = restore ? "ACTIVE" : "INACTIVE";
        var now = DateTime.UtcNow;

        // PostgreSQL checks the status again after any concurrent writer releases its row lock.
        // A repeated action affects zero rows and never changes UpdatedAt or unrelated columns.
        var changed = await _context.EmployeeTypes
            .Where(x => x.EmployeeTypeId == id && x.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, targetStatus)
                .SetProperty(x => x.UpdatedAt, now));
        if (changed > 0) return EmployeeTypeStatusChangeResult.Changed;

        if (!await _context.EmployeeTypes.AsNoTracking().AnyAsync(x => x.EmployeeTypeId == id))
            return EmployeeTypeStatusChangeResult.NotFound;

        return restore ? EmployeeTypeStatusChangeResult.AlreadyActive : EmployeeTypeStatusChangeResult.AlreadyInactive;
    }

    private async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: NormalizedNameUniqueIndex or "employee_types_type_name_key"
        })
        {
            throw new InvalidOperationException("Employee type name already exists.", ex);
        }
    }
}
