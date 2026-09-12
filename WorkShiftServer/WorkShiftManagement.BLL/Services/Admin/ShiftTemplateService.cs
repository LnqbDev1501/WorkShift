using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class ShiftTemplateService(IShiftTemplateRepo repository)
{
    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ValidationException("id must be a positive integer.");

        var template = await repository.Query(tracking: true).SingleOrDefaultAsync(
            x => x.ShiftTemplateId == id, cancellationToken);
        if (template is null)
            throw new KeyNotFoundException("Shift template not found.");

        // Repeated restore requests must not change the restoration timestamp.
        if (template.Status == "ACTIVE")
            return false;

        template.Status = "ACTIVE";
        template.UpdatedAt = DateTime.UtcNow;

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            repository.Detach(template);
            throw new KeyNotFoundException("Shift template no longer exists.");
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ValidationException("id must be a positive integer.");

        var template = await repository.Query(tracking: true).SingleOrDefaultAsync(
            x => x.ShiftTemplateId == id, cancellationToken);
        if (template is null)
            throw new KeyNotFoundException("Shift template not found.");

        // Report an already inactive template without changing its deactivation timestamp.
        if (template.Status == "INACTIVE")
            return false;

        template.Status = "INACTIVE";
        template.UpdatedAt = DateTime.UtcNow;

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            repository.Detach(template);
            throw new KeyNotFoundException("Shift template no longer exists.");
        }

        return true;
    }

    public async Task<ShiftTemplateResponse?> UpdateAsync(
        int id, UpdateShiftTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ValidationException("id must be a positive integer.");
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

        var template = await repository.Query(tracking: true).SingleOrDefaultAsync(
            x => x.ShiftTemplateId == id, cancellationToken);
        if (template is null)
            throw new KeyNotFoundException("Shift template not found.");

        var shiftName = request.ShiftName!.Trim();
        if (await repository.Query().AnyAsync(
            x => x.ShiftTemplateId != id && x.ShiftName == shiftName, cancellationToken))
            return null;

        // Existing WorkShifts retain their snapshots, including already scheduled future shifts.
        template.ShiftName = shiftName;
        template.StartTime = TimeOnly.ParseExact(request.StartTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        template.EndTime = TimeOnly.ParseExact(request.EndTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        template.BreakMinutes = request.BreakMinutes!.Value;
        template.Description = request.Description?.Trim();
        template.UpdatedAt = DateTime.UtcNow;

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            repository.Detach(template);
            throw new KeyNotFoundException("Shift template no longer exists.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "shift_templates_shift_name_key"
        })
        {
            repository.Detach(template);
            return null;
        }

        return ToResponse(template);
    }

    public async Task<PagedResponse<ShiftTemplateListResponse>> GetAllAsync(
        ShiftTemplateListQuery query, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(query, new ValidationContext(query), validateAllProperties: true);
        const int pageSize = 20;

        var templates = repository.Query();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Treat LIKE wildcards as literal characters in the search text.
            var search = query.Search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{search}%";
            templates = templates.Where(template =>
                EF.Functions.ILike(template.ShiftName, pattern, "\\") ||
                (template.Description != null && EF.Functions.ILike(template.Description, pattern, "\\")));
        }

        var totalCount = await templates.CountAsync(cancellationToken);
        var items = await templates
            .OrderByDescending(template => template.CreatedAt)
            .ThenBy(template => template.ShiftTemplateId)
            .Skip((query.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(template => new ShiftTemplateListResponse
            {
                ShiftTemplateId = template.ShiftTemplateId,
                ShiftName = template.ShiftName,
                StartTime = template.StartTime,
                EndTime = template.EndTime,
                BreakMinutes = template.BreakMinutes,
                Status = template.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<ShiftTemplateListResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ShiftTemplateResponse?> GetByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ValidationException("id must be a positive integer.");

        return await repository.Query()
            .Where(template => template.ShiftTemplateId == id)
            .Select(template => new ShiftTemplateResponse
            {
                ShiftTemplateId = template.ShiftTemplateId,
                ShiftName = template.ShiftName,
                StartTime = template.StartTime,
                EndTime = template.EndTime,
                BreakMinutes = template.BreakMinutes,
                Description = template.Description,
                Status = template.Status,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ShiftTemplateResponse?> CreateAsync(
        CreateShiftTemplateRequest request, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

        var shiftName = request.ShiftName!.Trim();
        var startTime = TimeOnly.ParseExact(request.StartTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        var endTime = TimeOnly.ParseExact(request.EndTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        var breakMinutes = request.BreakMinutes!.Value;

        if (await repository.Query().AnyAsync(x => x.ShiftName == shiftName, cancellationToken))
            return null;

        var template = new ShiftTemplate
        {
            ShiftName = shiftName,
            StartTime = startTime,
            EndTime = endTime,
            BreakMinutes = breakMinutes,
            Description = request.Description?.Trim(),
            Status = "ACTIVE"
        };

        repository.Add(template);
        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "shift_templates_shift_name_key"
        })
        {
            repository.Detach(template);
            return null;
        }

        return ToResponse(template);
    }

    private static ShiftTemplateResponse ToResponse(ShiftTemplate template)
    {
        return new ShiftTemplateResponse
        {
            ShiftTemplateId = template.ShiftTemplateId,
            ShiftName = template.ShiftName,
            StartTime = template.StartTime,
            EndTime = template.EndTime,
            BreakMinutes = template.BreakMinutes,
            Description = template.Description,
            Status = template.Status,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

}
