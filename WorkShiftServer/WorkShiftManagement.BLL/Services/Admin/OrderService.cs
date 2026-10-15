using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.DTOs.Admin.Request;
using WorkShiftManagement.BLL.DTOs.Admin.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.BLL.Services.Admin;

public class OrderService : IOrderService
{
    private readonly IOrderRepo _orderRepo;
    private readonly IPaymentRepo _paymentRepo;
    private readonly AppDbContext _context;

    public OrderService(IOrderRepo orderRepo, IPaymentRepo paymentRepo, AppDbContext context)
    {
        _orderRepo = orderRepo;
        _paymentRepo = paymentRepo;
        _context = context;
    }

    public async Task<PagedResponse<OrderResponse>> GetPagedAsync(OrderListQuery query, Guid actorId)
    {
        if (query == null)
            throw new ArgumentException("Query is required.");

        var actor = await GetActorAsync(actorId);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;
        if (pageSize > 100)
            pageSize = 100;

        string? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            status = query.Status.Trim().ToUpperInvariant();
            if (status != "DRAFT" && status != "COMPLETED" && status != "CANCELLED")
                throw new ArgumentException("Status filter only accepts 'DRAFT', 'COMPLETED', or 'CANCELLED'.");
        }

        string? paymentStatus = null;
        if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
        {
            paymentStatus = query.PaymentStatus.Trim().ToUpperInvariant();
            if (paymentStatus != "PAID" && paymentStatus != "UNPAID" && paymentStatus != "PARTIAL")
                throw new ArgumentException("Payment status filter only accepts 'PAID', 'UNPAID', or 'PARTIAL'.");
        }

        // Managers only see orders of their own branch.
        // Accountants and Admins can filter by branch or see all.
        int? branchId = actor.Role == "MANAGER" ? actor.BranchId : query.BranchId is > 0 ? query.BranchId : null;

        var (items, totalCount) = await _orderRepo.GetPagedAsync(
            query.Search,
            branchId,
            status,
            paymentStatus,
            query.DateFrom,
            query.DateTo,
            pageNumber,
            pageSize);

