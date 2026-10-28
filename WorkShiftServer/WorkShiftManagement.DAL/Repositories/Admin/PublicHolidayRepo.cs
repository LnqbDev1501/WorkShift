using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class PublicHolidayRepo : IPublicHolidayRepo
{
    private readonly AppDbContext _context;

    public PublicHolidayRepo(AppDbContext context) => _context = context;

    public async Task<string?> GetActiveRoleNameAsync(Guid userId, CancellationToken ct = default)
    {
        // Read the real role from the database, not a role sent by the client.
        return await (
            from u in _context.Users.AsNoTracking()
            join r in _context.Roles.AsNoTracking() on u.RoleId equals r.RoleId
            where u.UserId == userId && u.Status == "ACTIVE"
                && u.DeletedAt == null && r.Status == "ACTIVE"
            select r.RoleName
        ).SingleOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<PublicHolidayConfig> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, int? year, int? month, string? search,
        bool includeInactive, bool? isActive, CancellationToken ct = default)
    {
        IQueryable<PublicHolidayConfig> query = _context.PublicHolidayConfigs.AsNoTracking();
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        else if (!includeInactive) query = query.Where(x => x.IsActive);

        if (year.HasValue)
        {
            var first = new DateOnly(year.Value, month ?? 1, 1);
            var last = month.HasValue
                ? new DateOnly(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value))
                : new DateOnly(year.Value, 12, 31);
            query = query.Where(x => x.HolidayDate >= first && x.HolidayDate <= last);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Escape LIKE wildcards so the search is literal and parameterized.
            var keyword = search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(x => EF.Functions.ILike(x.HolidayName, $"%{keyword}%", @"\"));
        }

        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.HolidayDate)
            .ThenBy(x => x.PublicHolidayId)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, count);
    }

    public Task<PublicHolidayConfig?> GetByIdAsync(int id, bool includeInactive,
        CancellationToken ct = default)
    {
        var query = _context.PublicHolidayConfigs.AsNoTracking()
            .Where(x => x.PublicHolidayId == id);
        if (!includeInactive) query = query.Where(x => x.IsActive);
        return query.SingleOrDefaultAsync(ct);
    }

    public async Task<PublicHolidayConfig?> GetForUpdateAsync(int id, CancellationToken ct = default)
    {
        if (_context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A transaction is required before locking a holiday.");

        // Materialize the raw query directly: do not compose LINQ over FOR UPDATE.
        var rows = await _context.PublicHolidayConfigs
            .FromSqlInterpolated($"SELECT * FROM public.public_holiday_configs WHERE public_holiday_id = {id} FOR UPDATE")
            .AsTracking().ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    public async Task<IReadOnlyList<DateOnly>> GetExistingDatesAsync(
        IReadOnlyCollection<DateOnly> dates, int? excludeId = null, CancellationToken ct = default)
    {
        if (dates.Count == 0) return Array.Empty<DateOnly>();
        var query = _context.PublicHolidayConfigs.AsNoTracking()
            .Where(x => dates.Contains(x.HolidayDate));
        if (excludeId.HasValue)
            query = query.Where(x => x.PublicHolidayId != excludeId.Value);
        // Do not filter IsActive: the date is unique even after soft delete.
        return await query.Select(x => x.HolidayDate).ToListAsync(ct);
    }

    public Task<bool> IsUsedInPayrollAsync(int id, CancellationToken ct = default) =>
        _context.PayrollDetails.AsNoTracking().AnyAsync(x => x.PublicHolidayId == id, ct);

    public Task AddRangeAsync(IEnumerable<PublicHolidayConfig> entities, CancellationToken ct = default) =>
        _context.PublicHolidayConfigs.AddRangeAsync(entities, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try { return await _context.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (IsHolidayDateUniqueViolation(ex))
        {
            throw new DuplicateNameException("A public holiday with this date already exists.", ex);
        }
    }

    private static bool IsHolidayDateUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == PostgresErrorCodes.UniqueViolation
        && pg.TableName == "public_holiday_configs"
        && (pg.ConstraintName?.Contains("holiday_date", StringComparison.OrdinalIgnoreCase) ?? false);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var result = await operation();
        await tx.CommitAsync(ct);
        return result;
        // Disposal rolls back if operation or commit fails.
    }

    public async Task WriteAuditAsync(Guid actorId, string action, int recordId,
        string? oldValueJson, string? newValueJson, CancellationToken ct = default)
    {
        // Parameterized SQL avoids guessing the scaffolded CLR type of jsonb columns.
        // The statement participates in the caller's EF transaction.
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO public.audit_logs
                (user_id, action, table_name, record_id, old_value, new_value)
            VALUES ({actorId}, {action}, 'public_holiday_configs', {recordId.ToString()},
                CAST({oldValueJson} AS jsonb), CAST({newValueJson} AS jsonb))", ct);
    }
}
