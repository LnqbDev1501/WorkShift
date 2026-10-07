using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Accountant;

internal static class PayrollPeriodExcelReport
{
    private static readonly XNamespace Sheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relations = "http://schemas.openxmlformats.org/package/2006/relationships";

    // A real XLSX package using inline strings, so names/notes never become Excel formulas.
    public static byte[] Create(PayrollPeriod p, List<Payroll> payrolls)
    {
        List<object?[]> rows =
        [
            ["Kỳ lương", p.PeriodCode, p.PeriodName], ["Chi nhánh", p.BranchNameSnapshot],
            ["Từ ngày", p.StartDate.ToString("yyyy-MM-dd"), "Đến ngày", p.EndDate.ToString("yyyy-MM-dd")],
            ["Trạng thái", p.Status, "Quyết toán UTC", p.PaidAt?.ToString("O"), "Đóng kỳ UTC", p.ClosedAt?.ToString("O")],
            ["Payroll ID", "Nhân viên", "Version", "Giờ công", "Tiền công", "Thưởng", "Phạt", "Giảm trừ công",
                "Carry Forward", "Tổng giảm trừ thực tế", "Chuyển kỳ mới", "Thực nhận", "Trạng thái", "Thanh toán UTC", "Tham chiếu"]
        ];
        foreach (var w in payrolls.OrderBy(w => w.EmployeeFullNameSnapshot).ThenBy(w => w.PayrollId))
            rows.Add([w.PayrollId, w.EmployeeFullNameSnapshot, w.VersionNo, w.TotalWorkHours, w.WorkPay, w.TotalReward,
                w.TotalPenalty, w.AttendanceDeduction, w.CarryForwardDeduction, w.TotalDeduction,
                w.DeferredDeductionAmount, w.NetSalary, w.Status, w.PaidAt?.ToString("O"), w.PaymentReference]);
        rows.Add(["Tổng", null, null, payrolls.Sum(w => w.TotalWorkHours), payrolls.Sum(w => w.WorkPay),
            payrolls.Sum(w => w.TotalReward), payrolls.Sum(w => w.TotalPenalty), payrolls.Sum(w => w.AttendanceDeduction),
            payrolls.Sum(w => w.CarryForwardDeduction), payrolls.Sum(w => w.TotalDeduction),
            payrolls.Sum(w => w.DeferredDeductionAmount), payrolls.Sum(w => w.NetSalary)]);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            XNamespace types = "http://schemas.openxmlformats.org/package/2006/content-types";
            Write(archive, "[Content_Types].xml", new XElement(types + "Types",
                new XElement(types + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(types + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
                new XElement(types + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(types + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));
            Write(archive, "_rels/.rels", new XElement(Relations + "Relationships", new XElement(Relations + "Relationship",
                new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))));
            XNamespace officeRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            Write(archive, "xl/workbook.xml", new XElement(Sheet + "workbook", new XAttribute(XNamespace.Xmlns + "r", officeRel),
                new XElement(Sheet + "sheets", new XElement(Sheet + "sheet", new XAttribute("name", "Payroll"), new XAttribute("sheetId", 1), new XAttribute(officeRel + "id", "rId1")))));
            Write(archive, "xl/_rels/workbook.xml.rels", new XElement(Relations + "Relationships", new XElement(Relations + "Relationship",
                new XAttribute("Id", "rId1"), new XAttribute("Type", officeRel.NamespaceName + "/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml"))));
            Write(archive, "xl/worksheets/sheet1.xml", new XElement(Sheet + "worksheet",
                new XElement(Sheet + "cols", new XElement(Sheet + "col", new XAttribute("min", 1), new XAttribute("max", 15), new XAttribute("width", 23), new XAttribute("customWidth", 1))),
                new XElement(Sheet + "sheetData", rows.Select((row, index) => new XElement(Sheet + "row", new XAttribute("r", index + 1),
                    row.Select((value, col) => Cell(value, $"{(char)('A' + col)}{index + 1}")))))));
        }
        return stream.ToArray();
    }

    private static XElement Cell(object? value, string reference)
    {
        if (value is decimal or int)
            return new(Sheet + "c", new XAttribute("r", reference), new XElement(Sheet + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
        var text = value?.ToString() ?? string.Empty;
        text = new string(text.Where(ch => ch is '\t' or '\n' or '\r' || ch >= ' ' && ch is not ('\uFFFE' or '\uFFFF')).ToArray());
        return new(Sheet + "c", new XAttribute("r", reference), new XAttribute("t", "inlineStr"),
            new XElement(Sheet + "is", new XElement(Sheet + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), text)));
    }

    private static void Write(ZipArchive archive, string path, XElement content)
    { using var stream = archive.CreateEntry(path).Open(); new XDocument(content).Save(stream); }
}
