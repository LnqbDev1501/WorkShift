using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.DAL.Data;

namespace WorkShiftManagement.BLL.Services.Accountant;

public class BranchSalesReportService : IBranchSalesReportService
{
    private readonly AppDbContext _context;

    public BranchSalesReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateBranchSalesReportAsync(
        int branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BranchId == branchId, cancellationToken)
            ?? throw new ArgumentException("Branch not found.");

        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.BranchId == branchId && o.Status != "CANCELLED");

        if (dateFrom.HasValue)
        {
            var fromUtc = ConvertVnDateToUtcStartOfDay(DateOnly.FromDateTime(dateFrom.Value));
            query = query.Where(o => o.OrderDate >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtcExclusive = ConvertVnDateToUtcStartOfNextDay(DateOnly.FromDateTime(dateTo.Value));
            query = query.Where(o => o.OrderDate < toUtcExclusive);
        }

        var orders = await query
            .OrderBy(o => o.OrderDate)
            .Select(o => new
            {
                o.OrderNumber,
                o.OrderDate,
                o.Status,
                o.TotalAmount,
                StaffName = o.CreatedByNavigation.FullName
            })
            .ToListAsync(cancellationToken);

        var rangeLabel = (dateFrom.HasValue, dateTo.HasValue) switch
        {
            (true, true) => $"From {dateFrom!.Value:dd/MM/yyyy} to {dateTo!.Value:dd/MM/yyyy}",
            (true, false) => $"From {dateFrom!.Value:dd/MM/yyyy}",
            (false, true) => $"To {dateTo!.Value:dd/MM/yyyy}",
            _ => "All time"
        };

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Branch Sales");
        ws.Columns(1, 5).Width = 22;

        var titleCell = ws.Cell(1, 1);
        titleCell.Value = "REVENUE REPORT";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range(1, 1, 1, 5).Merge();

        ws.Cell(2, 1).Value = $"Branch: {branch.BranchName}";
        ws.Cell(2, 1).Style.Font.Bold = true;

        ws.Cell(3, 1).Value = rangeLabel;

        var headerRow = 5;
        string[] headers = ["Order ID", "Date", "Staff", "Status", "Amount (VND)"];
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var row = headerRow + 1;
        foreach (var order in orders)
        {
            ws.Cell(row, 1).Value = order.OrderNumber;
            ws.Cell(row, 2).Value = ToVietnamTime(order.OrderDate).ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 3).Value = order.StaffName;
            ws.Cell(row, 4).Value = order.Status;
            ws.Cell(row, 5).Value = order.TotalAmount;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        var totalRow = row;
        ws.Cell(totalRow, 4).Value = "Total";
        ws.Cell(totalRow, 4).Style.Font.Bold = true;
        ws.Cell(totalRow, 5).Value = orders.Sum(o => o.TotalAmount);
        ws.Cell(totalRow, 5).Style.Font.Bold = true;
        ws.Cell(totalRow, 5).Style.NumberFormat.Format = "#,##0";
        ws.Cell(totalRow, 5).Style.Border.TopBorder = XLBorderStyleValues.Thin;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static TimeZoneInfo VietnamTimeZone { get; } = ResolveVietnamTimeZone();

    private static DateTime ConvertVnDateToUtcStartOfDay(DateOnly date)
    {
        var vnDateTime = date.ToDateTime(TimeOnly.MinValue);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(vnDateTime, DateTimeKind.Unspecified), VietnamTimeZone);
    }

    private static DateTime ConvertVnDateToUtcStartOfNextDay(DateOnly date) =>
        ConvertVnDateToUtcStartOfDay(date.AddDays(1));

    private static DateTime ToVietnamTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, VietnamTimeZone);
    }

    private static TimeZoneInfo ResolveVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
    }
}
