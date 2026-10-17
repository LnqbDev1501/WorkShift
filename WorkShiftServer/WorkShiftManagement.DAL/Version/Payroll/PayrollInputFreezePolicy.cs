using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WorkShiftManagement.DAL.Entities;
namespace WorkShiftManagement.DAL.Version.Payroll;
public static class PayrollInputFreezePolicy
{
    public static bool IsFinalApproval(ChangeTracker tracker, EntityEntry entry, int periodId, params string[] allowedFields)
        => entry.State == EntityState.Modified
            && entry.Properties.Where(p => p.IsModified).All(p => allowedFields.Contains(p.Metadata.Name))
            && tracker.Entries<PayrollPeriod>().Any(p => p.Entity.PayrollPeriodId == periodId && p.State == EntityState.Modified
                && p.Property(x => x.Status).OriginalValue == "SUBMITTED" && p.Entity.Status == "APPROVED");
}
