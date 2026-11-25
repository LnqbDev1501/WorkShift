using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Interfaces.Admin;

public interface IOrderRepo
{
    Task<(List<OrderData> Items, int TotalCount, int PaidCount, int CompletedCount, decimal TotalOrderValue)> GetPagedAsync(
        string? search,
        int? branchId,
        string? status,
        string? paymentStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber,
        int pageSize,
        string? sortBy = null);

    Task<OrderDetailData?> GetByIdAsync(int id);
    Task<OrderDetailData?> GetByIdWithBranchAsync(int id, int branchId);
    Task<OrderDetailData?> GetByOrderNumberAsync(string orderNumber);
    Task<bool> ExistsByOrderNumberAsync(string orderNumber);
    Task<OrderData> CreateAsync(Order order);
    Task<OrderData?> UpdateAsync(int id, List<OrderItem> items, string? note);
    Task<bool> UpdateStatusAsync(int id, string status);
    Task<bool> UpdatePaymentStatusAsync(int id, string paymentStatus, DateTime? paidAt);
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> IsStaffInWorkingShiftAsync(Guid staffId, int branchId, DateTime currentUtcTime, CancellationToken cancellationToken = default);
    Task<int> AutoCompletePastPendingOrdersAsync(DateTime beforeUtc, CancellationToken cancellationToken = default);
    DateTime GetStartOfTodayVnInUtc();
}

public interface IPaymentRepo
{
    Task<PaymentData> CreateAsync(Payment payment);
    Task<List<PaymentData>> GetByOrderIdAsync(int orderId);
}
