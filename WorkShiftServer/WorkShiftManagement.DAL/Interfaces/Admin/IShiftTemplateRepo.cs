using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IShiftTemplateRepo
{
    IQueryable<ShiftTemplate> Query(bool tracking = false);
    void Add(ShiftTemplate template);
    void Detach(ShiftTemplate template);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
