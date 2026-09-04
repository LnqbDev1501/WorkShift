using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Admin;

public sealed class ShiftTemplateService(AppDbContext context)
{
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
