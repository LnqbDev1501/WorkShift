using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class ShiftTemplateService(AppDbContext context)
{
    public async Task<PagedResponse<ShiftTemplateResponse>> GetAllAsync(
        ShiftTemplateListQuery query, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(query, new ValidationContext(query), validateAllProperties: true);
        const int pageSize = 20;

        var templates = context.ShiftTemplates.AsNoTracking();
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
            .ToListAsync(cancellationToken);

        return new PagedResponse<ShiftTemplateResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ShiftTemplate?> CreateAsync(
        CreateShiftTemplateRequest request, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);

        var shiftName = request.ShiftName!.Trim();
        var startTime = TimeOnly.ParseExact(request.StartTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        var endTime = TimeOnly.ParseExact(request.EndTime!, "HH:mm:ss", CultureInfo.InvariantCulture);
        var breakMinutes = request.BreakMinutes!.Value;

        if (await context.ShiftTemplates.AnyAsync(x => x.ShiftName == shiftName, cancellationToken))
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

        context.ShiftTemplates.Add(template);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "shift_templates_shift_name_key"
        })
        {
            context.Entry(template).State = EntityState.Detached;
            return null;
        }

        return template;
    }
}
