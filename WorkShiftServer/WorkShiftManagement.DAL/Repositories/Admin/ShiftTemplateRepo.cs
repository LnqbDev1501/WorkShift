using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class ShiftTemplateRepo(AppDbContext context) : IShiftTemplateRepo
{
    public IQueryable<ShiftTemplate> Query(bool tracking = false) =>
        tracking ? context.ShiftTemplates.AsTracking() : context.ShiftTemplates.AsNoTracking();

    public void Add(ShiftTemplate template) => context.ShiftTemplates.Add(template);

    public void Detach(ShiftTemplate template) => context.Entry(template).State = EntityState.Detached;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
