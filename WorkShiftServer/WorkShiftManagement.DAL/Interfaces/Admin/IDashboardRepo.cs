using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IDashboardRepo
{
    IQueryable<User> Users { get; }
    IQueryable<Attendance> Attendances { get; }
    IQueryable<PayrollPeriod> PayrollPeriods { get; }
    IQueryable<Region> Regions { get; }
    IQueryable<Branch> Branches { get; }
    IQueryable<Payroll> Payrolls { get; }
}
