using ClosedXML.Excel;
using WorkShiftManagement.BLL.DTOs.Manager.Timesheet.Response;

namespace WorkShiftManagement.BLL.Services.Manager;

public static class TimesheetExcelExporter
{
    public static (byte[] FileBytes, string FileName) GenerateWeeklyTimesheetExcel(
        ManagerWeeklyTimesheetDetailResponse data,
        string exportedBy)
    {
        using var workbook = new XLWorkbook();

        // Worksheet name rules: max 31 chars, cannot contain invalid characters
        var safePeriod = new string(data.Period.Select(c => @"\/?*[]:".Contains(c) ? '-' : c).ToArray()).Trim();
        var sheetName = $"Week {safePeriod}".Trim();
        if (sheetName.Length > 31) sheetName = sheetName.Substring(0, 31).Trim();
        if (string.IsNullOrWhiteSpace(sheetName)) sheetName = "Weekly_Timesheet";
        var worksheet = workbook.Worksheets.Add(sheetName);

        // Page setup
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;

        const int totalCols = 22;

        // 1. Title & Header (Rows 1 to 6)
        worksheet.Cell(1, 1).Value = "WORKSHIFT MANAGEMENT SYSTEM";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 10;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");

        worksheet.Cell(2, 1).Value = $"Branch: {data.BranchName}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Font.FontSize = 11;
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#1E293B");

        worksheet.Range(3, 1, 3, totalCols).Merge();
        worksheet.Cell(3, 1).Value = "WEEKLY TIMESHEET SUMMARY REPORT";
        worksheet.Cell(3, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 1).Style.Font.FontSize = 16;
        worksheet.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
        worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(4, 1, 4, totalCols).Merge();
        worksheet.Cell(4, 1).Value = $"Period: Week {data.Period} ({data.WeekStartDate:dd/MM/yyyy} – {data.WeekEndDate:dd/MM/yyyy})";
        worksheet.Cell(4, 1).Style.Font.Italic = true;
        worksheet.Cell(4, 1).Style.Font.FontSize = 11;
        worksheet.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml("#475569");
        worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(5, 1, 5, totalCols).Merge();
        bool isLocked = string.Equals(data.Status, "LOCKED", StringComparison.OrdinalIgnoreCase);
        bool isReviewed = string.Equals(data.Status, "REVIEWED", StringComparison.OrdinalIgnoreCase);

        string statusText;
        XLColor statusColor;
        if (isLocked)
        {
            statusText = "[OFFICIAL - FINALIZED TIMESHEET]";
            statusColor = XLColor.FromHtml("#15803D");
        }
        else if (isReviewed)
        {
            statusText = "[REVIEWED - REVIEWED TIMESHEET]";
            statusColor = XLColor.FromHtml("#1E40AF");
        }
        else
        {
            statusText = "[DRAFT - REVIEW PERIOD TIMESHEET]";
            statusColor = XLColor.FromHtml("#B45309");
        }

        worksheet.Cell(5, 1).Value = statusText;
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 1).Style.Font.FontSize = 11;
        worksheet.Cell(5, 1).Style.Font.FontColor = statusColor;
        worksheet.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(6, 1, 6, totalCols).Merge();
        worksheet.Cell(6, 1).Value = $"Exported At: {DateTime.UtcNow.AddHours(7):HH:mm:ss dd/MM/yyyy} (UTC+7) | Exported By: {exportedBy}";
        worksheet.Cell(6, 1).Style.Font.FontSize = 9;
        worksheet.Cell(6, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        worksheet.Cell(6, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        // 2. Column Headers (Row 7 and Row 8)
        worksheet.Range(7, 1, 8, 1).Merge().Value = "NO.";
        worksheet.Range(7, 2, 8, 2).Merge().Value = "STAFF ID";
        worksheet.Range(7, 3, 8, 3).Merge().Value = "FULL NAME";
        worksheet.Range(7, 4, 8, 4).Merge().Value = "ROLE";

        string[] dayNames = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };
        for (int i = 0; i < 7; i++)
        {
            var dayDate = data.WeekStartDate.AddDays(i);
            worksheet.Range(7, 5 + i, 8, 5 + i).Merge().Value = $"{dayNames[i]}\n{dayDate:dd/MM}";
        }

        // Summary columns: 12 to 21
        worksheet.Range(7, 12, 7, 21).Merge().Value = "WEEKLY SUMMARY";
        worksheet.Cell(8, 12).Value = "ASSIGNED";
        worksheet.Cell(8, 13).Value = "COMPLETED";
        worksheet.Cell(8, 14).Value = "INCOMPLETE";
        worksheet.Cell(8, 15).Value = "WORK (H)";
        worksheet.Cell(8, 16).Value = "LATE\n(TIMES)";
        worksheet.Cell(8, 17).Value = "LATE\n(MIN)";
        worksheet.Cell(8, 18).Value = "EARLY\n(TIMES)";
        worksheet.Cell(8, 19).Value = "EARLY\n(MIN)";
        worksheet.Cell(8, 20).Value = "LEAVE\n(EXCUSED)";
        worksheet.Cell(8, 21).Value = "ABSENT\n(UNEXCUSED)";

        // Column 22: Staff Review
        worksheet.Range(7, 22, 8, 22).Merge().Value = "STAFF\nREVIEW";

        // Style Headers
        var headerRange = worksheet.Range(7, 1, 8, totalCols);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;

        var subSummaryRange = worksheet.Range(8, 12, 8, 21);
        subSummaryRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
        subSummaryRange.Style.Font.FontSize = 9;

        // 3. Data Rows (Starting at Row 9)
        int currentRow = 9;
        int staffIndex = 1;

        foreach (var staff in data.StaffTimesheets.OrderBy(s => s.FullName))
        {
            string shortId = staff.UserId.ToString().Length >= 8 ? staff.UserId.ToString().Substring(0, 8).ToUpper() : staff.UserId.ToString();
            int row = currentRow;

            worksheet.Cell(row, 1).Value = staffIndex++;
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(row, 2).Value = shortId;
            worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(row, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(row, 3).Value = staff.FullName;
            worksheet.Cell(row, 3).Style.Font.Bold = true;
            worksheet.Cell(row, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            worksheet.Cell(row, 4).Value = staff.RoleName ?? "Staff";
            worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(row, 4).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int maxShiftsOnAnyDay = 1;

            for (int d = 0; d < 7; d++)
            {
                var targetDate = data.WeekStartDate.AddDays(d);
                var cell = worksheet.Cell(row, 5 + d);
                var dayShifts = staff.Shifts
                    .Where(sh => IsShiftOnDate(sh, targetDate, data.WeekStartDate, data.WeekEndDate))
                    .OrderBy(sh => sh.ScheduledStartAt)
                    .ToList();

                if (dayShifts.Count > maxShiftsOnAnyDay)
                {
                    maxShiftsOnAnyDay = dayShifts.Count;
                }

                if (dayShifts.Count == 0)
                {
                    cell.Value = "OFF";
                    cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    FormatDayCell(cell, dayShifts);
                }
            }

            SetSummaryColumns(worksheet, row, staff.TotalAssignedShifts, staff.CompletedShifts,
                staff.IncompleteCount, staff.TotalWorkHours, staff.LateCount, staff.TotalLateMinutes,
                staff.EarlyLeaveCount, staff.TotalEarlyLeaveMinutes, staff.ExcusedLeaveCount,
                staff.UnexcusedAbsentCount, staff.StaffReviewStatus, isBold: false);

            worksheet.Row(row).Height = Math.Max(28, maxShiftsOnAnyDay * 18 + 8);
            worksheet.Row(row).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            currentRow++;
        }


        // 4. Grand Total / Summary Row
        int totalRow = currentRow;
        worksheet.Range(totalRow, 1, totalRow, 11).Merge();
        worksheet.Cell(totalRow, 1).Value = "GRAND TOTAL";
        worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
        worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        worksheet.Cell(totalRow, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(totalRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        worksheet.Row(totalRow).Style.Font.Bold = true;
        worksheet.Row(totalRow).Height = 26;
        worksheet.Row(totalRow).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        SetSummaryColumns(worksheet, totalRow,
            data.StaffTimesheets.Sum(s => s.TotalAssignedShifts),
            data.StaffTimesheets.Sum(s => s.CompletedShifts),
            data.StaffTimesheets.Sum(s => s.IncompleteCount),
            data.StaffTimesheets.Sum(s => s.TotalWorkHours),
            data.StaffTimesheets.Sum(s => s.LateCount),
            data.StaffTimesheets.Sum(s => s.TotalLateMinutes),
            data.StaffTimesheets.Sum(s => s.EarlyLeaveCount),
            data.StaffTimesheets.Sum(s => s.TotalEarlyLeaveMinutes),
            data.StaffTimesheets.Sum(s => s.ExcusedLeaveCount),
            data.StaffTimesheets.Sum(s => s.UnexcusedAbsentCount),
            "", isBold: true);

        // Apply Borders to Entire Table
        var tableRange = worksheet.Range(7, 1, totalRow, totalCols);
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#64748B");

        // 5. Signatures Block
        int sigRow = totalRow + 3;

        // Prepared by (Left side)
        worksheet.Range(sigRow, 2, sigRow, 5).Merge();
        worksheet.Cell(sigRow, 2).Value = "PREPARED BY";
        worksheet.Cell(sigRow, 2).Style.Font.Bold = true;
        worksheet.Cell(sigRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(sigRow + 1, 2, sigRow + 1, 5).Merge();
        worksheet.Cell(sigRow + 1, 2).Value = "(Signature & Full Name)";
        worksheet.Cell(sigRow + 1, 2).Style.Font.Italic = true;
        worksheet.Cell(sigRow + 1, 2).Style.Font.FontSize = 9;
        worksheet.Cell(sigRow + 1, 2).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        worksheet.Cell(sigRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(sigRow + 5, 2, sigRow + 5, 5).Merge();
        worksheet.Cell(sigRow + 5, 2).Value = exportedBy;
        worksheet.Cell(sigRow + 5, 2).Style.Font.Bold = true;
        worksheet.Cell(sigRow + 5, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Reviewed by (Branch Manager - Center)
        worksheet.Range(sigRow, 10, sigRow, 14).Merge();
        worksheet.Cell(sigRow, 10).Value = "REVIEWED BY (MANAGER)";
        worksheet.Cell(sigRow, 10).Style.Font.Bold = true;
        worksheet.Cell(sigRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(sigRow + 1, 10, sigRow + 1, 14).Merge();
        worksheet.Cell(sigRow + 1, 10).Value = "(Signature & Full Name)";
        worksheet.Cell(sigRow + 1, 10).Style.Font.Italic = true;
        worksheet.Cell(sigRow + 1, 10).Style.Font.FontSize = 9;
        worksheet.Cell(sigRow + 1, 10).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        worksheet.Cell(sigRow + 1, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (isReviewed || isLocked)
        {
            worksheet.Range(sigRow + 5, 10, sigRow + 5, 14).Merge();
            worksheet.Cell(sigRow + 5, 10).Value = exportedBy;
            worksheet.Cell(sigRow + 5, 10).Style.Font.Bold = true;
            worksheet.Cell(sigRow + 5, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Approved by (Director - Right side) - ONLY FOR LOCKED
        if (isLocked)
        {
            worksheet.Range(sigRow, 18, sigRow, 22).Merge();
            worksheet.Cell(sigRow, 18).Value = "APPROVED BY (DIRECTOR)";
            worksheet.Cell(sigRow, 18).Style.Font.Bold = true;
            worksheet.Cell(sigRow, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Range(sigRow + 1, 18, sigRow + 1, 22).Merge();
            worksheet.Cell(sigRow + 1, 18).Value = "(Signature & Full Name)";
            worksheet.Cell(sigRow + 1, 18).Style.Font.Italic = true;
            worksheet.Cell(sigRow + 1, 18).Style.Font.FontSize = 9;
            worksheet.Cell(sigRow + 1, 18).Style.Font.FontColor = XLColor.FromHtml("#64748B");
            worksheet.Cell(sigRow + 1, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // 6. Set Column Widths
        worksheet.Column(1).Width = 6;   // No.
        worksheet.Column(2).Width = 12;  // Staff ID
        worksheet.Column(3).Width = 24;  // Full Name
        worksheet.Column(4).Width = 16;  // Role
        for (int c = 5; c <= 11; c++)
        {
            worksheet.Column(c).Width = 30; // 7 days Mon - Sun (spacious enough for shift info)
        }
        worksheet.Column(12).Width = 12; // Assigned
        worksheet.Column(13).Width = 13; // Completed
        worksheet.Column(14).Width = 13; // Incomplete
        worksheet.Column(15).Width = 14; // Work (H)
        worksheet.Column(16).Width = 12; // Late (Times)
        worksheet.Column(17).Width = 12; // Late (Min)
        worksheet.Column(18).Width = 12; // Early (Times)
        worksheet.Column(19).Width = 12; // Early (Min)
        worksheet.Column(20).Width = 14; // Leave (Excused)
        worksheet.Column(21).Width = 15; // Absent (Unexcused)
        worksheet.Column(22).Width = 15; // Staff Review

        // File name
        string cleanBranch = string.Concat(data.BranchName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        string cleanPeriod = new string(data.Period.Select(c => Path.GetInvalidFileNameChars().Contains(c) || @"\/?*[]:".Contains(c) ? '-' : c).ToArray()).Replace(" ", "");
        string fileName = $"Timesheet_{cleanBranch}_Week_{cleanPeriod}_{data.WeekStartDate:yyyyMMdd}.xlsx";

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return (memoryStream.ToArray(), fileName);
    }

    private static void FormatDayCell(IXLCell cell, List<ManagerStaffShiftAttendanceDto> shifts)
    {
        var lines = new List<string>();
        bool hasAbsent = false;
        bool hasLeave = false;
        bool hasLateOrEarly = false;

        foreach (var s in shifts)
        {
            string attStatus = (s.AttendanceStatus ?? "SCHEDULED").ToUpperInvariant();

            if (s.IsExcusedLeave || attStatus == "LEAVE")
            {
                hasLeave = true;
                string reason = !string.IsNullOrWhiteSpace(s.LeaveReason) ? $": {s.LeaveReason}" : "";
                lines.Add($"{s.ShiftName}: LEAVE{reason}");
            }
            else if (attStatus == "ABSENT")
            {
                hasAbsent = true;
                lines.Add($"{s.ShiftName}: ABSENT");
            }
            else
            {
                string inTime = s.CheckInTime.HasValue ? s.CheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
                string outTime = s.CheckOutTime.HasValue ? s.CheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
                lines.Add($"{s.ShiftName}: {inTime}-{outTime}");

                if (s.TotalLateMinutes > 0 || s.TotalEarlyLeaveMinutes > 0)
                {
                    hasLateOrEarly = true;
                }
            }
        }

        cell.Value = string.Join("\n", lines);
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // Apply background/font color based on attendance status
        if (hasAbsent)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
            cell.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
        }
        else if (hasLateOrEarly)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
            cell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
        }
        else if (hasLeave)
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
            cell.Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
        }
        else
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FDF4");
            cell.Style.Font.FontColor = XLColor.FromHtml("#166534");
        }
    }

    private static void SetSummaryColumns(IXLWorksheet ws, int row, int assigned, int completed,
        int incomplete, decimal workH, int lateTimes, int lateMin, int earlyTimes, int earlyMin,
        int excusedLeave, int unexcusedAbsent, string reviewStatus, bool isBold)
    {
        ws.Cell(row, 12).Value = assigned;
        ws.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 12).Style.Font.Bold = isBold;

        ws.Cell(row, 13).Value = completed;
        ws.Cell(row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 13).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 13).Style.Font.Bold = isBold;

        ws.Cell(row, 14).Value = incomplete;
        ws.Cell(row, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 14).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 14).Style.Font.Bold = isBold;
        if (incomplete > 0) ws.Cell(row, 14).Style.Font.FontColor = XLColor.FromHtml("#E11D48");

        ws.Cell(row, 15).Value = workH;
        ws.Cell(row, 15).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 15).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 15).Style.Font.Bold = isBold;

        ws.Cell(row, 16).Value = lateTimes;
        ws.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 16).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 16).Style.Font.Bold = isBold;
        if (lateTimes > 0) ws.Cell(row, 16).Style.Font.FontColor = XLColor.FromHtml("#D97706");

        ws.Cell(row, 17).Value = lateMin;
        ws.Cell(row, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 17).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 17).Style.Font.Bold = isBold;
        if (lateMin > 0) ws.Cell(row, 17).Style.Font.FontColor = XLColor.FromHtml("#D97706");

        ws.Cell(row, 18).Value = earlyTimes;
        ws.Cell(row, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 18).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 18).Style.Font.Bold = isBold;
        if (earlyTimes > 0) ws.Cell(row, 18).Style.Font.FontColor = XLColor.FromHtml("#EA580C");

        ws.Cell(row, 19).Value = earlyMin;
        ws.Cell(row, 19).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 19).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 19).Style.Font.Bold = isBold;
        if (earlyMin > 0) ws.Cell(row, 19).Style.Font.FontColor = XLColor.FromHtml("#EA580C");

        ws.Cell(row, 20).Value = excusedLeave;
        ws.Cell(row, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 20).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 20).Style.Font.Bold = isBold;
        if (excusedLeave > 0) ws.Cell(row, 20).Style.Font.FontColor = XLColor.FromHtml("#2563EB");

