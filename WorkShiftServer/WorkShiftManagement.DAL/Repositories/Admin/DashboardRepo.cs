using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public sealed class DashboardRepo(AppDbContext context) : IDashboardRepo
{
    public IQueryable<User> Users => context.Users.AsNoTracking();
    public IQueryable<Attendance> Attendances => context.Attendances.AsNoTracking();
    public IQueryable<PayrollPeriod> PayrollPeriods => context.PayrollPeriods.AsNoTracking();
    public IQueryable<Region> Regions => context.Regions.AsNoTracking();
    public IQueryable<Branch> Branches => context.Branches.AsNoTracking();
    public IQueryable<Payroll> Payrolls => context.Payrolls.AsNoTracking();
}
