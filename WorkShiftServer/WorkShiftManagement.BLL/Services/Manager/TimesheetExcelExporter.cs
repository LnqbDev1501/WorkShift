using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        const int totalCols = 21;

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
        string statusText = isLocked ? "[OFFICIAL - FINALIZED TIMESHEET]" : "[DRAFT - REVIEW PERIOD TIMESHEET]";
        worksheet.Cell(5, 1).Value = statusText;
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 1).Style.Font.FontSize = 11;
        worksheet.Cell(5, 1).Style.Font.FontColor = isLocked ? XLColor.FromHtml("#15803D") : XLColor.FromHtml("#B45309");
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

        // Summary columns: 12 to 20
        worksheet.Range(7, 12, 7, 20).Merge().Value = "WEEKLY SUMMARY";
        worksheet.Cell(8, 12).Value = "ASSIGNED";
        worksheet.Cell(8, 13).Value = "COMPLETED";
        worksheet.Cell(8, 14).Value = "WORK (H)";
        worksheet.Cell(8, 15).Value = "LATE\n(TIMES)";
        worksheet.Cell(8, 16).Value = "LATE\n(MIN)";
        worksheet.Cell(8, 17).Value = "EARLY\n(TIMES)";
        worksheet.Cell(8, 18).Value = "EARLY\n(MIN)";
        worksheet.Cell(8, 19).Value = "LEAVE\n(EXCUSED)";
        worksheet.Cell(8, 20).Value = "ABSENT\n(UNEXCUSED)";

        // Column 21: Staff Review
        worksheet.Range(7, 21, 8, 21).Merge().Value = "STAFF\nREVIEW";

        // Style Headers
        var headerRange = worksheet.Range(7, 1, 8, totalCols);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;

        var subSummaryRange = worksheet.Range(8, 12, 8, 20);
        subSummaryRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
        subSummaryRange.Style.Font.FontSize = 9;

        // 3. Data Rows (Starting at Row 9)
        int currentRow = 9;
        int staffIndex = 1;

        foreach (var staff in data.StaffTimesheets.OrderBy(s => s.FullName))
        {
            worksheet.Cell(currentRow, 1).Value = staffIndex++;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string shortId = staff.UserId.ToString().Length >= 8 ? staff.UserId.ToString().Substring(0, 8).ToUpper() : staff.UserId.ToString();
            worksheet.Cell(currentRow, 2).Value = shortId;
            worksheet.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 3).Value = staff.FullName;
            worksheet.Cell(currentRow, 3).Style.Font.Bold = true;

            worksheet.Cell(currentRow, 4).Value = staff.RoleName ?? "Staff";
            worksheet.Cell(currentRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // 7 Days (Mon to Sun)
            for (int d = 0; d < 7; d++)
            {
                var targetDate = data.WeekStartDate.AddDays(d);
                var cell = worksheet.Cell(currentRow, 5 + d);

                var dayShifts = staff.Shifts.Where(s => IsShiftOnDate(s, targetDate, data.WeekStartDate, data.WeekEndDate)).ToList();

                if (!dayShifts.Any())
                {
                    cell.Value = "OFF";
                    cell.Style.Font.FontColor = XLColor.FromHtml("#94A3B8");
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                else
                {
                    var shiftLines = new List<string>();
                    bool hasUnexcusedAbsent = false;
                    bool hasExcusedLeave = false;
                    bool hasLate = false;

                    foreach (var s in dayShifts)
                    {
                        string attStatus = (s.AttendanceStatus ?? "SCHEDULED").ToUpperInvariant();

                        if (s.IsExcusedLeave || attStatus == "LEAVE")
                        {
                            hasExcusedLeave = true;
                            string reason = !string.IsNullOrWhiteSpace(s.LeaveReason) ? $": {s.LeaveReason}" : "";
                            shiftLines.Add($"{s.ShiftName}: [LEAVE (EXCUSED){reason}]");
                        }
                        else if (attStatus == "ABSENT")
                        {
                            hasUnexcusedAbsent = true;
                            shiftLines.Add($"{s.ShiftName}: [ABSENT (UNEXCUSED)]");
                        }
                        else
                        {
                            string inTime = s.CheckInTime.HasValue ? s.CheckInTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
                            string outTime = s.CheckOutTime.HasValue ? s.CheckOutTime.Value.AddHours(7).ToString("HH:mm") : "--:--";
                            decimal workH = Math.Round((decimal)s.ActualWorkingMinutes / 60m, 1);

                            string notes = "";
                            if (s.TotalLateMinutes > 0 && s.TotalEarlyLeaveMinutes > 0)
                            {
                                hasLate = true;
                                notes = $" (Late {s.TotalLateMinutes}m, Early {s.TotalEarlyLeaveMinutes}m)";
                            }
                            else if (s.TotalLateMinutes > 0)
                            {
                                hasLate = true;
                                notes = $" (Late {s.TotalLateMinutes}m)";
                            }
                            else if (s.TotalEarlyLeaveMinutes > 0)
                            {
                                hasLate = true;
                                notes = $" (Early {s.TotalEarlyLeaveMinutes}m)";
                            }

                            shiftLines.Add($"{s.ShiftName}: {inTime}-{outTime} ({workH}h){notes}");
                        }
                    }

                    cell.Value = string.Join("\n", shiftLines);
                    cell.Style.Alignment.WrapText = true;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    if (hasUnexcusedAbsent)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                        cell.Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                    }
                    else if (hasExcusedLeave)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
                        cell.Style.Font.FontColor = XLColor.FromHtml("#1D4ED8");
                    }
                    else if (hasLate)
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                        cell.Style.Font.FontColor = XLColor.FromHtml("#92400E");
                    }
                    else
                    {
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FDF4");
                        cell.Style.Font.FontColor = XLColor.FromHtml("#166534");
                    }
                }
            }

            // Summary columns (12 to 20)
            worksheet.Cell(currentRow, 12).Value = staff.TotalAssignedShifts;
            worksheet.Cell(currentRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 13).Value = staff.CompletedShifts;
            worksheet.Cell(currentRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(currentRow, 14).Value = staff.TotalWorkHours;
            worksheet.Cell(currentRow, 14).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(currentRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell(currentRow, 14).Style.Font.Bold = true;

            // Late count (Times)
            worksheet.Cell(currentRow, 15).Value = staff.LateCount;
            worksheet.Cell(currentRow, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (staff.LateCount > 0)
            {
                worksheet.Cell(currentRow, 15).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                worksheet.Cell(currentRow, 15).Style.Font.Bold = true;
            }

            // Late (Minutes)
            worksheet.Cell(currentRow, 16).Value = staff.TotalLateMinutes;
            worksheet.Cell(currentRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            if (staff.TotalLateMinutes > 0)
            {
                worksheet.Cell(currentRow, 16).Style.Font.FontColor = XLColor.FromHtml("#D97706");
                worksheet.Cell(currentRow, 16).Style.Font.Bold = true;
            }

            // Early Leave count (Times)
            worksheet.Cell(currentRow, 17).Value = staff.EarlyLeaveCount;
            worksheet.Cell(currentRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (staff.EarlyLeaveCount > 0)
            {
                worksheet.Cell(currentRow, 17).Style.Font.FontColor = XLColor.FromHtml("#EA580C");
                worksheet.Cell(currentRow, 17).Style.Font.Bold = true;
            }

            // Early Leave (Minutes)
            worksheet.Cell(currentRow, 18).Value = staff.TotalEarlyLeaveMinutes;
            worksheet.Cell(currentRow, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            if (staff.TotalEarlyLeaveMinutes > 0)
            {
                worksheet.Cell(currentRow, 18).Style.Font.FontColor = XLColor.FromHtml("#EA580C");
            }

            // Excused Leave Count
            worksheet.Cell(currentRow, 19).Value = staff.ExcusedLeaveCount;
            worksheet.Cell(currentRow, 19).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (staff.ExcusedLeaveCount > 0)
            {
                worksheet.Cell(currentRow, 19).Style.Font.FontColor = XLColor.FromHtml("#2563EB");
                worksheet.Cell(currentRow, 19).Style.Font.Bold = true;
            }

            // Unexcused Absent Count
            worksheet.Cell(currentRow, 20).Value = staff.UnexcusedAbsentCount;
            worksheet.Cell(currentRow, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (staff.UnexcusedAbsentCount > 0)
            {
                worksheet.Cell(currentRow, 20).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
                worksheet.Cell(currentRow, 20).Style.Font.Bold = true;
            }

            // Col 21: Staff Review Status
            string reviewStatus = staff.StaffReviewStatus?.ToUpper() ?? "PENDING";
            worksheet.Cell(currentRow, 21).Value = reviewStatus;
            worksheet.Cell(currentRow, 21).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(currentRow, 21).Style.Font.Bold = true;
            if (string.Equals(reviewStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            {
                worksheet.Cell(currentRow, 21).Style.Font.FontColor = XLColor.FromHtml("#16A34A");
            }
            else
            {
                worksheet.Cell(currentRow, 21).Style.Font.FontColor = XLColor.FromHtml("#D97706");
            }

            currentRow++;
        }

        // 4. Grand Total / Summary Row
        int totalRow = currentRow;
        worksheet.Range(totalRow, 1, totalRow, 11).Merge();
        worksheet.Cell(totalRow, 1).Value = "GRAND TOTAL / SUMMARY";
        worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
        worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        worksheet.Row(totalRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        worksheet.Row(totalRow).Style.Font.Bold = true;

        int startDataRow = 9;
        int endDataRow = currentRow - 1;

        if (endDataRow >= startDataRow)
        {
            worksheet.Cell(totalRow, 12).FormulaA1 = $"SUM(L{startDataRow}:L{endDataRow})";
            worksheet.Cell(totalRow, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(totalRow, 13).FormulaA1 = $"SUM(M{startDataRow}:M{endDataRow})";
            worksheet.Cell(totalRow, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(totalRow, 14).FormulaA1 = $"SUM(N{startDataRow}:N{endDataRow})";
            worksheet.Cell(totalRow, 14).Style.NumberFormat.Format = "#,##0.00";
            worksheet.Cell(totalRow, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(totalRow, 15).FormulaA1 = $"SUM(O{startDataRow}:O{endDataRow})";
            worksheet.Cell(totalRow, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(totalRow, 16).FormulaA1 = $"SUM(P{startDataRow}:P{endDataRow})";
            worksheet.Cell(totalRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(totalRow, 17).FormulaA1 = $"SUM(Q{startDataRow}:Q{endDataRow})";
            worksheet.Cell(totalRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(totalRow, 18).FormulaA1 = $"SUM(R{startDataRow}:R{endDataRow})";
            worksheet.Cell(totalRow, 18).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell(totalRow, 19).FormulaA1 = $"SUM(S{startDataRow}:S{endDataRow})";
            worksheet.Cell(totalRow, 19).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell(totalRow, 20).FormulaA1 = $"SUM(T{startDataRow}:T{endDataRow})";
            worksheet.Cell(totalRow, 20).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Apply Borders to Entire Table
        var tableRange = worksheet.Range(7, 1, totalRow, totalCols);
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#CBD5E1");
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#64748B");

        // 5. Signatures Block (Removed CHECKED BY ACCOUNTANT as requested)
        int sigRow = totalRow + 3;

        // Prepared by (Branch Manager - Left side)
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

        // Approved by (Director - Right side)
        worksheet.Range(sigRow, 17, sigRow, 20).Merge();
        worksheet.Cell(sigRow, 17).Value = "APPROVED BY (DIRECTOR)";
        worksheet.Cell(sigRow, 17).Style.Font.Bold = true;
        worksheet.Cell(sigRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(sigRow + 1, 17, sigRow + 1, 20).Merge();
        worksheet.Cell(sigRow + 1, 17).Value = "(Signature & Full Name)";
        worksheet.Cell(sigRow + 1, 17).Style.Font.Italic = true;
        worksheet.Cell(sigRow + 1, 17).Style.Font.FontSize = 9;
        worksheet.Cell(sigRow + 1, 17).Style.Font.FontColor = XLColor.FromHtml("#64748B");
        worksheet.Cell(sigRow + 1, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // 6. Set Column Widths
        worksheet.Column(1).Width = 6;   // No.
        worksheet.Column(2).Width = 12;  // Staff ID
        worksheet.Column(3).Width = 24;  // Full Name
        worksheet.Column(4).Width = 16;  // Role
        for (int c = 5; c <= 11; c++)
        {
            worksheet.Column(c).Width = 26; // 7 days Mon - Sun
        }
        worksheet.Column(12).Width = 12; // Assigned
        worksheet.Column(13).Width = 13; // Completed
        worksheet.Column(14).Width = 14; // Work (H)
        worksheet.Column(15).Width = 12; // Late (Times)
        worksheet.Column(16).Width = 12; // Late (Min)
        worksheet.Column(17).Width = 12; // Early (Times)
        worksheet.Column(18).Width = 12; // Early (Min)
        worksheet.Column(19).Width = 14; // Leave (Excused)
        worksheet.Column(20).Width = 15; // Absent (Unexcused)
        worksheet.Column(21).Width = 15; // Staff Review

        // File name
        string cleanBranch = string.Concat(data.BranchName.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        string cleanPeriod = new string(data.Period.Select(c => Path.GetInvalidFileNameChars().Contains(c) || @"\/?*[]:".Contains(c) ? '-' : c).ToArray()).Replace(" ", "");
        string fileName = $"Timesheet_{cleanBranch}_Week_{cleanPeriod}_{data.WeekStartDate:yyyyMMdd}.xlsx";

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return (memoryStream.ToArray(), fileName);
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
        // Match ONLY by the raw stored DayOfWeek (e.g. Tuesday 13/10 matches Tuesday 20/10)
        // Strictly avoid checking both raw and +7h DayOfWeek so it NEVER duplicates across two days.
        return s.ScheduledStartAt.DayOfWeek == targetDate.DayOfWeek;
    }
}