        ws.Cell(row, 21).Value = unexcusedAbsent;
        ws.Cell(row, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 21).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 21).Style.Font.Bold = isBold;
        if (unexcusedAbsent > 0) ws.Cell(row, 21).Style.Font.FontColor = XLColor.FromHtml("#DC2626");

        string displayStatus = reviewStatus;
        if (string.Equals(reviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
        {
            displayStatus = "CONFIRMED";
            ws.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#16A34A");
        }
        else if (string.Equals(reviewStatus, "ADJUSTMENT_PENDING", StringComparison.OrdinalIgnoreCase))
        {
            displayStatus = "DISPUTED";
            ws.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#7C3AED");
        }
        else if (string.Equals(reviewStatus, "PENDING", StringComparison.OrdinalIgnoreCase))
        {
            displayStatus = "PENDING";
            ws.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#D97706");
        }
        else if (string.Equals(reviewStatus, "NO_RESPONSE", StringComparison.OrdinalIgnoreCase))
        {
            displayStatus = "NO RESPONSE";
            ws.Cell(row, 22).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
        }

        ws.Cell(row, 22).Value = displayStatus;
        ws.Cell(row, 22).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 22).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell(row, 22).Style.Font.Bold = isBold;
    }

    private static bool IsShiftOnDate(ManagerStaffShiftAttendanceDto s, DateOnly targetDate, DateOnly weekStartDate, DateOnly weekEndDate)
    {
        // 1. Direct date match (Vietnam time UTC+7)
        var vnDate = DateOnly.FromDateTime(s.ScheduledStartAt.Kind == DateTimeKind.Utc
            ? s.ScheduledStartAt.AddHours(7)
            : DateTime.SpecifyKind(s.ScheduledStartAt, DateTimeKind.Utc).AddHours(7));
        if (vnDate >= weekStartDate && vnDate <= weekEndDate)
        {
            return vnDate == targetDate;
        }

        // 2. Direct date match (Raw stored date)
        var rawDate = DateOnly.FromDateTime(s.ScheduledStartAt);
        if (rawDate >= weekStartDate && rawDate <= weekEndDate)
        {
            return rawDate == targetDate;
        }

        // 3. Fallback for test shifts whose dates fall outside the schedule week range:
        // Convert to Vietnam time (UTC+7) first so early morning / midnight shifts (e.g. 02:30 AM) don't fall back to previous day UTC
        var vnShiftTime = s.ScheduledStartAt.Kind == DateTimeKind.Utc
            ? s.ScheduledStartAt.AddHours(7)
            : DateTime.SpecifyKind(s.ScheduledStartAt, DateTimeKind.Utc).AddHours(7);

        return vnShiftTime.DayOfWeek == targetDate.DayOfWeek;
    }
}
