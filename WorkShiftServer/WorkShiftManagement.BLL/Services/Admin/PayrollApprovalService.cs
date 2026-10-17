using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using System.Text.Json.Nodes;
using WorkShiftManagement.DAL;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Version.Payroll;
using WorkShiftManagement.DAL.Repositories.Admin;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Request;
using WorkShiftManagement.BLL.DTOs.Admin.PayrollApproval.Response;
using WorkShiftManagement.BLL.Interfaces.Admin;
using WorkShiftManagement.DAL.Interfaces.Admin;

namespace WorkShiftManagement.BLL.Services.Admin
{
    public class PayrollApprovalService
        : IPayrollApprovalService
    {
        private readonly IPayrollApprovalRepo _repo;
        private readonly IPayrollChangeTransaction _holidays;
        private readonly IPayrollFeedbackService _reviews;

        private static readonly HashSet<string>
            AllowedPeriodStatuses =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "DRAFT",
                    "OPEN",
                    "CALCULATED",
                    "SUBMITTED",
                    "REVISION_REQUIRED",
                    "APPROVED",
                    "PAID",
                    "CLOSED"
                };

        public PayrollApprovalService(
            IPayrollApprovalRepo repo, IPayrollChangeTransaction holidays, IPayrollFeedbackService reviews)
        {
            _repo = repo;
            _holidays = holidays;
            _reviews = reviews;
        }

