using WorkShiftManagement.DAL.Entities;
using PayrollEntity = WorkShiftManagement.DAL.Entities.Payroll;

namespace WorkShiftManagement.BLL.Policies.Payroll;

public static class PayrollVersionPolicy
{
    // Review uses the latest candidate. Financial reports use approved/paid versions,
    // so a correction still awaiting approval cannot replace a published result.
    public static List<PayrollEntity> Current(IEnumerable<PayrollEntity> payrolls, string periodStatus)
    {
        var candidates = payrolls.Where(p => p.Status != "REPLACED");
        if (periodStatus is "APPROVED" or "PAID" or "CLOSED")
            candidates = candidates.Where(p => p.Status is "APPROVED" or "PAID");
        return candidates.GroupBy(p => p.UserId)
            .Select(g => g.OrderByDescending(p => p.VersionNo).ThenByDescending(p => p.PayrollId).First())
            .ToList();
    }
}

