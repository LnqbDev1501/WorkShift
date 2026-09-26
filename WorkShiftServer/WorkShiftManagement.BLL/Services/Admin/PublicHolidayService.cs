using System.Data;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Repositories.Admin;
using System.Text.Json;
using Npgsql;
using WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Request;
using WorkShiftManagement.BLL.DTOs.Admin.PublicHoliday.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class PublicHolidayService : IPublicHolidayService
{
    private const int PageSize = 20;
    private const int MaxBatchSize = 366;
    private readonly IPublicHolidayRepo _repo;

    private readonly HolidayConfigurationRepo _config;
    public PublicHolidayService(IPublicHolidayRepo repo, HolidayConfigurationRepo config) { _repo = repo; _config = config; }

    private static T Error<T>(T exception, string code) where T : Exception
    {
        exception.Data["code"] = code;
        return exception;
    }

    // Authorization is enforced by the admin controller; audit uses its authenticated actor.
    private Task WriteAuditIfKnownAsync(
        Guid callerId, string action, int recordId,
        string? oldValueJson, string? newValueJson,
        CancellationToken ct)
    {
        if (callerId == Guid.Empty)
            return Task.CompletedTask;

        return _repo.WriteAuditAsync(callerId, action, recordId,
            oldValueJson, newValueJson, ct);
    }

    // Recognize the PostgreSQL UNIQUE violation even if the Repository
    // wraps it in another exception. No custom Exception class is required.
    private static bool IsHolidayDateConflict(Exception ex)
    {
        if (ex is DuplicateNameException)
            return true;

        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && pg.TableName == "public_holiday_configs"
                && (pg.ConstraintName?.Contains("holiday_date",
                    StringComparison.OrdinalIgnoreCase) ?? false))
                return true;
        }

        return false;
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
            throw Error(new ArgumentException("Public holiday id must be greater than 0."), "INVALID_ID");
    }

    private static (string Name, DateOnly Date) ValidateItem(
        string? name, DateOnly? date)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 150)
            throw Error(new ArgumentException("Holiday name must contain 1 to 150 characters."), "INVALID_HOLIDAY_NAME");
        if (!date.HasValue || date.Value == default)
            throw Error(new ArgumentException("A valid holiday date is required."), "INVALID_HOLIDAY_DATE");
        return (trimmed, date.Value);
    }

    private async Task<PublicHolidayResponse> Map(PublicHolidayConfig e, CancellationToken ct = default)
    {
        var rule = await _config.Rules.AsNoTracking().Where(r => (r.Status == "ACTIVE" || r.Status == "INACTIVE" || r.Status == "PUBLISH") && r.EffectiveFrom <= e.HolidayDate && (r.EffectiveTo == null || e.HolidayDate < r.EffectiveTo)).OrderByDescending(r => r.EffectiveFrom).FirstOrDefaultAsync(ct);
        return new()
        {
            PublicHolidayId = e.PublicHolidayId,
            HolidayName = e.HolidayName,
            HolidayDate = e.HolidayDate,
            PayMultiplier = rule?.PayMultiplier,
            HolidayPayRuleId = rule?.HolidayPayRuleId,
            IsPayrollLocked = !e.IsActive || await _repo.IsUsedInPayrollAsync(e.PublicHolidayId, ct) || await _config.IsLockedAsync(e.PublicHolidayId, e.HolidayDate, ct),
            IsActive = e.IsActive,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }

    private static object AuditSnapshot(PublicHolidayConfig e) => new
    {
        e.PublicHolidayId,
        e.HolidayName,
        e.HolidayDate,
        e.IsActive
    };

    private async Task<List<PublicHolidayResponse>> MapMany(IEnumerable<PublicHolidayConfig> items, CancellationToken ct)
    {
        var result = new List<PublicHolidayResponse>();
        foreach (var item in items) result.Add(await Map(item, ct));
        return result;
    }
    private static string Json(object value) => JsonSerializer.Serialize(value);

    public async Task<PublicHolidayPageResponse> GetAllAsync(PublicHolidayListRequest request,
        Guid callerId, CancellationToken ct = default)
    {
        if (request == null)
            throw Error(new ArgumentException("Query is required."), "INVALID_REQUEST");
        if (request.PageNumber < 1 || request.PageNumber > int.MaxValue / PageSize)
            throw Error(new ArgumentException("PageNumber is out of range."), "INVALID_PAGE");
        if (request.Year is < 1 or > 9999)
            throw Error(new ArgumentException("Year must be between 1 and 9999."), "INVALID_YEAR");
        if (request.Month is < 1 or > 12)
            throw Error(new ArgumentException("Month must be between 1 and 12."), "INVALID_MONTH");
        if (request.Month.HasValue && !request.Year.HasValue)
            throw Error(new ArgumentException("Year is required when filtering by month."), "YEAR_REQUIRED");
        if (request.Search?.Length > 150)
            throw Error(new ArgumentException("Search cannot exceed 150 characters."), "INVALID_SEARCH");

        var (items, count) = await _repo.GetPagedAsync(request.PageNumber, PageSize,
            request.Year, request.Month, request.Search, request.IncludeInactive, request.IsActive, ct);
        return new PublicHolidayPageResponse
        {
            Items = await MapMany(items, ct),
            PageNumber = request.PageNumber,
            PageSize = PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)PageSize)
        };
    }

    public async Task<PublicHolidayResponse> GetByIdAsync(int id, Guid callerId, CancellationToken ct = default)
    {
        ValidateId(id);
        var holiday = await _repo.GetByIdAsync(id,
            true, ct);
        if (holiday == null)
            throw Error(new KeyNotFoundException("Public holiday not found."), "HOLIDAY_NOT_FOUND");
        return await Map(holiday, ct);
    }

    public async Task<IReadOnlyList<PublicHolidayResponse>> CreateAsync(CreatePublicHolidaysRequest request,
        Guid callerId, CancellationToken ct = default)
    {
        if (request?.Holidays == null || request.Holidays.Count == 0 || request.Holidays.Count > MaxBatchSize)
            throw Error(new ArgumentException("Holidays must contain between 1 and 366 items."), "INVALID_BATCH");

        var normalized = new List<(string Name, DateOnly Date)>();
        foreach (var item in request.Holidays)
        {
            if (item == null)
                throw Error(new ArgumentException("Holiday items cannot be null."), "INVALID_ITEM");
            var validated = ValidateItem(item.HolidayName, item.HolidayDate);
            HolidayWorkflowPolicy.RequireFuture(validated.Date, HolidayPayRuleService.Today);
            normalized.Add(validated);
        }

        var duplicateInRequest = normalized.GroupBy(x => x.Date).Where(g => g.Count() > 1)
            .Select(g => g.Key).ToList();
        if (duplicateInRequest.Count > 0)
            throw Error(new ArgumentException($"Duplicate dates in request: {string.Join(", ", duplicateInRequest)}."), "DUPLICATE_DATES_IN_REQUEST");

        try
        {
            return await _config.TransactionAsync(async () =>
            {
                var dates = normalized.Select(x => x.Date).ToList();
                var existing = await _repo.GetExistingDatesAsync(dates, ct: ct);
                if (existing.Count > 0)
                    throw Error(new InvalidOperationException($"Public holiday dates already exist: {string.Join(", ", existing)}. Existing dates remain reserved, including inactive holidays."), "HOLIDAY_DATE_EXISTS");

                var now = DateTime.UtcNow;
                var entities = normalized.Select((x, index) => new PublicHolidayConfig
                {
                    HolidayName = x.Name,
                    HolidayDate = x.Date,
                    IsActive = true,
                    CreatedBy = callerId == Guid.Empty ? (Guid?)null : callerId,
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList();

                var affected = await _config.InvalidateAsync(entities.Where(e => e.IsActive).Select(e => e.HolidayDate).ToList(), null, callerId, ct);
                await _repo.AddRangeAsync(entities, ct);
                await _repo.SaveChangesAsync(ct);
                foreach (var entity in entities)
                    await WriteAuditIfKnownAsync(callerId, "CREATE_PUBLIC_HOLIDAY", entity.PublicHolidayId,
                        null, Json(AuditSnapshot(entity)), ct);
                var result = await MapMany(entities, ct);
                foreach (var item in result) item.AffectedPayrollPeriodIds = affected;
                return (IReadOnlyList<PublicHolidayResponse>)result;
            }, ct);
        }
        catch (Exception ex) when (IsHolidayDateConflict(ex))
        {
            throw Error(new InvalidOperationException("A public holiday already exists for one of the requested dates."), "HOLIDAY_DATE_EXISTS");
        }
    }

    public async Task<PublicHolidayResponse> UpdateAsync(int id, UpdatePublicHolidayRequest request,
        Guid callerId, CancellationToken ct = default)
    {
        ValidateId(id);
        if (request == null)
            throw Error(new ArgumentException("Request body is required."), "INVALID_REQUEST");
        var data = ValidateItem(request.HolidayName, request.HolidayDate);

        try
        {
            return await _config.TransactionAsync(async () =>
            {
                var holiday = await _repo.GetForUpdateAsync(id, ct);
                if (holiday == null)
                    throw Error(new KeyNotFoundException("Active public holiday not found."), "HOLIDAY_NOT_FOUND");

                HolidayWorkflowPolicy.RequireEditable(holiday.IsActive, await _repo.IsUsedInPayrollAsync(id, ct));
                if (await _config.IsLockedAsync(id, holiday.HolidayDate, ct)) throw HolidayConfigurationRepo.Conflict("This holiday belongs to finalized payroll and is read-only.", "HOLIDAY_READ_ONLY");
                if (holiday.HolidayDate != data.Date) HolidayWorkflowPolicy.RequireFuture(data.Date, HolidayPayRuleService.Today);
                var existing = await _repo.GetExistingDatesAsync(new[] { data.Date }, id, ct);
                if (existing.Count > 0)
                    throw Error(new InvalidOperationException("Another public holiday already exists on this date."), "HOLIDAY_DATE_EXISTS");

                var dateChanged = holiday.HolidayDate != data.Date;

                var nameChanged = holiday.HolidayName != data.Name;


                if (!dateChanged && !nameChanged)
                    return await Map(holiday, ct);

                var affected = dateChanged ? await _config.InvalidateAsync(new[] { holiday.HolidayDate, data.Date }, id, callerId, ct) : new List<int>();
                var oldValue = Json(AuditSnapshot(holiday));
                holiday.HolidayName = data.Name;
                holiday.HolidayDate = data.Date;

                holiday.UpdatedAt = DateTime.UtcNow;
                // CreatedBy and CreatedAt are immutable.
                await _repo.SaveChangesAsync(ct);
                await WriteAuditIfKnownAsync(callerId, "UPDATE_PUBLIC_HOLIDAY", id,
                    oldValue, Json(AuditSnapshot(holiday)), ct);
                var result = await Map(holiday, ct);
                result.AffectedPayrollPeriodIds = affected;
                return result;
            }, ct);
        }
        catch (Exception ex) when (IsHolidayDateConflict(ex))
        {
            throw Error(new InvalidOperationException("Another public holiday already exists on this date."), "HOLIDAY_DATE_EXISTS");
        }
    }

    public Task<PublicHolidayResponse> RestoreAsync(int id, Guid callerId, CancellationToken ct = default) =>
        throw HolidayConfigurationRepo.Conflict("Inactive holidays are read-only and cannot be restored.", "HOLIDAY_RESTORE_DISABLED");

    public async Task DeleteAsync(int id, Guid callerId, CancellationToken ct = default)
    {
        ValidateId(id);
        await _config.TransactionAsync(async () =>
        {
            var holiday = await _repo.GetForUpdateAsync(id, ct);
            if (holiday == null)
                throw Error(new KeyNotFoundException("Public holiday not found."), "HOLIDAY_NOT_FOUND");
            HolidayWorkflowPolicy.RequireEditable(holiday.IsActive, await _repo.IsUsedInPayrollAsync(id, ct));


            var affected = await _config.InvalidateAsync(new[] { holiday.HolidayDate }, id, callerId, ct);
            var oldValue = Json(AuditSnapshot(holiday));
            holiday.IsActive = false;
            holiday.UpdatedAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync(ct);
            await WriteAuditIfKnownAsync(callerId, "DELETE_PUBLIC_HOLIDAY", id,
                oldValue, Json(AuditSnapshot(holiday)), ct);
            return true;
        }, ct);
    }
}