        return new PagedResponse<OrderResponse>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalCount
        };
    }

    public async Task<OrderDetailResponse?> GetByIdAsync(int id, Guid actorId)
    {
        if (id <= 0)
            return null;

        var actor = await GetActorAsync(actorId);

        var data = actor.Role == "MANAGER" && actor.BranchId.HasValue
            ? await _orderRepo.GetByIdWithBranchAsync(id, actor.BranchId.Value)
            : await _orderRepo.GetByIdAsync(id);

        if (data == null)
            return null;

        if (actor.Role == "MANAGER" && data.BranchId != actor.BranchId)
            return null;

        return MapDetail(data);
    }

    public async Task<OrderDetailResponse> CreateAsync(CreateOrderRequest request, Guid actorId)
    {
        var actor = await GetActorAsync(actorId);
        if (!actor.BranchId.HasValue)
            throw new UnauthorizedAccessException("Your account is not assigned to a branch.");
        var branchId = actor.BranchId.Value;

        if (request == null)
            throw new ArgumentException("Request body cannot be null.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.");

        // Validate all products exist and are active in the branch
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && p.BranchId == branchId && p.Status == "ACTIVE")
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found, inactive, or not in your branch.");

            if (item.Quantity < 1)
                throw new ArgumentException($"Quantity for product {product.ProductName} must be at least 1.");

            if (item.DiscountAmount < 0)
                throw new ArgumentException("Discount amount cannot be negative.");
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(branchId);

        var now = DateTime.UtcNow;
        var order = new Order
        {
            OrderNumber = orderNumber,
            BranchId = branchId,
            BranchNameSnapshot = (await _context.Branches.FindAsync(branchId))?.BranchName ?? string.Empty,
            CreatedBy = actorId,
            OrderDate = now,
            Status = "COMPLETED",
            PaymentStatus = "PAID",
            PaidAt = now,
            Note = request.Note?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create order items
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var unitPrice = product.Price;
            var totalAmount = unitPrice * item.Quantity - item.DiscountAmount;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductNameSnapshot = product.ProductName,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                DiscountAmount = item.DiscountAmount,
                TotalAmount = totalAmount,
                Note = item.Note?.Trim() ?? string.Empty,
                CreatedAt = now
            });
        }

        order.Subtotal = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);
        order.TotalAmount = order.OrderItems.Sum(i => i.TotalAmount);

        var created = await _orderRepo.CreateAsync(order);
        // Fetch the full detail with items
        var detail = await _orderRepo.GetByIdAsync(created.OrderId);
        return MapDetail(detail!);
    }

    public async Task<OrderDetailResponse?> UpdateAsync(int id, UpdateOrderRequest request, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        if (id <= 0)
            throw new ArgumentException("Order id must be greater than 0.");
        if (request == null)
            throw new ArgumentException("Request body cannot be null.");
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.");

        var existing = await _orderRepo.GetByIdWithBranchAsync(id, branchId);
        if (existing == null)
            return null;

        if (existing.Status != "ACTIVE")
            throw new InvalidOperationException("Only active orders can be updated.");

        // Validate all products exist and are active in the branch
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && p.BranchId == branchId && p.Status == "ACTIVE")
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOperationException($"Product with ID {item.ProductId} not found, inactive, or not in your branch.");

            if (item.Quantity < 1)
                throw new ArgumentException($"Quantity for product {product.ProductName} must be at least 1.");

            if (item.DiscountAmount < 0)
                throw new ArgumentException("Discount amount cannot be negative.");
        }

        var orderItems = request.Items.Select(item =>
        {
            var product = products[item.ProductId];
            var unitPrice = product.Price;
            var totalAmount = unitPrice * item.Quantity - item.DiscountAmount;

            return new OrderItem
            {
                OrderItemId = item.OrderItemId ?? 0,
                OrderId = id,
                ProductId = item.ProductId,
                ProductNameSnapshot = product.ProductName,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                DiscountAmount = item.DiscountAmount,
                TotalAmount = totalAmount,
                Note = item.Note?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        var updated = await _orderRepo.UpdateAsync(id, orderItems, request.Note?.Trim());
        if (updated == null)
            return null;
        var detail = await _orderRepo.GetByIdAsync(id);
        return MapDetail(detail!);
    }

    public async Task<bool> UpdateStatusAsync(int id, string status, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);
        var normalizedStatus = status.Trim().ToUpperInvariant();

        if (!new[] { "DRAFT", "COMPLETED", "CANCELLED" }.Contains(normalizedStatus))
            throw new ArgumentException("Invalid status. Allowed: DRAFT, COMPLETED, CANCELLED.");

        var existing = await _orderRepo.GetByIdWithBranchAsync(id, branchId);
        if (existing == null)
            return false;

        if (normalizedStatus == "COMPLETED" && existing.PaymentStatus != "PAID")
            throw new InvalidOperationException("Order must be fully paid before it can be completed.");

        if (normalizedStatus == "CANCELLED" && existing.PaymentStatus == "PAID")
            throw new InvalidOperationException("Cannot cancel a fully paid order. Process a refund instead.");

        return await _orderRepo.UpdateStatusAsync(id, normalizedStatus);
    }

    public async Task<bool> RecordPaymentAsync(int id, RecordPaymentRequest request, Guid actorId)
    {
        var branchId = await RequireManagerBranchAsync(actorId);

        var existing = await _orderRepo.GetByIdWithBranchAsync(id, branchId);
        if (existing == null)
            return false;

        if (existing.PaymentStatus == "PAID")
            throw new InvalidOperationException("Order is already fully paid.");

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than 0.");

        var totalPaid = existing.Payments.Sum(p => p.Amount);
        var remaining = existing.TotalAmount - totalPaid;

        if (request.Amount > remaining)
            throw new InvalidOperationException($"Payment amount exceeds remaining balance. Remaining: {remaining:N0}");

        var payment = new Payment
        {
            OrderId = id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.Trim().ToUpperInvariant(),
            ReferenceNumber = request.ReferenceNumber?.Trim(),
            Note = request.Note?.Trim(),
            RecordedBy = await GetActorIdFromContextAsync(actorId),
            RecordedAt = DateTime.UtcNow
        };

        await _paymentRepo.CreateAsync(payment);

        // Update order payment status
        var newTotalPaid = totalPaid + request.Amount;
        var newPaymentStatus = newTotalPaid >= existing.TotalAmount ? "PAID" : "PARTIAL";
        var paidAt = newPaymentStatus == "PAID" ? DateTime.UtcNow : (DateTime?)null;

        await _orderRepo.UpdatePaymentStatusAsync(id, newPaymentStatus, paidAt);

        return true;
    }

    public async Task<OrderDetailResponse?> GetByOrderNumberAsync(string orderNumber, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return null;

        var actor = await GetActorAsync(actorId);

        var data = actor.Role == "MANAGER" && actor.BranchId.HasValue
            ? await _orderRepo.GetByIdWithBranchAsync(0, actor.BranchId.Value) // We need GetByOrderNumberAsync
            : await _orderRepo.GetByOrderNumberAsync(orderNumber.Trim());

        if (data == null)
            return null;

        if (actor.Role == "MANAGER" && data.BranchId != actor.BranchId)
            return null;

        return MapDetail(data);
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int id, Guid actorId)
    {
        var detail = await GetByIdAsync(id, actorId);
        if (detail == null)
            throw new InvalidOperationException("Order not found.");

        // TODO: Implement PDF generation (using QuestPDF or similar)
        // For now return empty bytes
        return Array.Empty<byte>();
    }

    private async Task<(string Role, int? BranchId)> GetActorAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new UnauthorizedAccessException("The authenticated user ID is invalid.");

        var actor = await _context.Users.AsNoTracking()
            .Where(user => user.UserId == actorId && user.Status == "ACTIVE" && user.DeletedAt == null)
            .Select(user => new { Role = user.Role.RoleName, user.BranchId })
            .SingleOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Your WorkShift account is inactive or unavailable.");

        return (actor.Role.Trim().ToUpperInvariant(), actor.BranchId);
    }

    private async Task<int> RequireManagerBranchAsync(Guid actorId)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.Role != "MANAGER")
            throw new UnauthorizedAccessException("Only branch managers can manage orders.");
        if (!actor.BranchId.HasValue)
            throw new UnauthorizedAccessException("The manager account is not assigned to a branch.");

        return actor.BranchId.Value;
    }

    private async Task<Guid> GetActorIdFromContextAsync(Guid actorId)
    {
        return actorId; // Simplified - in reality might come from HttpContext
    }

    private async Task<string> GenerateOrderNumberAsync(int branchId)
    {
        var branch = await _context.Branches.FindAsync(branchId);
        var prefix = branch?.BranchCode?.ToUpperInvariant() ?? "WS";
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Orders
            .Where(o => o.BranchId == branchId && o.CreatedAt.Date == DateTime.UtcNow.Date)
            .CountAsync();
        var sequence = (count + 1).ToString("D4");
        return $"{prefix}-{datePart}-{sequence}";
    }

    private static OrderResponse Map(OrderData data) => new()
    {
        OrderId = data.OrderId,
        OrderNumber = data.OrderNumber,
        BranchId = data.BranchId,
        BranchName = data.BranchName,
        CreatedBy = data.CreatedBy,
        CreatedByName = data.CreatedByName,
        OrderDate = data.OrderDate,
        Subtotal = data.Subtotal,
        TotalAmount = data.TotalAmount,
        PaymentStatus = data.PaymentStatus,
        PaidAt = data.PaidAt,
        Status = data.Status,
        Note = data.Note,
        CreatedAt = data.CreatedAt,
        Items = data.Items.Select(MapItem).ToList()
    };

    private static OrderDetailResponse MapDetail(OrderDetailData data) => new()
    {
        OrderId = data.OrderId,
        OrderNumber = data.OrderNumber,
        BranchId = data.BranchId,
        BranchName = data.BranchName,
        CreatedBy = data.CreatedBy,
        CreatedByName = data.CreatedByName,
        OrderDate = data.OrderDate,
        Subtotal = data.Subtotal,
        TotalAmount = data.TotalAmount,
        PaymentStatus = data.PaymentStatus,
        PaidAt = data.PaidAt,
        Status = data.Status,
        Note = data.Note,
        CreatedAt = data.CreatedAt,
        UpdatedAt = data.UpdatedAt,
        Items = data.Items.Select(MapItem).ToList(),
        Payments = data.Payments.Select(MapPayment).ToList()
    };

    private static OrderDetailResponse MapDetail(OrderData data) => new()
    {
        OrderId = data.OrderId,
        OrderNumber = data.OrderNumber,
        BranchId = data.BranchId,
        BranchName = data.BranchName,
        CreatedBy = data.CreatedBy,
        CreatedByName = data.CreatedByName,
        OrderDate = data.OrderDate,
        Subtotal = data.Subtotal,
        TotalAmount = data.TotalAmount,
        PaymentStatus = data.PaymentStatus,
        PaidAt = data.PaidAt,
        Status = data.Status,
        Note = data.Note,
        CreatedAt = data.CreatedAt,
        UpdatedAt = data.CreatedAt, // OrderData doesn't have UpdatedAt, use CreatedAt as fallback
        Items = data.Items.Select(MapItem).ToList(),
        Payments = [] // OrderData doesn't include payments
    };

    private static OrderItemResponse MapItem(OrderItemData item) => new()
    {
        OrderItemId = item.OrderItemId,
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        DiscountAmount = item.DiscountAmount,
        TotalAmount = item.TotalAmount,
        Note = item.Note
    };

    private static PaymentResponse MapPayment(PaymentData payment) => new()
    {
        PaymentId = payment.PaymentId,
        OrderId = payment.OrderId,
        Amount = payment.Amount,
        PaymentMethod = payment.PaymentMethod,
        ReferenceNumber = payment.ReferenceNumber,
        Note = payment.Note,
        RecordedBy = payment.RecordedBy,
        RecordedByName = payment.RecordedByName,
        RecordedAt = payment.RecordedAt
    };
}