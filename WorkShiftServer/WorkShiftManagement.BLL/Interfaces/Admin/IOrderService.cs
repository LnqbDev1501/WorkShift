using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;

namespace WorkShiftManagement.BLL.Interfaces.Admin;

public interface IOrderService
{
    Task<PagedResponse<OrderResponse>> GetPagedAsync(OrderListQuery query, Guid actorId);
    Task<OrderDetailResponse?> GetByIdAsync(int id, Guid actorId);
    Task<OrderDetailResponse> CreateAsync(CreateOrderRequest request, Guid actorId);
    Task<OrderDetailResponse?> UpdateAsync(int id, UpdateOrderRequest request, Guid actorId);
    Task<bool> UpdateStatusAsync(int id, string status, Guid actorId);
    Task<bool> RecordPaymentAsync(int id, RecordPaymentRequest request, Guid actorId);
    Task<OrderDetailResponse?> GetByOrderNumberAsync(string orderNumber, Guid actorId);
    Task<byte[]> GenerateInvoicePdfAsync(int id, Guid actorId);
}

public interface IPaymentService
{
    Task<PaymentResponse> RecordPaymentAsync(int orderId, RecordPaymentRequest request, Guid actorId);
    Task<List<PaymentResponse>> GetByOrderIdAsync(int orderId, Guid actorId);
}