        // =====================================================
        // 1. GET PAYROLL PERIOD LIST
        // =====================================================
        public async Task<PayrollPeriodListResponse>
            GetPayrollPeriodsAsync(
                string? status,
                int? regionId,
                int? branchId,
                string? search,
                int pageNumber,
                int pageSize,
                DateOnly? startDateFrom = null,
                DateOnly? startDateTo = null)
        {
            ValidatePagination(
                pageNumber,
                pageSize);

            if (startDateFrom > startDateTo) throw new ArgumentException("StartDateFrom must not exceed StartDateTo.");
            if (regionId is <= 0 || branchId is <= 0) throw new ArgumentException("RegionId and BranchId must be positive.");
            if (search?.Trim().Length > 200) throw new ArgumentException("Search cannot exceed 200 characters.");

            if (!string.IsNullOrWhiteSpace(status))
            {
                status =
                    status.Trim().ToUpperInvariant();

                if (!AllowedPeriodStatuses.Contains(status))
                {
                    throw new ArgumentException(
                        "Status không hợp lệ cho kỳ lương.");
                }
            }

            var result =
                await _repo.GetPayrollPeriodsAsync(
                    status,
                    regionId,
                    branchId,
                    search,
                    pageNumber,
                    pageSize,
                    startDateFrom,
                    startDateTo);

            var response =
                new PayrollPeriodListResponse
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = result.TotalCount,

                    TotalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize)
                };

            foreach (var period in result.Items)
            {
                var current = PayrollVersionPolicy.Current(period.Payrolls, period.Status);
                var item =
                    new PayrollPeriodListItemResponse
                    {
                        PayrollPeriodId = period.PayrollPeriodId,
                        PeriodCode = period.PeriodCode,
                        PeriodName = period.PeriodName,
                        BranchId = period.BranchId,
                        BranchName = period.BranchNameSnapshot,
                        StartDate = period.StartDate,
                        EndDate = period.EndDate,
                        Status = period.Status,
                        ScheduleWeekId = period.ScheduleWeekId,
                        ApprovalDeadlineAt = period.ApprovalDeadlineAt,
                        PaidAt = period.PaidAt,
                        ClosedAt = period.ClosedAt,
                        TotalPayrolls = current.Count,
                        RejectedPayrollCount = current.Count(x => x.Status == "REJECTED"),
                        TotalNetSalary = current.Sum(x => x.NetSalary),
                        SubmittedAt = period.SubmittedAt,
                        ApprovedAt = period.ApprovedAt
                    };

                if (period.SubmittedBy.HasValue &&
                    period.SubmittedByNavigation != null)
                {
                    item.SubmittedBy =
                        new PayrollActorResponse
                        {
                            UserId = period.SubmittedBy.Value,
                            FullName = period.SubmittedByNavigation.FullName
                        };
                }

                if (period.ApprovedBy.HasValue &&
                    period.ApprovedByNavigation != null)
                {
                    item.ApprovedBy =
                        new PayrollActorResponse
                        {
                            UserId = period.ApprovedBy.Value,
                            FullName = period.ApprovedByNavigation.FullName
                        };
                }

                response.Items.Add(item);
            }

            return response;
        }

        // =====================================================
        // 2. GET PAYROLL PERIOD DETAIL
        // =====================================================
        public async Task<PayrollPeriodDetailResponse>
            GetPayrollPeriodDetailAsync(
                int payrollPeriodId,
                string? search,
                int pageNumber,
                int pageSize)
        {
            ValidatePagination(
                pageNumber,
                pageSize);

            var period =
                await _repo
                    .GetPayrollPeriodDetailAsync(
                        payrollPeriodId);

            if (period == null)
            {
                throw new KeyNotFoundException("Không tìm thấy kỳ lương.");
            }

            var payrollResult =
                await _repo
                    .GetPayrollsByPeriodAsync(
                        payrollPeriodId,
                        search,
                        pageNumber,
                        pageSize);

            var current = PayrollVersionPolicy.Current(period.Payrolls, period.Status);
            var response =
                new PayrollPeriodDetailResponse
                {
                    ScheduleWeekId = period.ScheduleWeekId,
                    ApprovalDeadlineAt = period.ApprovalDeadlineAt,
                    PaidAt = period.PaidAt,
                    ClosedAt = period.ClosedAt,
                    ExpectedEmployeeCount = await _repo.GetExpectedEmployeeCountAsync(payrollPeriodId),
                    LockedTimesheetCount = period.Timesheets.Where(t => t.Status == "LOCKED").Select(t => t.UserId).Distinct().Count(),
                    PayrollPeriodId = period.PayrollPeriodId,
                    PeriodCode = period.PeriodCode,
                    PeriodName = period.PeriodName,
                    BranchId = period.BranchId,
                    BranchName = period.BranchNameSnapshot,
                    StartDate = period.StartDate,
                    EndDate = period.EndDate,
                    Status = period.Status,
                    CalculatedAt = period.CalculatedAt,
                    SubmittedAt = period.SubmittedAt,
                    ApprovedAt = period.ApprovedAt,
                    Summary = new PayrollSummaryResponse
                    {
                        TotalPayrolls = current.Count,
                        RejectedPayrollCount = current.Count(x => x.Status == "REJECTED"),
                        TotalWorkPay = current.Sum(x => x.WorkPay),
                        TotalReward = current.Sum(x => x.TotalReward),
                        TotalPenalty = current.Sum(x => x.TotalPenalty),
                        TotalAttendanceDeduction = current.Sum(x => x.AttendanceDeduction),
                        TotalNetSalary = current.Sum(x => x.NetSalary)
                    },
                    Payrolls = new PayrollPageResponse
                    {
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = payrollResult.TotalCount,
                        TotalPages = (int)Math.Ceiling(payrollResult.TotalCount / (double)pageSize)
                    }
                };

            // Người tính lương
            if (period.CalculatedBy.HasValue &&
                period.CalculatedByNavigation != null)
            {
                response.CalculatedBy =
                    new PayrollActorResponse
                    {
                        UserId = period.CalculatedBy.Value,
                        FullName = period.CalculatedByNavigation.FullName
                    };
            }

            // Người submit
            if (period.SubmittedBy.HasValue &&
                period.SubmittedByNavigation != null)
            {
                response.SubmittedBy =
                    new PayrollActorResponse
                    {
                        UserId = period.SubmittedBy.Value,
                        FullName = period.SubmittedByNavigation.FullName
                    };
            }

            // Admin approve
            if (period.ApprovedBy.HasValue &&
                period.ApprovedByNavigation != null)
            {
                response.ApprovedBy =
                    new PayrollActorResponse
                    {
                        UserId = period.ApprovedBy.Value,
                        FullName = period.ApprovedByNavigation.FullName
                    };
            }

            // Payroll từng Staff
            foreach (var payroll in payrollResult.Items)
            {
                var payrollResponse =
                    new PayrollResponse
                    {
                        PayrollId = payroll.PayrollId,
                        VersionNo = payroll.VersionNo,
                        PreviousPayrollId = payroll.PreviousPayrollId,
                        CorrectionReason = payroll.CorrectionReason,
                        CarryForwardDeduction = payroll.CarryForwardDeduction,
                        DeferredDeductionAmount = payroll.DeferredDeductionAmount,
                        UserId = payroll.UserId,
                        EmployeeFullName = payroll.EmployeeFullNameSnapshot,
                        EmployeePosition = payroll.EmployeePositionSnapshot,
                        HourlyRate = payroll.HourlyRateSnapshot,
                        TotalWorkHours = payroll.TotalWorkHours,
                        WorkPay = payroll.WorkPay,
                        TotalReward = payroll.TotalReward,
                        TotalPenalty = payroll.TotalPenalty,
                        AttendanceDeduction = payroll.AttendanceDeduction,
                        GrossSalary = payroll.GrossSalary,
                        TotalDeduction = payroll.TotalDeduction,
                        NetSalary = payroll.NetSalary,
                        Status = payroll.Status
                    };

                // Payroll Details
                payrollResponse.Details =
                    payroll.PayrollDetails
                        .OrderBy(x =>
                            x.DisplayOrder)
                        .ThenBy(x =>
                            x.PayrollDetailId)
                        .Select(detail =>
                            new PayrollDetailResponse
                            {
                                PayrollDetailId = detail.PayrollDetailId,
                                Type = detail.Type,
                                ItemCode = detail.ItemCode,
                                Description = detail.Description,
                                Multiplier = detail.Multiplier,
                                WorkFrom = detail.WorkFrom,
                                WorkTo = detail.WorkTo,
                                Quantity = detail.Quantity,
                                Rate = detail.Rate,
                                Amount = detail.Amount,
                                AppliedAmount = detail.AppliedAmount, SalaryHistoryId = detail.SalaryHistoryId,
                                RewardPenaltyId = detail.RewardPenaltyId, AttendancePenaltyId = detail.AttendancePenaltyId,
                                CarryForwardId = detail.CarryForwardId, PublicHolidayId = detail.PublicHolidayId,
                                HolidayPayRuleId = detail.HolidayPayRuleId,
                                DisplayOrder = detail.DisplayOrder,
                                Note = detail.Note
                            })
                        .ToList();

                response.Payrolls.Items.Add(payrollResponse);
            }

            return response;
        }

        // =====================================================
        // 3. REJECT ONE PAYROLL
        // =====================================================
        public Task<PayrollRejectResponse>
            RejectPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                RejectPayrollRequest request,
                Guid? adminId)
        { throw new InvalidOperationException("Admin không reject riêng Payroll. Vấn đề được xử lý qua Accountant/Manager trước khi Submit toàn kỳ."); }

        // =====================================================
        // 4. APPROVE WHOLE PAYROLL PERIOD
        // =====================================================
        public async Task<PayrollApprovalResponse>
            ApprovePayrollPeriodAsync(
                int payrollPeriodId,
                Guid? adminId)
        {
            if (!adminId.HasValue) throw new UnauthorizedAccessException("Cần Admin đã đăng nhập.");
            await _reviews.ApprovePeriodAsync(adminId.Value, payrollPeriodId, CancellationToken.None);
            var period = await _repo.GetPayrollPeriodDetailAsync(payrollPeriodId) ?? throw new KeyNotFoundException();
            return new PayrollApprovalResponse { PayrollPeriodId = payrollPeriodId, Status = period.Status, ApprovedAt = period.ApprovedAt!.Value, Message = "Đã duyệt, khóa công và phát phiếu lương chính thức." };
        }

        // =====================================================
        // 5. ACCOUNTANT SUBMIT REJECTED PAYROLL AGAIN
        // =====================================================
        public Task<PayrollSubmitResponse>
            SubmitRejectedPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                Guid accountantId)
        { throw new InvalidOperationException("Luồng mới chỉ Submit toàn kỳ sau khi mọi lượt review/correction hoàn tất."); }

        public async Task<PayrollPeriodSubmitResponse> SubmitPayrollPeriodAsync(
            int payrollPeriodId,
            Guid accountantId)
        {
            await _reviews.SubmitPeriodAsync(accountantId, payrollPeriodId, CancellationToken.None);
            var period = await _repo.GetPayrollPeriodDetailAsync(payrollPeriodId) ?? throw new KeyNotFoundException();
            return new PayrollPeriodSubmitResponse { PayrollPeriodId = payrollPeriodId, Status = period.Status, SubmittedAt = period.SubmittedAt!.Value, SubmittedPayrollCount = WorkShiftManagement.DAL.Version.Payroll.PayrollVersionPolicy.Current(period.Payrolls, period.Status).Count, Message = "Đã gửi toàn kỳ cho Admin duyệt." };
        }

        public static void ValidateTotals(Payroll after)
        {
            var d = after.PayrollDetails;
            if (d.Any(x => x.Amount < 0 || x.AppliedAmount < 0 || x.AppliedAmount > x.Amount
                    || x.Type is "WORK" or "REWARD" && x.AppliedAmount != null
                    || x.Type is "PENALTY" or "ATTENDANCE_DEDUCTION" or "CARRY_FORWARD_DEDUCTION" && x.AppliedAmount == null
                    || x.Type is not ("WORK" or "REWARD" or "PENALTY" or "ATTENDANCE_DEDUCTION" or "CARRY_FORWARD_DEDUCTION"))
                || after.WorkPay != d.Where(x => x.Type == "WORK").Sum(x => x.Amount)
                || after.TotalReward != d.Where(x => x.Type == "REWARD").Sum(x => x.Amount)
                || after.TotalPenalty != d.Where(x => x.Type == "PENALTY").Sum(x => x.Amount)
                || after.AttendanceDeduction != d.Where(x => x.Type == "ATTENDANCE_DEDUCTION").Sum(x => x.Amount)
                || after.GrossSalary != after.WorkPay + after.TotalReward
                || after.TotalDeduction != d.Sum(x => x.AppliedAmount ?? 0)
                || after.NetSalary != after.GrossSalary - after.TotalDeduction || after.NetSalary < 0
                || after.CarryForwardDeduction != d.Where(x => x.Type == "CARRY_FORWARD_DEDUCTION").Sum(x => x.AppliedAmount ?? 0)
                || after.DeferredDeductionAmount != d.Where(x => x.Type is "PENALTY" or "ATTENDANCE_DEDUCTION").Sum(x => x.Amount - (x.AppliedAmount ?? 0)))
                throw new InvalidOperationException("Correction totals do not match calculation details.");
        }

        public static bool HasCalculationChange(Payroll before, Payroll after)
        {
            if (before.SalaryHistoryId != after.SalaryHistoryId || before.HourlyRateSnapshot != after.HourlyRateSnapshot
                || before.WorkPay != after.WorkPay || before.TotalReward != after.TotalReward || before.TotalPenalty != after.TotalPenalty
                || before.AttendanceDeduction != after.AttendanceDeduction || before.GrossSalary != after.GrossSalary
                || before.TotalDeduction != after.TotalDeduction || before.NetSalary != after.NetSalary
                || before.CarryForwardDeduction != after.CarryForwardDeduction || before.DeferredDeductionAmount != after.DeferredDeductionAmount)
                return true;
            static string Signature(PayrollDetail d) => System.Text.Json.JsonSerializer.Serialize(new
            {
                d.Type,
                d.Quantity,
                d.Rate,
                d.Multiplier,
                d.Amount,
                d.AppliedAmount,
                d.WorkFrom,
                d.WorkTo,
                d.SalaryHistoryId,
                d.PublicHolidayId,
                d.HolidayPayRuleId,
                d.RewardPenaltyId,
                d.AttendancePenaltyId,
                d.CarryForwardId
            });
            return !before.PayrollDetails.Select(Signature).OrderBy(s => s, StringComparer.Ordinal)
                .SequenceEqual(after.PayrollDetails.Select(Signature).OrderBy(s => s, StringComparer.Ordinal));
        }

        // =====================================================
        // PAGINATION VALIDATION
        // =====================================================
        private static void ValidatePagination(
            int pageNumber,
            int pageSize)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentException("pageNumber phải lớn hơn hoặc bằng 1.");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentException("pageSize phải từ 1 đến 100.");
            }

            if ((long)(pageNumber - 1) * pageSize > int.MaxValue)
                throw new ArgumentException("PageNumber is out of range.");
        }
    }
}

