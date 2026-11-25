using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using WorkShiftManagement.DAL.Entities;

namespace WorkShiftManagement.BLL.Services.Accountant;

public static class PayrollPeriodExcelReport
{
    private const string CurrencyFormat = "#,##0";
    private const string DecimalFormat = "0.00";
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";

    public static byte[] Create(PayrollPeriod p, List<Payroll> payrolls, string exportedBy = "System")
    {
        using var workbook = new XLWorkbook();
        workbook.Style.Font.FontName = "Segoe UI";

        var sortedPayrolls = payrolls
            .OrderBy(w => w.EmployeeFullNameSnapshot)
            .ThenBy(w => w.PayrollId)
            .ToList();

        BuildPayrollSummarySheet(workbook, p, sortedPayrolls, exportedBy);
        BuildEmployeeDetailsSheet(workbook, p, sortedPayrolls);
        BuildAdjustmentsSheet(workbook, p, sortedPayrolls);
        BuildPaymentDetailsSheet(workbook, p, sortedPayrolls);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    #region Sheet 1: Payroll Summary

    private static void BuildPayrollSummarySheet(
        IXLWorkbook workbook,
        PayrollPeriod p,
        List<Payroll> payrolls,
        string exportedBy)
    {
        var ws = workbook.Worksheets.Add("Payroll Summary");
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

        const int totalCols = 17;

        // 1. Title & Header Block
        ws.Cell(1, 1).Value = "WORKSHIFT MANAGEMENT SYSTEM";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 10;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

        ws.Cell(2, 1).Value = $"Branch: {Sanitize(p.BranchNameSnapshot)}";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        ws.Range(3, 1, 3, totalCols).Merge();
        ws.Cell(3, 1).Value = "PAYROLL PERIOD SUMMARY REPORT";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontSize = 16;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(4, 1, 4, totalCols).Merge();
        ws.Cell(4, 1).Value = $"Period: {p.PeriodCode} - {Sanitize(p.PeriodName)} ({p.StartDate:dd/MM/yyyy} – {p.EndDate:dd/MM/yyyy})";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontSize = 11;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string statusText;
        XLColor statusColor;
        switch (p.Status)
        {
            case "PAID":
                statusText = "[OFFICIAL - PAID PAYROLL]";
                statusColor = XLColor.FromHtml("#15803D");
                break;
            case "APPROVED":
                statusText = "[APPROVED - READY FOR PAYMENT]";
                statusColor = XLColor.FromHtml("#1E40AF");
                break;
            case "CLOSED":
                statusText = "[CLOSED - FINALIZED PERIOD]";
                statusColor = XLColor.FromHtml("#475569");
                break;
            default:
                statusText = $"[{p.Status}]";
                statusColor = XLColor.FromHtml("#B45309");
                break;
        }

        ws.Range(5, 1, 5, totalCols).Merge();
        ws.Cell(5, 1).Value = statusText;
        ws.Cell(5, 1).Style.Font.Bold = true;
        ws.Cell(5, 1).Style.Font.FontSize = 11;
        ws.Cell(5, 1).Style.Font.FontColor = statusColor;
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(6, 1, 6, totalCols).Merge();
        ws.Cell(6, 1).Value = $"Exported At: {DateTime.UtcNow.AddHours(7):HH:mm:ss dd/MM/yyyy} (UTC+7) | Exported By: {Sanitize(exportedBy)}";
        ws.Cell(6, 1).Style.Font.FontSize = 9;
        ws.Cell(6, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Cell(6, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // 2. Overview Totals KPI Cards (Rows 8 to 11)
        // Row 1 Cards: Total Staff, Total Hours, Total Work Pay, Total Rewards, Gross Salary
        BuildKpiCard(ws, 8, 1, 9, 3, "TOTAL EMPLOYEES", payrolls.Count.ToString(), "#F8FAFC", "#1E293B");
        BuildKpiCard(ws, 8, 4, 9, 6, "TOTAL WORK HOURS", $"{payrolls.Sum(w => w.TotalWorkHours):0.00} hrs", "#F8FAFC", "#1E293B");
        BuildKpiCard(ws, 8, 7, 9, 9, "TOTAL WORK PAY", $"{payrolls.Sum(w => w.WorkPay):#,##0} đ", "#EFF6FF", "#1E40AF");
        BuildKpiCard(ws, 8, 10, 9, 13, "TOTAL REWARDS", $"+{payrolls.Sum(w => w.TotalReward):#,##0} đ", "#F0FDF4", "#15803D");
        BuildKpiCard(ws, 8, 14, 9, 17, "GROSS SALARY", $"{payrolls.Sum(w => w.GrossSalary):#,##0} đ", "#F8FAFC", "#0F172A");

        // Row 2 Cards: Disciplinary Penalties, Attendance Deductions, CF Deducted, Total Deductions, Net Salary
        BuildKpiCard(ws, 10, 1, 11, 3, "DISCIPLINARY PENALTIES", $"-{payrolls.Sum(w => w.TotalPenalty):#,##0} đ", "#FEF2F2", "#B91C1C");
        BuildKpiCard(ws, 10, 4, 11, 6, "ATTENDANCE DEDUCTIONS", $"-{payrolls.Sum(w => w.AttendanceDeduction):#,##0} đ", "#FFF7ED", "#C2410C");
        BuildKpiCard(ws, 10, 7, 11, 9, "CARRY FORWARD DEDUCTED", $"-{payrolls.Sum(w => w.CarryForwardDeduction):#,##0} đ", "#FAF5FF", "#7E22CE");
        BuildKpiCard(ws, 10, 10, 11, 13, "TOTAL DEDUCTIONS", $"-{payrolls.Sum(w => w.TotalDeduction):#,##0} đ", "#FEF2F2", "#DC2626");
        BuildKpiCard(ws, 10, 14, 11, 17, "NET SALARY PAYABLE", $"{payrolls.Sum(w => w.NetSalary):#,##0} đ", "#DCFCE7", "#15803D");

        // Section Title
        ws.Cell(13, 1).Value = "EMPLOYEE PAYROLL SUMMARY";
        ws.Cell(13, 1).Style.Font.Bold = true;
        ws.Cell(13, 1).Style.Font.FontSize = 11;
        ws.Cell(13, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

        // 3. Table Column Headers (Row 14)
        string[] headers =
        [
            "NO.", "STAFF ID", "FULL NAME", "POSITION / ROLE", "HOURLY RATE (VND)",
            "WORK (H)", "WORK PAY (VND)", "REWARDS (VND)", "GROSS SALARY (VND)",
            "PENALTIES (VND)", "ATTENDANCE DED. (VND)", "CF DEDUCTED (VND)",
            "TOTAL DED. (VND)", "DEFERRED DED. (VND)", "NET SALARY (VND)",
            "STATUS", "PAYMENT"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(14, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9.5;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        ws.Row(14).Height = 28;

        // 4. Data Rows (Starting Row 15)
        int currentRow = 15;
        int index = 1;

        foreach (var w in payrolls)
        {
            var position = w.EmployeePositionSnapshot ?? w.User?.EmployeeType?.TypeName ?? "Staff";
            var shortId = FormatShortId(w.UserId);
            var isPaid = w.PaidAt.HasValue;

            ws.Cell(currentRow, 1).Value = index;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 2).Value = shortId;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 3).Value = Sanitize(w.EmployeeFullNameSnapshot);
            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell(currentRow, 4).Value = Sanitize(position);
            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell(currentRow, 5).Value = w.HourlyRateSnapshot;
            ws.Cell(currentRow, 5).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 6).Value = w.TotalWorkHours;
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = DecimalFormat;

            ws.Cell(currentRow, 7).Value = w.WorkPay;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 8).Value = w.TotalReward;
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 9).Value = w.GrossSalary;
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 10).Value = w.TotalPenalty;
            ws.Cell(currentRow, 10).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 11).Value = w.AttendanceDeduction;
            ws.Cell(currentRow, 11).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 12).Value = w.CarryForwardDeduction;
            ws.Cell(currentRow, 12).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 13).Value = w.TotalDeduction;
            ws.Cell(currentRow, 13).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 14).Value = w.DeferredDeductionAmount;
            ws.Cell(currentRow, 14).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(currentRow, 15).Value = w.NetSalary;
            ws.Cell(currentRow, 15).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(currentRow, 15).Style.Font.Bold = true;

            ws.Cell(currentRow, 16).Value = w.Status;
            ws.Cell(currentRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 17).Value = isPaid ? "PAID" : "UNPAID";
            ws.Cell(currentRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 17).Style.Font.Bold = true;
            ws.Cell(currentRow, 17).Style.Font.FontColor = isPaid ? XLColor.FromHtml("#15803D") : XLColor.FromHtml("#B45309");

            var rowRange = ws.Range(currentRow, 1, currentRow, totalCols);
            ApplyThinBorder(rowRange);

            if (index % 2 == 0)
            {
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            currentRow++;
            index++;
        }

        // 5. Grand Total Row
        int totalRow = currentRow;
        int lastDataRow = currentRow - 1;

        if (payrolls.Count > 0)
        {
            ws.Range(totalRow, 1, totalRow, 4).Merge();
            ws.Cell(totalRow, 1).Value = $"GRAND TOTAL ({payrolls.Count} EMPLOYEES)";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 6).FormulaA1 = $"SUM(F15:F{lastDataRow})";
            ws.Cell(totalRow, 6).Style.NumberFormat.Format = DecimalFormat;

            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G15:G{lastDataRow})";
            ws.Cell(totalRow, 7).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 8).FormulaA1 = $"SUM(H15:H{lastDataRow})";
            ws.Cell(totalRow, 8).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 9).FormulaA1 = $"SUM(I15:I{lastDataRow})";
            ws.Cell(totalRow, 9).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 10).FormulaA1 = $"SUM(J15:J{lastDataRow})";
            ws.Cell(totalRow, 10).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 11).FormulaA1 = $"SUM(K15:K{lastDataRow})";
            ws.Cell(totalRow, 11).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 12).FormulaA1 = $"SUM(L15:L{lastDataRow})";
            ws.Cell(totalRow, 12).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 13).FormulaA1 = $"SUM(M15:M{lastDataRow})";
            ws.Cell(totalRow, 13).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 14).FormulaA1 = $"SUM(N15:N{lastDataRow})";
            ws.Cell(totalRow, 14).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 15).FormulaA1 = $"SUM(O15:O{lastDataRow})";
            ws.Cell(totalRow, 15).Style.NumberFormat.Format = CurrencyFormat;

            var grandTotalRange = ws.Range(totalRow, 1, totalRow, totalCols);
            grandTotalRange.Style.Font.Bold = true;
            grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            grandTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            grandTotalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#64748B");
            grandTotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            grandTotalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#1E3A8A");

            ws.Range(14, 1, lastDataRow, totalCols).SetAutoFilter();
        }

        // 6. Column Widths
        int[] widths = [6, 12, 25, 18, 16, 12, 16, 15, 17, 16, 18, 16, 16, 17, 18, 14, 13];
        for (int i = 0; i < widths.Length; i++)
        {
            ws.Column(i + 1).Width = widths[i];
        }

        ws.SheetView.FreezeRows(14);
    }

    #endregion

    #region Sheet 2: Employee Details

    private static void BuildEmployeeDetailsSheet(
        IXLWorkbook workbook,
        PayrollPeriod p,
        List<Payroll> payrolls)
    {
        var ws = workbook.Worksheets.Add("Employee Details");
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

        const int totalCols = 11;

        // 1. Report Header
        ws.Cell(1, 1).Value = "WORKSHIFT MANAGEMENT SYSTEM";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 10;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

        ws.Cell(2, 1).Value = $"Branch: {Sanitize(p.BranchNameSnapshot)}";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        ws.Range(3, 1, 3, totalCols).Merge();
        ws.Cell(3, 1).Value = "EMPLOYEE PAYROLL BREAKDOWN & AUDIT TRAIL";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontSize = 16;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(4, 1, 4, totalCols).Merge();
        ws.Cell(4, 1).Value = $"Period: {p.PeriodCode} - {Sanitize(p.PeriodName)} ({p.StartDate:dd/MM/yyyy} – {p.EndDate:dd/MM/yyyy})";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontSize = 11;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(5, 1, 5, totalCols).Merge();
        ws.Cell(5, 1).Value = "Detailed breakdown lines, hourly rate snapshots, timesheet audit references, and penalty deductions.";
        ws.Cell(5, 1).Style.Font.FontSize = 9.5;
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int currentRow = 7;
        int employeeIndex = 1;

        foreach (var w in payrolls)
        {
            var position = w.EmployeePositionSnapshot ?? w.User?.EmployeeType?.TypeName ?? "Staff";
            var shortId = FormatShortId(w.UserId);

            // A. Employee Banner Row
            ws.Range(currentRow, 1, currentRow, totalCols).Merge();
            ws.Cell(currentRow, 1).Value = $"EMPLOYEE #{employeeIndex}: {Sanitize(w.EmployeeFullNameSnapshot)} | ID: {shortId} | POSITION: {Sanitize(position)} | PAYROLL ID: #{w.PayrollId} (v{w.VersionNo}) | STATUS: {w.Status}";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Font.FontSize = 10.5;
            ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell(currentRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(currentRow).Height = 26;
            currentRow++;

            // B. 5 KPI Summary Cards (2 rows: currentRow to currentRow + 1)
            BuildKpiCard(ws, currentRow, 1, currentRow + 1, 2, "WORK PAY", $"{w.WorkPay:#,##0} đ ({w.TotalWorkHours:0.00}h)", "#EFF6FF", "#1E40AF");
            BuildKpiCard(ws, currentRow, 3, currentRow + 1, 4, "TOTAL REWARDS", $"+{w.TotalReward:#,##0} đ", "#F0FDF4", "#15803D");
            BuildKpiCard(ws, currentRow, 5, currentRow + 1, 6, "GROSS SALARY", $"{w.GrossSalary:#,##0} đ", "#F8FAFC", "#0F172A");
            BuildKpiCard(ws, currentRow, 7, currentRow + 1, 8, "TOTAL DEDUCTIONS", $"-{w.TotalDeduction:#,##0} đ", "#FEF2F2", "#DC2626");
            BuildKpiCard(ws, currentRow, 9, currentRow + 1, 11, "NET SALARY", $"{w.NetSalary:#,##0} đ", "#DCFCE7", "#15803D");
            currentRow += 2;

            // C. Calculation Parameters Snapshot Card (Rows currentRow to currentRow + 1)
            int paramRow = currentRow;
            // Row 1: Header/Labels
            ws.Range(paramRow, 1, paramRow, 3).Merge().Value = "HOURLY RATE & SOURCES";
            ws.Range(paramRow, 4, paramRow, 6).Merge().Value = "DEDUCTIONS BREAKDOWN";
            ws.Range(paramRow, 7, paramRow, 8).Merge().Value = "DEFERRED DEDUCTIONS";
            ws.Range(paramRow, 9, paramRow, 11).Merge().Value = "CALCULATION AUDIT (UTC+7)";

            var labelRange = ws.Range(paramRow, 1, paramRow, totalCols);
            labelRange.Style.Font.Bold = true;
            labelRange.Style.Font.FontSize = 8.5;
            labelRange.Style.Font.FontColor = XLColor.FromHtml("#475569");
            labelRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Row 2: Values
            int valRow = paramRow + 1;
            ws.Range(valRow, 1, valRow, 3).Merge().Value = $"Rate: {w.HourlyRateSnapshot:#,##0} đ/h | Timesheet #{w.TimesheetId} | Salary History #{w.SalaryHistoryId}";
            ws.Range(valRow, 4, valRow, 6).Merge().Value = $"Penalties: -{w.TotalPenalty:#,##0} đ | Attendance: -{w.AttendanceDeduction:#,##0} đ | CF: -{w.CarryForwardDeduction:#,##0} đ";
            ws.Range(valRow, 7, valRow, 8).Merge().Value = $"Deferred to Next Period: {w.DeferredDeductionAmount:#,##0} đ";
            ws.Range(valRow, 9, valRow, 11).Merge().Value = $"Calculated: {FormatVnDateTime(w.CalculatedAt)} | By: {Sanitize(w.CalculatedByNavigation?.FullName ?? "System")}";

            var valRange = ws.Range(valRow, 1, valRow, totalCols);
            valRange.Style.Font.FontSize = 8.5;
            valRange.Style.Font.FontColor = XLColor.FromHtml("#1E293B");
            valRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            valRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var paramBlockRange = ws.Range(paramRow, 1, valRow, totalCols);
            ApplyThinBorder(paramBlockRange);
            currentRow += 2;

            // D. Calculation Breakdown Lines Table Header
            string[] tableHeaders =
            [
                "LINE #", "TYPE", "ITEM CODE", "DESCRIPTION", "WORK DATE(S)",
                "QTY (HRS)", "RATE (VND)", "MULTIPLIER", "FULL AMOUNT (VND)",
                "APPLIED (VND)", "AUDIT SOURCE REF"
            ];

            for (int i = 0; i < tableHeaders.Length; i++)
            {
                var cell = ws.Cell(currentRow, i + 1);
                cell.Value = tableHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 9;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(currentRow).Height = 22;
            int detailHeaderRow = currentRow;
            currentRow++;

            // E. Calculation Breakdown Lines
            var details = w.PayrollDetails
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.PayrollDetailId)
                .ToList();

            if (details.Count == 0)
            {
                ws.Range(currentRow, 1, currentRow, totalCols).Merge();
                ws.Cell(currentRow, 1).Value = "No calculation breakdown details recorded.";
                ws.Cell(currentRow, 1).Style.Font.Italic = true;
                ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ApplyThinBorder(ws.Range(currentRow, 1, currentRow, totalCols));
                currentRow++;
            }
            else
            {
                int lineIndex = 1;
                foreach (var d in details)
                {
                    ws.Cell(currentRow, 1).Value = d.DisplayOrder > 0 ? d.DisplayOrder : lineIndex;
                    ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(currentRow, 2).Value = d.Type;
                    ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 2).Style.Font.Bold = true;

                    ws.Cell(currentRow, 3).Value = Sanitize(d.ItemCode ?? "—");
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var desc = Sanitize(d.Description);
                    if (!string.IsNullOrWhiteSpace(d.Note))
                    {
                        desc += $"\n({Sanitize(d.Note)})";
                    }
                    ws.Cell(currentRow, 4).Value = desc;
                    ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(currentRow, 4).Style.Alignment.WrapText = true;

                    string dateStr;
                    if (d.WorkFrom.HasValue && d.WorkTo.HasValue && d.WorkFrom != d.WorkTo)
                        dateStr = $"{d.WorkFrom.Value:dd/MM/yyyy} - {d.WorkTo.Value:dd/MM/yyyy}";
                    else if (d.WorkFrom.HasValue)
                        dateStr = d.WorkFrom.Value.ToString("dd/MM/yyyy");
                    else
                        dateStr = "—";
                    ws.Cell(currentRow, 5).Value = dateStr;
                    ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (d.Quantity.HasValue)
                    {
                        ws.Cell(currentRow, 6).Value = d.Quantity.Value;
                        ws.Cell(currentRow, 6).Style.NumberFormat.Format = DecimalFormat;
                    }
                    else
                    {
                        ws.Cell(currentRow, 6).Value = "—";
                        ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    if (d.Rate.HasValue)
                    {
                        ws.Cell(currentRow, 7).Value = d.Rate.Value;
                        ws.Cell(currentRow, 7).Style.NumberFormat.Format = CurrencyFormat;
                    }
                    else
                    {
                        ws.Cell(currentRow, 7).Value = "—";
                        ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    ws.Cell(currentRow, 8).Value = d.Multiplier;
                    ws.Cell(currentRow, 8).Style.NumberFormat.Format = DecimalFormat;

                    ws.Cell(currentRow, 9).Value = d.Amount;
                    ws.Cell(currentRow, 9).Style.NumberFormat.Format = CurrencyFormat;

                    if (d.AppliedAmount.HasValue)
                    {
                        ws.Cell(currentRow, 10).Value = d.AppliedAmount.Value;
                        ws.Cell(currentRow, 10).Style.NumberFormat.Format = CurrencyFormat;
                    }
                    else
                    {
                        ws.Cell(currentRow, 10).Value = "—";
                        ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    var sources = new List<string>();
                    if (d.SalaryHistoryId.HasValue) sources.Add($"Salary #{d.SalaryHistoryId}");
                    if (d.PublicHolidayId.HasValue) sources.Add($"Holiday #{d.PublicHolidayId}");
                    if (d.HolidayPayRuleId.HasValue) sources.Add($"Rule #{d.HolidayPayRuleId}");
                    if (d.RewardPenaltyId.HasValue) sources.Add($"Reward/Penalty #{d.RewardPenaltyId}");
                    if (d.AttendancePenaltyId.HasValue) sources.Add($"Attendance #{d.AttendancePenaltyId}");
                    if (d.CarryForwardId.HasValue) sources.Add($"CF #{d.CarryForwardId}");

                    ws.Cell(currentRow, 11).Value = sources.Count > 0 ? string.Join(" | ", sources) : "—";
                    ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(currentRow, 11).Style.Font.FontSize = 8.5;

                    var lineRange = ws.Range(currentRow, 1, currentRow, totalCols);
                    ApplyThinBorder(lineRange);

                    // Type color-coding
                    switch (d.Type)
                    {
                        case "WORK":
                            lineRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F9FF");
                            ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#0369A1");
                            break;
                        case "REWARD":
                            lineRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FDF4");
                            ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                            break;
                        case "PENALTY":
                            lineRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                            ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
                            break;
                        case "ATTENDANCE_DEDUCTION":
                            lineRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
                            ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#C2410C");
                            break;
                        case "CARRY_FORWARD_DEDUCTION":
                        case "CARRY_FORWARD":
                            lineRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FAF5FF");
                            ws.Cell(currentRow, 2).Style.Font.FontColor = XLColor.FromHtml("#7E22CE");
                            break;
                    }

                    currentRow++;
                    lineIndex++;
                }

                // Subtotal Row for Employee
                int detailStartRow = detailHeaderRow + 1;
                int detailEndRow = currentRow - 1;

                ws.Range(currentRow, 1, currentRow, 5).Merge();
                ws.Cell(currentRow, 1).Value = $"SUBTOTAL — {Sanitize(w.EmployeeFullNameSnapshot)}";
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(currentRow, 6).Value = w.TotalWorkHours;
                ws.Cell(currentRow, 6).Style.NumberFormat.Format = DecimalFormat;

                ws.Cell(currentRow, 9).Value = details.Sum(d => d.Amount);
                ws.Cell(currentRow, 9).Style.NumberFormat.Format = CurrencyFormat;

                ws.Cell(currentRow, 10).Value = details.Sum(d => d.AppliedAmount ?? (d.Type == "WORK" || d.Type == "REWARD" ? d.Amount : 0));
                ws.Cell(currentRow, 10).Style.NumberFormat.Format = CurrencyFormat;

                ws.Cell(currentRow, 11).Value = $"Net: {w.NetSalary:#,##0} đ";
                ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var subtotalRange = ws.Range(currentRow, 1, currentRow, totalCols);
                subtotalRange.Style.Font.Bold = true;
                subtotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                subtotalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                subtotalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#94A3B8");
                subtotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
                subtotalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#475569");
                currentRow++;
            }

            // Divider spacer
            currentRow += 2;
            employeeIndex++;
        }

        // Column Widths
        int[] widths = [8, 22, 18, 34, 16, 12, 15, 12, 17, 17, 24];
        for (int i = 0; i < widths.Length; i++)
        {
            ws.Column(i + 1).Width = widths[i];
        }
    }

    #endregion

    #region Sheet 3: Adjustments & Carry Forward

    private static void BuildAdjustmentsSheet(
        IXLWorkbook workbook,
        PayrollPeriod p,
        List<Payroll> payrolls)
    {
        var ws = workbook.Worksheets.Add("Adjustments & Carry Forward");
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

        const int totalCols = 12;

        // 1. Report Header
        ws.Cell(1, 1).Value = "WORKSHIFT MANAGEMENT SYSTEM";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 10;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

        ws.Cell(2, 1).Value = $"Branch: {Sanitize(p.BranchNameSnapshot)}";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        ws.Range(3, 1, 3, totalCols).Merge();
        ws.Cell(3, 1).Value = "PAYROLL ADJUSTMENTS & CARRY FORWARD REGISTER";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontSize = 16;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(4, 1, 4, totalCols).Merge();
        ws.Cell(4, 1).Value = $"Period: {p.PeriodCode} - {Sanitize(p.PeriodName)} ({p.StartDate:dd/MM/yyyy} – {p.EndDate:dd/MM/yyyy})";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontSize = 11;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(5, 1, 5, totalCols).Merge();
        ws.Cell(5, 1).Value = "Complete audit schedule of bonuses, penalties, attendance deductions, and carry forward allocations.";
        ws.Cell(5, 1).Style.Font.FontSize = 9.5;
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // 2. Table Column Headers (Row 7)
        string[] headers =
        [
            "NO.", "STAFF ID", "FULL NAME", "ADJUSTMENT TYPE", "ITEM CODE",
            "DESCRIPTION & REASON", "EFFECTIVE DATE", "ORIGINAL AMOUNT (VND)",
            "APPLIED AMOUNT (VND)", "DEFERRED / EXCESS (VND)", "SOURCE AUDIT REF", "APPLICATION STATUS"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(7, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9.5;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        ws.Row(7).Height = 28;

        // 3. Extract all adjustment lines
        var adjustmentItems = new List<(Payroll Payroll, PayrollDetail Detail)>();
        foreach (var w in payrolls)
        {
            foreach (var d in w.PayrollDetails.Where(d => d.Type != "WORK").OrderBy(d => d.DisplayOrder))
            {
                adjustmentItems.Add((w, d));
            }
        }

        int currentRow = 8;
        if (adjustmentItems.Count == 0)
        {
            ws.Range(currentRow, 1, currentRow, totalCols).Merge();
            ws.Cell(currentRow, 1).Value = "No adjustments or carry forward items recorded in this payroll period.";
            ws.Cell(currentRow, 1).Style.Font.Italic = true;
            ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ApplyThinBorder(ws.Range(currentRow, 1, currentRow, totalCols));
            currentRow++;
        }
        else
        {
            int index = 1;
            foreach (var (w, d) in adjustmentItems)
            {
                var shortId = FormatShortId(w.UserId);
                var applied = d.AppliedAmount ?? (d.Type == "REWARD" ? d.Amount : 0);
                var deferred = d.Type == "REWARD" ? 0 : Math.Max(0, d.Amount - applied);

                string typeDisplay = d.Type switch
                {
                    "REWARD" => "REWARD / BONUS",
                    "PENALTY" => "DISCIPLINARY PENALTY",
                    "ATTENDANCE_DEDUCTION" => "ATTENDANCE DEDUCTION",
                    "CARRY_FORWARD_DEDUCTION" or "CARRY_FORWARD" => "CARRY FORWARD",
                    _ => d.Type
                };

                string effectiveDate = d.WorkFrom.HasValue
                    ? d.WorkFrom.Value.ToString("dd/MM/yyyy")
                    : d.CreatedAt.AddHours(7).ToString("dd/MM/yyyy");

                string sourceRef = "—";
                if (d.RewardPenaltyId.HasValue) sourceRef = $"Reward/Penalty #{d.RewardPenaltyId}";
                else if (d.AttendancePenaltyId.HasValue) sourceRef = $"Attendance Penalty #{d.AttendancePenaltyId}";
                else if (d.CarryForwardId.HasValue) sourceRef = $"Carry Forward #{d.CarryForwardId}";

                string appStatus;
                if (d.Type == "REWARD")
                {
                    appStatus = "Fully Credited";
                }
                else if (applied >= d.Amount)
                {
                    appStatus = "Applied in Full";
                }
                else if (applied > 0)
                {
                    appStatus = $"Partially Applied ({applied:#,##0} đ)";
                }
                else
                {
                    appStatus = "Deferred to Next Period";
                }

                ws.Cell(currentRow, 1).Value = index;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = shortId;
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 3).Value = Sanitize(w.EmployeeFullNameSnapshot);
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                ws.Cell(currentRow, 4).Value = typeDisplay;
                ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 4).Style.Font.Bold = true;

                ws.Cell(currentRow, 5).Value = Sanitize(d.ItemCode ?? "—");
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var desc = Sanitize(d.Description);
                if (!string.IsNullOrWhiteSpace(d.Note)) desc += $" ({Sanitize(d.Note)})";
                ws.Cell(currentRow, 6).Value = desc;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                ws.Cell(currentRow, 7).Value = effectiveDate;
                ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 8).Value = d.Amount;
                ws.Cell(currentRow, 8).Style.NumberFormat.Format = CurrencyFormat;

                ws.Cell(currentRow, 9).Value = applied;
                ws.Cell(currentRow, 9).Style.NumberFormat.Format = CurrencyFormat;

                ws.Cell(currentRow, 10).Value = deferred;
                ws.Cell(currentRow, 10).Style.NumberFormat.Format = CurrencyFormat;

                ws.Cell(currentRow, 11).Value = sourceRef;
                ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 12).Value = appStatus;
                ws.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var rowRange = ws.Range(currentRow, 1, currentRow, totalCols);
                ApplyThinBorder(rowRange);

                // Row color
                switch (d.Type)
                {
                    case "REWARD":
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FDF4");
                        ws.Cell(currentRow, 4).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                        break;
                    case "PENALTY":
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF2F2");
                        ws.Cell(currentRow, 4).Style.Font.FontColor = XLColor.FromHtml("#B91C1C");
                        break;
                    case "ATTENDANCE_DEDUCTION":
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF7ED");
                        ws.Cell(currentRow, 4).Style.Font.FontColor = XLColor.FromHtml("#C2410C");
                        break;
                    case "CARRY_FORWARD_DEDUCTION":
                    case "CARRY_FORWARD":
                        rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#FAF5FF");
                        ws.Cell(currentRow, 4).Style.Font.FontColor = XLColor.FromHtml("#7E22CE");
                        break;
                }

                currentRow++;
                index++;
            }

            // Grand Total Row
            int totalRow = currentRow;
            int lastDataRow = currentRow - 1;

            ws.Range(totalRow, 1, totalRow, 7).Merge();
            ws.Cell(totalRow, 1).Value = $"TOTAL ADJUSTMENTS ({adjustmentItems.Count} ITEMS)";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 8).FormulaA1 = $"SUM(H8:H{lastDataRow})";
            ws.Cell(totalRow, 8).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 9).FormulaA1 = $"SUM(I8:I{lastDataRow})";
            ws.Cell(totalRow, 9).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 10).FormulaA1 = $"SUM(J8:J{lastDataRow})";
            ws.Cell(totalRow, 10).Style.NumberFormat.Format = CurrencyFormat;

            var grandTotalRange = ws.Range(totalRow, 1, totalRow, totalCols);
            grandTotalRange.Style.Font.Bold = true;
            grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            grandTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            grandTotalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#64748B");
            grandTotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            grandTotalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#1E3A8A");

            ws.Range(7, 1, lastDataRow, totalCols).SetAutoFilter();
        }

        // Column Widths
        int[] widths = [6, 12, 24, 22, 18, 32, 16, 18, 18, 18, 22, 18];
        for (int i = 0; i < widths.Length; i++)
        {
            ws.Column(i + 1).Width = widths[i];
        }

        ws.SheetView.FreezeRows(7);
    }

    #endregion

    #region Sheet 4: Payment Details

    private static void BuildPaymentDetailsSheet(
        IXLWorkbook workbook,
        PayrollPeriod p,
        List<Payroll> payrolls)
    {
        var ws = workbook.Worksheets.Add("Payment Details");
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;

        const int totalCols = 11;

        // 1. Report Header
        ws.Cell(1, 1).Value = "WORKSHIFT MANAGEMENT SYSTEM";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 10;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

        ws.Cell(2, 1).Value = $"Branch: {Sanitize(p.BranchNameSnapshot)}";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        ws.Range(3, 1, 3, totalCols).Merge();
        ws.Cell(3, 1).Value = "PAYROLL DISBURSEMENT & PAYMENT SCHEDULE";
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontSize = 16;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(4, 1, 4, totalCols).Merge();
        ws.Cell(4, 1).Value = $"Period: {p.PeriodCode} - {Sanitize(p.PeriodName)} ({p.StartDate:dd/MM/yyyy} – {p.EndDate:dd/MM/yyyy})";
        ws.Cell(4, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Style.Font.FontSize = 11;
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        ws.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(5, 1, 5, totalCols).Merge();
        ws.Cell(5, 1).Value = "Official bank disbursement records, transaction references, settlement timestamps, and authorization signatures.";
        ws.Cell(5, 1).Style.Font.FontSize = 9.5;
        ws.Cell(5, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // 2. Table Column Headers (Row 7)
        string[] headers =
        [
            "NO.", "STAFF ID", "FULL NAME", "POSITION / ROLE", "PAYROLL ID",
            "VERSION", "NET SALARY (VND)", "PAYMENT STATUS", "PAID AT (UTC+7)",
            "PAYMENT REFERENCE", "PROCESSED BY"
        ];

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(7, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 9.5;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
        ws.Row(7).Height = 28;

        // 3. Data Rows (Starting Row 8)
        int currentRow = 8;
        int index = 1;

        foreach (var w in payrolls)
        {
            var position = w.EmployeePositionSnapshot ?? w.User?.EmployeeType?.TypeName ?? "Staff";
            var shortId = FormatShortId(w.UserId);
            var isPaid = w.PaidAt.HasValue;

            ws.Cell(currentRow, 1).Value = index;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 2).Value = shortId;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 3).Value = Sanitize(w.EmployeeFullNameSnapshot);
            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            ws.Cell(currentRow, 3).Style.Font.Bold = true;

            ws.Cell(currentRow, 4).Value = Sanitize(position);
            ws.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell(currentRow, 5).Value = w.PayrollId;
            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 6).Value = $"v{w.VersionNo}";
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 7).Value = w.NetSalary;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = CurrencyFormat;
            ws.Cell(currentRow, 7).Style.Font.Bold = true;

            ws.Cell(currentRow, 8).Value = isPaid ? "PAID" : "UNPAID";
            ws.Cell(currentRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 8).Style.Font.Bold = true;
            if (isPaid)
            {
                ws.Cell(currentRow, 8).Style.Font.FontColor = XLColor.FromHtml("#15803D");
                ws.Cell(currentRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
            }
            else
            {
                ws.Cell(currentRow, 8).Style.Font.FontColor = XLColor.FromHtml("#B45309");
                ws.Cell(currentRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            }

            ws.Cell(currentRow, 9).Value = isPaid ? FormatVnDateTime(w.PaidAt) : "—";
            ws.Cell(currentRow, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 10).Value = Sanitize(w.PaymentReference ?? "—");
            ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var processedBy = w.PaymentConfirmedByNavigation?.FullName
                              ?? (isPaid ? "Admin" : "—");
            ws.Cell(currentRow, 11).Value = Sanitize(processedBy);
            ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            var rowRange = ws.Range(currentRow, 1, currentRow, totalCols);
            ApplyThinBorder(rowRange);

            if (index % 2 == 0 && !isPaid)
            {
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }

            currentRow++;
            index++;
        }

        // 4. Grand Total Row
        int totalRow = currentRow;
        int lastDataRow = currentRow - 1;

        if (payrolls.Count > 0)
        {
            ws.Range(totalRow, 1, totalRow, 6).Merge();
            ws.Cell(totalRow, 1).Value = $"TOTAL DISBURSEMENT ({payrolls.Count} EMPLOYEES)";
            ws.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G8:G{lastDataRow})";
            ws.Cell(totalRow, 7).Style.NumberFormat.Format = CurrencyFormat;

            ws.Cell(totalRow, 8).Value = $"Paid: {payrolls.Count(w => w.PaidAt.HasValue)} / {payrolls.Count}";
            ws.Cell(totalRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(totalRow, 8).Style.Font.Bold = true;

            var grandTotalRange = ws.Range(totalRow, 1, totalRow, totalCols);
            grandTotalRange.Style.Font.Bold = true;
            grandTotalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            grandTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            grandTotalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#64748B");
            grandTotalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            grandTotalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#1E3A8A");

            ws.Range(7, 1, lastDataRow, totalCols).SetAutoFilter();
        }

        // Column Widths
        int[] widths = [6, 12, 26, 18, 13, 10, 18, 16, 20, 22, 20];
        for (int i = 0; i < widths.Length; i++)
        {
            ws.Column(i + 1).Width = widths[i];
        }

        ws.SheetView.FreezeRows(7);
    }

    #endregion

    #region Helper Methods

    private static void BuildKpiCard(
        IXLWorksheet ws,
        int r1, int c1,
        int r2, int c2,
        string label,
        string value,
        string bgHtml,
        string textHtml)
    {
        // Top label cell
        ws.Range(r1, c1, r1, c2).Merge();
        var labelCell = ws.Cell(r1, c1);
        labelCell.Value = label;
        labelCell.Style.Font.Bold = true;
        labelCell.Style.Font.FontSize = 8.5;
        labelCell.Style.Font.FontColor = XLColor.FromHtml("#64748B");
        labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        labelCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Bottom value cell
        ws.Range(r2, c1, r2, c2).Merge();
        var valueCell = ws.Cell(r2, c1);
        valueCell.Value = value;
        valueCell.Style.Font.Bold = true;
        valueCell.Style.Font.FontSize = 12;
        valueCell.Style.Font.FontColor = XLColor.FromHtml(textHtml);
        valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Card Range style
        var cardRange = ws.Range(r1, c1, r2, c2);
        cardRange.Style.Fill.BackgroundColor = XLColor.FromHtml(bgHtml);
        ApplyThinBorder(cardRange);
    }

    private static void ApplyThinBorder(IXLRange range)
    {
        range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        range.Style.Border.TopBorderColor = XLColor.FromHtml("#CBD5E1");
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Border.BottomBorderColor = XLColor.FromHtml("#CBD5E1");
        range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
        range.Style.Border.LeftBorderColor = XLColor.FromHtml("#CBD5E1");
        range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
        range.Style.Border.RightBorderColor = XLColor.FromHtml("#CBD5E1");
    }

    private static string FormatShortId(Guid userId)
    {
        var str = userId.ToString();
        return str.Length >= 8 ? str[..8].ToUpperInvariant() : str;
    }

    private static string FormatVnDateTime(DateTime? dt)
    {
        if (!dt.HasValue) return "—";
        var vn = dt.Value.Kind == DateTimeKind.Utc
            ? dt.Value.AddHours(7)
            : DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc).AddHours(7);
        return vn.ToString(DateTimeFormat);
    }

    private static string Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Trim();
        if (clean.StartsWith('=') || clean.StartsWith('+') || clean.StartsWith('-') || clean.StartsWith('@'))
        {
            return "'" + clean;
        }
        return clean;
    }

    #endregion
}
