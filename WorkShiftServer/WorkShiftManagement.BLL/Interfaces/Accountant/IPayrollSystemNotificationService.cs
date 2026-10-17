using WorkShiftManagement.DAL.Interfaces.Accountant;
namespace WorkShiftManagement.BLL.Interfaces.Accountant;
public interface IPayrollSystemNotificationService
{
    Task<List<PayrollSystemEvent>> ListAsync(Guid actor, int page, int pageSize, CancellationToken ct);
    Task QueueRemindersAsync(CancellationToken ct);
}
