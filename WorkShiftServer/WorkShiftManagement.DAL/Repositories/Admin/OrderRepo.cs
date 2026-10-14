using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;
using WorkShiftManagement.DAL.Models.Admin;

namespace WorkShiftManagement.DAL.Repositories.Admin;

public class OrderRepo : IOrderRepo
{
    private readonly AppDbContext _context;

    public OrderRepo(AppDbContext context) => _context = context;

    public async Task<(List<OrderData> Items, int TotalCount)> GetPagedAsync(
        string? search,
        int? branchId,
        string? status,
        string? paymentStatus,
        DateTime? dateFrom,
        DateTime? dateTo,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(order =>
                EF.Functions.ILike(order.OrderNumber, pattern) ||
                EF.Functions.ILike(order.BranchNameSnapshot, pattern) ||
                EF.Functions.ILike(order.CreatedByNavigation.FullName, pattern));
        }

        if (branchId.HasValue && branchId.Value > 0)
        {
            query = query.Where(order => order.BranchId == branchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(order => order.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            var normalizedPaymentStatus = paymentStatus.Trim().ToUpperInvariant();
            query = query.Where(order => order.PaymentStatus == normalizedPaymentStatus);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(order => order.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(order => order.OrderDate <= dateTo.Value.Date.AddDays(1).AddTicks(-1));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(order => order.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderData
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                BranchId = order.BranchId,
                BranchName = order.BranchNameSnapshot,
                CreatedBy = order.CreatedBy,
                CreatedByName = order.CreatedByNavigation.FullName,
                OrderDate = order.OrderDate,
                Subtotal = order.Subtotal,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus,
                PaidAt = order.PaidAt,
                Status = order.Status,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems.Select(oi => new OrderItemData
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductNameSnapshot,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    DiscountAmount = oi.DiscountAmount,
                    TotalAmount = oi.TotalAmount,
                    Note = oi.Note
                }).ToList()
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<OrderDetailData?> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderId == id)
            .Select(order => new OrderDetailData
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                BranchId = order.BranchId,
                BranchName = order.BranchNameSnapshot,
                CreatedBy = order.CreatedBy,
                CreatedByName = order.CreatedByNavigation.FullName,
                OrderDate = order.OrderDate,
                Subtotal = order.Subtotal,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus,
                PaidAt = order.PaidAt,
                Status = order.Status,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(oi => new OrderItemData
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductNameSnapshot,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    DiscountAmount = oi.DiscountAmount,
                    TotalAmount = oi.TotalAmount,
                    Note = oi.Note
                }).ToList(),
                Payments = order.Payments.Select(p => new PaymentData
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    Note = p.Note,
                    RecordedBy = p.RecordedBy,
                    RecordedByName = p.RecordedByNavigation.FullName,
                    RecordedAt = p.RecordedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<OrderDetailData?> GetByIdWithBranchAsync(int id, int branchId)
    {
        if (id <= 0)
            return null;

        return await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderId == id && order.BranchId == branchId)
            .Select(order => new OrderDetailData
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                BranchId = order.BranchId,
                BranchName = order.BranchNameSnapshot,
                CreatedBy = order.CreatedBy,
                CreatedByName = order.CreatedByNavigation.FullName,
                OrderDate = order.OrderDate,
                Subtotal = order.Subtotal,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus,
                PaidAt = order.PaidAt,
                Status = order.Status,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(oi => new OrderItemData
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductNameSnapshot,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    DiscountAmount = oi.DiscountAmount,
                    TotalAmount = oi.TotalAmount,
                    Note = oi.Note
                }).ToList(),
                Payments = order.Payments.Select(p => new PaymentData
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    Note = p.Note,
                    RecordedBy = p.RecordedBy,
                    RecordedByName = p.RecordedByNavigation.FullName,
                    RecordedAt = p.RecordedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<OrderDetailData?> GetByOrderNumberAsync(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return null;

        var trimmed = orderNumber.Trim();
        return await _context.Orders
            .AsNoTracking()
            .Where(order => order.OrderNumber == trimmed)
            .Select(order => new OrderDetailData
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                BranchId = order.BranchId,
                BranchName = order.BranchNameSnapshot,
                CreatedBy = order.CreatedBy,
                CreatedByName = order.CreatedByNavigation.FullName,
                OrderDate = order.OrderDate,
                Subtotal = order.Subtotal,
                TotalAmount = order.TotalAmount,
                PaymentStatus = order.PaymentStatus,
                PaidAt = order.PaidAt,
                Status = order.Status,
                Note = order.Note,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                Items = order.OrderItems.Select(oi => new OrderItemData
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductNameSnapshot,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    DiscountAmount = oi.DiscountAmount,
                    TotalAmount = oi.TotalAmount,
                    Note = oi.Note
                }).ToList(),
                Payments = order.Payments.Select(p => new PaymentData
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    Amount = p.Amount,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    Note = p.Note,
                    RecordedBy = p.RecordedBy,
                    RecordedByName = p.RecordedByNavigation.FullName,
                    RecordedAt = p.RecordedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsByOrderNumberAsync(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return false;

        var trimmed = orderNumber.Trim();
        return await _context.Orders
            .AsNoTracking()
            .AnyAsync(order => order.OrderNumber == trimmed);
    }

    public async Task<OrderData> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return new OrderData
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            BranchId = order.BranchId,
            BranchName = order.BranchNameSnapshot,
            CreatedBy = order.CreatedBy,
            CreatedByName = order.CreatedByNavigation.FullName,
            OrderDate = order.OrderDate,
            Subtotal = order.Subtotal,
            TotalAmount = order.TotalAmount,
            PaymentStatus = order.PaymentStatus,
            PaidAt = order.PaidAt,
            Status = order.Status,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemData
            {
                OrderItemId = oi.OrderItemId,
                ProductId = oi.ProductId,
                ProductName = oi.ProductNameSnapshot,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                DiscountAmount = oi.DiscountAmount,
                TotalAmount = oi.TotalAmount,
                Note = oi.Note
            }).ToList()
        };
    }

    public async Task<OrderData?> UpdateAsync(int id, List<OrderItem> items, string? note)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return null;

        // Update note
        order.Note = note;
        order.UpdatedAt = DateTime.UtcNow;

        // Clear existing items and add new ones
        _context.OrderItems.RemoveRange(order.OrderItems);
        order.OrderItems = items;

        // Recalculate totals
        order.Subtotal = items.Sum(i => i.UnitPrice * i.Quantity - i.DiscountAmount);
        order.TotalAmount = order.Subtotal;

        await _context.SaveChangesAsync();

        return new OrderData
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            BranchId = order.BranchId,
            BranchName = order.BranchNameSnapshot,
            CreatedBy = order.CreatedBy,
            CreatedByName = order.CreatedByNavigation.FullName,
            OrderDate = order.OrderDate,
            Subtotal = order.Subtotal,
            TotalAmount = order.TotalAmount,
            PaymentStatus = order.PaymentStatus,
            PaidAt = order.PaidAt,
            Status = order.Status,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemData
            {
                OrderItemId = oi.OrderItemId,
                ProductId = oi.ProductId,
                ProductName = oi.ProductNameSnapshot,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                DiscountAmount = oi.DiscountAmount,
                TotalAmount = oi.TotalAmount,
                Note = oi.Note
            }).ToList()
        };
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return false;

        order.Status = status.Trim().ToUpperInvariant();
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int id, string paymentStatus, DateTime? paidAt)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return false;

        order.PaymentStatus = paymentStatus.Trim().ToUpperInvariant();
        order.PaidAt = paidAt;
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null)
            return false;

        order.Status = "CANCELLED";
        order.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}

public class PaymentRepo : IPaymentRepo
{
    private readonly AppDbContext _context;

    public PaymentRepo(AppDbContext context) => _context = context;

    public async Task<PaymentData> CreateAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return new PaymentData
        {
            PaymentId = payment.PaymentId,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            ReferenceNumber = payment.ReferenceNumber,
            Note = payment.Note,
            RecordedBy = payment.RecordedBy,
            RecordedByName = payment.RecordedByNavigation.FullName,
            RecordedAt = payment.RecordedAt
        };
    }

    public async Task<List<PaymentData>> GetByOrderIdAsync(int orderId)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.RecordedAt)
            .Select(p => new PaymentData
            {
                PaymentId = p.PaymentId,
                OrderId = p.OrderId,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                Note = p.Note,
                RecordedBy = p.RecordedBy,
                RecordedByName = p.RecordedByNavigation.FullName,
                RecordedAt = p.RecordedAt
            })
            .ToListAsync();
    }
}