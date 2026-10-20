namespace WorkShiftManagement.DAL.Interfaces.Accountant;
public sealed record PayrollSystemMessage(int PayrollPeriodId, int BranchId, string AudienceRole, Guid? UserId, string Title, string Message);
public sealed class PayrollSystemEvent
{
    public long EventId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Payload { get; set; } = "";
}
public interface IPayrollSystemEventRepo
{
    Task<List<PayrollSystemEvent>> ListAsync(string role, int? branch, Guid user, int page, int size, CancellationToken ct);
    Task<List<PayrollSystemEvent>> PendingAsync(CancellationToken ct);
    Task<bool> IsDeliveredAsync(long eventId, CancellationToken ct);
    void Delivered(long eventId, DateTime now);
    Task<List<int>> ActivePeriodIdsAsync(CancellationToken ct);
    Task<bool> HasSlotAsync(string slot, CancellationToken ct);
    void RecordSlot(string slot, DateTime now);
}
