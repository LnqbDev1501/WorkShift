using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
    private readonly AppDbContext _context;

    public OrderService(IOrderRepo orderRepo, AppDbContext context)
    {
        _orderRepo = orderRepo;
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
            if (status != "PENDING" && status != "COMPLETED" && status != "CANCELLED")
                throw new ArgumentException("Status filter only accepts 'PENDING', 'COMPLETED', or 'CANCELLED'.");
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
            Status = "PENDING",
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

        if (existing.Status != "PENDING")
            throw new InvalidOperationException("Only pending orders can be updated.");

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
        var actor = await GetActorAsync(actorId);
        var branchId = actor.Role == "MANAGER" ? actor.BranchId.Value : (actor.BranchId ?? throw new UnauthorizedAccessException("User not assigned to a branch."));
        var normalizedStatus = status.Trim().ToUpperInvariant();

        if (!new[] { "PENDING", "COMPLETED", "CANCELLED" }.Contains(normalizedStatus))
            throw new ArgumentException("Invalid status. Allowed: PENDING, COMPLETED, CANCELLED.");

        var existing = await _orderRepo.GetByIdWithBranchAsync(id, branchId);
        if (existing == null)
            return false;

        // Staff can complete their own orders
        if (actor.Role == "STAFF" && existing.CreatedBy != actorId)
            throw new UnauthorizedAccessException("You can only complete your own orders.");

        if (normalizedStatus == "COMPLETED" && existing.PaymentStatus != "PAID")
            throw new InvalidOperationException("Order must be fully paid before it can be completed.");

        if (normalizedStatus == "CANCELLED" && existing.Status == "COMPLETED")
            throw new InvalidOperationException("Cannot cancel a completed order.");

        return await _orderRepo.UpdateStatusAsync(id, normalizedStatus);
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

        return BuildInvoicePdf(detail);
    }

    private static byte[] BuildInvoicePdf(OrderDetailResponse order)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(col =>
                {
                    col.Item().Text("HÓA ĐƠN THANH TOÁN").Bold().FontSize(18).FontColor(Colors.Indigo.Darken3).AlignCenter();
                    col.Item().Text($"Mã đơn: {order.OrderNumber}").FontSize(9).FontColor(Colors.Grey.Darken1).AlignCenter();
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(110);
                            cols.RelativeColumn();
                            cols.ConstantColumn(110);
                            cols.RelativeColumn();
                        });

                        AddInfoRow(table, "Chi nhánh:", order.BranchName);
                        AddInfoRow(table, "Ngày:", order.OrderDate.ToString("dd/MM/yyyy HH:mm"));
                        AddInfoRow(table, "Nhân viên:", order.CreatedByName);
                        AddInfoRow(table, "Trạng thái:", order.Status);
                    });

                    col.Item().PaddingTop(16).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn();
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Sản phẩm").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá").Bold();
                            header.Cell().Element(HeaderCell).AlignCenter().Text("SL").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Giảm giá").Bold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền").Bold();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().Element(BodyCell).Text(item.ProductName);
                            table.Cell().Element(BodyCell).AlignRight().Text($"{item.UnitPrice:N0}");
                            table.Cell().Element(BodyCell).AlignCenter().Text(item.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text($"{item.DiscountAmount:N0}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"{item.TotalAmount:N0}");
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Tạm tính: {order.Subtotal:N0} VNĐ").FontSize(10);
                        totals.Item().Text($"Tổng cộng: {order.TotalAmount:N0} VNĐ").Bold().FontSize(13).FontColor(Colors.Indigo.Darken3);
                    });

                    if (!string.IsNullOrWhiteSpace(order.Note))
                    {
                        col.Item().PaddingTop(14).Text("Ghi chú:").Bold().FontSize(10);
                        col.Item().PaddingTop(3).Background(Colors.Grey.Lighten4).Padding(8).Text(order.Note).FontSize(9);
                    }

                    col.Item().PaddingTop(20).Text("Cảm ơn quý khách đã sử dụng dịch vụ!").Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Medium).Padding(5);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

    private static void AddInfoRow(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(2).Text(label).Bold().FontSize(9);
        table.Cell().PaddingVertical(2).Text(value ?? "—").FontSize(9);
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