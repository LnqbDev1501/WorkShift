using Microsoft.EntityFrameworkCore;
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
        private async Task ValidateHolidaySnapshotsAsync(int periodId)
        {
            // Validate the saved calculation, without re-resolving live holiday configuration.
            var current = await _repo.GetAllPayrollsForUpdateAsync(periodId);
            foreach (var payroll in current)
            {
                ValidateTotals(payroll);
                if (payroll.Timesheet.Status != "LOCKED" || payroll.Timesheet.UserId != payroll.UserId
                    || payroll.Timesheet.PayrollPeriodId != periodId)
                    throw new InvalidOperationException("Payroll must reference its employee's LOCKED timesheet in this period.");
                try
                {
                    if (JsonNode.Parse(payroll.WorkInputSnapshot ?? "null") is not JsonObject)
                        throw new InvalidOperationException("Payroll must contain its frozen work input snapshot.");
                }
                catch (System.Text.Json.JsonException ex)
                { throw new InvalidOperationException("Payroll work snapshot is invalid.", ex); }
                foreach (var detail in payroll.PayrollDetails.Where(d => d.Type == "WORK"))
                    if (detail.WorkFrom == null || detail.WorkTo == null || detail.WorkFrom > detail.WorkTo
                        || detail.Quantity is null or < 0 || detail.Rate is null or < 0 || detail.Multiplier <= 0)
                        throw new InvalidOperationException("Work calculation details must contain dates, quantity, rate and multiplier.");
            }
        }
        private readonly IPayrollApprovalRepo _repo;
        private readonly IPayrollChangeTransaction _holidays;

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
            IPayrollApprovalRepo repo, IPayrollChangeTransaction holidays)
        {
            _repo = repo;
            _holidays = holidays;
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

                // Nếu Payroll bị reject
                if (payroll.Status == "REJECTED")
                {
                    payrollResponse.Rejection =
                        new PayrollRejectionResponse
                        {
                            RejectedById = payroll.RejectedBy,
                            RejectedByName = payroll.RejectedByNavigation?.FullName,
                            RejectedAt = payroll.RejectedAt,
                            Reason = payroll.RejectReason,
                            Category = payroll.RejectionCategory
                        };
                }

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
        public async Task<PayrollRejectResponse>
            RejectPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                RejectPayrollRequest request,
                Guid? adminId)
        {
            if (request == null)
            {
                throw new ArgumentException("Dữ liệu yêu cầu không hợp lệ.");
            }

            var reason = request.Reason?.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Lý do yêu cầu chỉnh sửa là bắt buộc.");
            }

            if (reason.Length > 1000)
            {
                throw new ArgumentException("Lý do không được vượt quá 1000 ký tự.");
            }

            var resolvedAdminId = await ResolveAdminIdAsync(adminId);
            if (request.RejectionCategory is not ("CALCULATION" or "RATE" or "REWARD" or "DEDUCTION" or "OTHER"))
                throw new ArgumentException("A valid rejection category is required.");
            await using var holidayTransaction = await _holidays.BeginPayrollChangeAsync();

            var period =
                await _repo
                    .GetPayrollPeriodForUpdateAsync(
                        payrollPeriodId);

            if (period == null)
            {
                throw new KeyNotFoundException("Không tìm thấy kỳ lương.");
            }

            var payroll =
                await _repo
                    .GetPayrollForUpdateAsync(
                        payrollId);

            if (payroll == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiếu lương.");
            }

            // Payroll phải thuộc Period trên URL
            if (payroll.PayrollPeriodId != payrollPeriodId)
            {
                throw new ArgumentException("Phiếu lương không thuộc kỳ lương này.");
            }

            /*
             * Quan trọng:
             *
             * Cho phép SUBMITTED hoặc REVISION_REQUIRED.
             *
             * Nếu chỉ cho SUBMITTED:
             * reject nhân viên đầu tiên
             * → Period = REVISION_REQUIRED
             * → không reject được người thứ 2.
             */
            if (period.Status != "SUBMITTED" &&
                period.Status != "REVISION_REQUIRED" && period.Status != "APPROVED")
            {
                throw new InvalidOperationException(
                    "Kỳ lương hiện tại không cho phép " +
                    "yêu cầu chỉnh sửa Payroll.");
            }

            // Chỉ Payroll đang SUBMITTED được reject
            if (payroll.Status == "REJECTED")
            {
                throw new InvalidOperationException(
                    "Phiếu lương này đã bị yêu cầu chỉnh sửa và không thể reject lần nữa.");
            }

            if (payroll.Status != "SUBMITTED")
            {
                throw new InvalidOperationException(
                    "Chỉ phiếu lương đang SUBMITTED " +
                    "mới có thể yêu cầu chỉnh sửa.");
            }

            var now = DateTime.UtcNow;
            var latest = await _repo.GetLatestPayrollAsync(payroll.UserId, payrollPeriodId);
            if (latest?.PayrollId != payrollId)
                throw new InvalidOperationException("Only the current payroll version can be rejected.");
            if (period.Status == "APPROVED" && !await _repo.HasApprovedCorrectionFeedbackAsync(payroll))
                throw new InvalidOperationException("Only a submitted feedback correction can be rejected after period approval.");
            payroll.Status = "REJECTED";
            payroll.RejectionCategory = request.RejectionCategory;
            payroll.RejectedBy = resolvedAdminId;
            payroll.RejectedAt = now;
            payroll.RejectReason = reason;

            // Có ít nhất một Payroll sai
            if (period.Status != "APPROVED") period.Status = "REVISION_REQUIRED";
            period.UpdatedAt = now;
            payroll.UpdatedAt = now;

            _repo.Audit(resolvedAdminId, "REJECT_EMPLOYEE_PAYROLL", period, payroll);
            await _repo.SaveChangesAsync();
            await holidayTransaction.CommitAsync();

            return new PayrollRejectResponse
            {
                Message = "Đã trả phiếu lương về để Accountant điều chỉnh.",
                PayrollPeriodId = payrollPeriodId,
                PayrollId = payrollId,
                PayrollStatus = payroll.Status,
                PayrollPeriodStatus = period.Status,
                RejectedAt = now
            };
        }

        // =====================================================
        // 4. APPROVE WHOLE PAYROLL PERIOD
        // =====================================================
        public async Task<PayrollApprovalResponse>
            ApprovePayrollPeriodAsync(
                int payrollPeriodId,
                Guid? adminId)
        {
            var resolvedAdminId = await ResolveAdminIdAsync(adminId);
            await using var holidayTransaction = await _holidays.BeginPayrollChangeAsync();

            var period =
                await _repo
                    .GetPayrollPeriodForUpdateAsync(
                        payrollPeriodId);

            if (period == null)
            {
                throw new KeyNotFoundException("Không tìm thấy kỳ lương.");
            }

            // Period phải trở lại SUBMITTED
            if (period.Status != "SUBMITTED")
            {
                throw new InvalidOperationException(
                    "Chỉ kỳ lương đang SUBMITTED " +
                    "mới được duyệt.");
            }

            var payrolls =
                await _repo
                    .GetAllPayrollsForUpdateAsync(
                        payrollPeriodId);

            if (payrolls.Count == 0)
            {
                throw new InvalidOperationException("Kỳ lương chưa có phiếu lương.");
            }

            if (!await _repo.HasCompleteLockedTimesheetsAsync(payrollPeriodId, payrolls.Select(p => p.UserId).ToArray()))
                throw new InvalidOperationException("The period must have payrolls for all expected employees with LOCKED timesheets.");
            if (period.ApprovalDeadlineAt.HasValue && DateTime.UtcNow >= period.ApprovalDeadlineAt.Value)
                throw new InvalidOperationException("The period approval deadline has passed.");

            // Còn REJECTED
            if (payrolls.Any(x => x.Status == "REJECTED"))
            {
                throw new InvalidOperationException(
                    "Kỳ lương vẫn còn phiếu lương " +
                    "đang chờ Accountant điều chỉnh.");
            }

            /*
             * Không chỉ check REJECTED.
             *
             * Ví dụ:
             * Payroll A = SUBMITTED
             * Payroll B = CALCULATED
             *
             * vẫn không được approve.
             */
            if (payrolls.Any(x => x.Status != "SUBMITTED"))
            {
                throw new InvalidOperationException("Tất cả phiếu lương phải ở trạng thái SUBMITTED trước khi duyệt kỳ lương.");
            }

            var now = DateTime.UtcNow;

            await ValidateHolidaySnapshotsAsync(payrollPeriodId);

            // Approve toàn bộ Payroll
            foreach (var payroll in payrolls)
            {
                payroll.Status =
                    "APPROVED";
                payroll.ApprovedBy = resolvedAdminId;
                payroll.ApprovedAt = now;
                payroll.UpdatedAt = now;
            }

            // Approve Period
            period.Status = "APPROVED";
            period.ApprovedBy = resolvedAdminId;
            period.ApprovedAt = now;
            period.UpdatedAt = now;
            _repo.Audit(resolvedAdminId, "APPROVE_PAYROLL_PERIOD", period);

            /*
             * EF SaveChanges với nhiều update như trên
             * mặc định thực hiện trong transaction.
             */
            await _repo.SaveChangesAsync();
            await holidayTransaction.CommitAsync();

            return new PayrollApprovalResponse
            {
                Message = "Đã duyệt kỳ lương thành công.",
                PayrollPeriodId = payrollPeriodId,
                Status = period.Status,
                ApprovedAt = now
            };
        }

        // =====================================================
        // 5. ACCOUNTANT SUBMIT REJECTED PAYROLL AGAIN
        // =====================================================
        public async Task<PayrollSubmitResponse>
            SubmitRejectedPayrollAsync(
                int payrollPeriodId,
                int payrollId,
                Guid accountantId)
        {
            if (!await _repo.UserHasActiveRoleAsync(accountantId, "ACCOUNTANT"))
            {
                throw new KeyNotFoundException("Không tìm thấy Accountant đang hoạt động.");
            }

            await using var holidayTransaction = await _holidays.BeginPayrollChangeAsync();
            var period = await _repo.GetPayrollPeriodForUpdateAsync(payrollPeriodId);

            if (period == null)
            {
                throw new KeyNotFoundException("Không tìm thấy kỳ lương.");
            }

            var payroll = await _repo.GetPayrollForUpdateAsync(payrollId);

            if (payroll == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phiếu lương.");
            }

            if (payroll.PayrollPeriodId != payrollPeriodId)
            {
                throw new ArgumentException("Phiếu lương không thuộc kỳ lương này.");
            }

            if (period.Status is not ("REVISION_REQUIRED" or "APPROVED"))
            {
                throw new InvalidOperationException(
                    "Chỉ kỳ lương đang REVISION_REQUIRED " +
                    "mới cho phép gửi lại Payroll.");
            }

            var branchId = await _repo.GetAccountantBranchIdAsync(accountantId);
            if (branchId == null || branchId != period.BranchId)
                throw new UnauthorizedAccessException("The payroll period does not belong to your branch.");

            var previous = payroll.PreviousPayroll;
            var latest = await _repo.GetLatestPayrollAsync(payroll.UserId, payrollPeriodId);
            var postApproval = period.Status == "APPROVED";
            if (payroll.Status != "CALCULATED" || previous == null
                || (!postApproval && previous.Status != "REJECTED")
                || (postApproval && previous.Status is not ("REJECTED" or "APPROVED"))
                || latest?.PayrollId != payroll.PayrollId)
            {
                throw new InvalidOperationException(
                    "Submit the latest CALCULATED correction version linked to the rejected payroll, not the rejected version itself.");
            }

            ValidateCorrection(previous, payroll);
            if (postApproval && !await _repo.HasApprovedCorrectionFeedbackAsync(payroll))
                throw new InvalidOperationException("The correction must be linked to accepted feedback on the published payslip.");
            if (!postApproval && period.ApprovalDeadlineAt.HasValue && DateTime.UtcNow >= period.ApprovalDeadlineAt.Value)
                throw new InvalidOperationException("The period approval deadline has passed.");

            if (previous.UserId != payroll.UserId || previous.PayrollPeriodId != payroll.PayrollPeriodId
                || previous.BranchId != payroll.BranchId || payroll.VersionNo != previous.VersionNo + 1
                || payroll.TimesheetId != previous.TimesheetId || string.IsNullOrWhiteSpace(payroll.CorrectionReason))
                throw new InvalidOperationException("The correction must preserve employee, period, branch and timesheet, with a reason and the next version number.");
            if (payroll.TotalWorkHours != previous.TotalWorkHours)
                throw new InvalidOperationException("The correction cannot change locked work hours.");
            try
            {
                if (JsonNode.Parse(previous.WorkInputSnapshot ?? "null") is not JsonObject
                    || !JsonNode.DeepEquals(JsonNode.Parse(payroll.WorkInputSnapshot ?? "null"), JsonNode.Parse(previous.WorkInputSnapshot!)))
                    throw new InvalidOperationException("The correction must preserve the frozen work snapshot.");
            }
            catch (System.Text.Json.JsonException ex)
            { throw new InvalidOperationException("The work snapshot is invalid.", ex); }
            if (!HasCalculationChange(previous, payroll))
                throw new InvalidOperationException("The correction must change calculation data before it can be resubmitted.");

            var now = DateTime.UtcNow;

            await ValidateHolidaySnapshotsAsync(payrollPeriodId);
            payroll.Status = "SUBMITTED";
            payroll.SubmittedBy = accountantId;
            payroll.SubmittedAt = now;
            payroll.UpdatedAt = now;

            var payrolls = await _repo.GetAllPayrollsForUpdateAsync(payrollPeriodId);

            if (postApproval)
            {
                // Published payrolls remain effective while this individual correction is being reviewed.
            }
            else if (payrolls.Any(x => x.Status != "SUBMITTED"))
            {
                period.Status = "REVISION_REQUIRED";
            }

            else
            {
                period.Status = "SUBMITTED";
                period.SubmittedBy = accountantId;
                period.SubmittedAt = now;
            }

            period.UpdatedAt = now;

            _repo.Audit(accountantId, "SUBMIT_CORRECTED_PAYROLL", period, payroll);
            await _repo.SaveChangesAsync();
            await holidayTransaction.CommitAsync();

            return new PayrollSubmitResponse
            {
                Message = "Đã gửi lại phiếu lương để Admin duyệt.",
                PayrollPeriodId = payrollPeriodId,
                PayrollId = payrollId,
                PayrollStatus = payroll.Status,
                PayrollPeriodStatus = period.Status,
                SubmittedAt = now
            };
        }

        public async Task<PayrollPeriodSubmitResponse> SubmitPayrollPeriodAsync(
            int payrollPeriodId,
            Guid accountantId)
        {
            if (!await _repo.UserHasActiveRoleAsync(accountantId, "ACCOUNTANT"))
            {
                throw new KeyNotFoundException("Không tìm thấy Accountant đang hoạt động.");
            }

            await using var holidayTransaction = await _holidays.BeginPayrollChangeAsync();
            var period = await _repo.GetPayrollPeriodForUpdateAsync(payrollPeriodId)
                ?? throw new KeyNotFoundException("Không tìm thấy kỳ lương.");

            var branchId = await _repo.GetAccountantBranchIdAsync(accountantId);
            if (branchId == null || branchId != period.BranchId)
                throw new UnauthorizedAccessException("The payroll period does not belong to your branch.");

            if (period.Status != "CALCULATED")
            {
                throw new InvalidOperationException(
                    "Chỉ kỳ lương đang CALCULATED mới được gửi Admin duyệt lần đầu.");
            }
            if (period.ApprovalDeadlineAt.HasValue && DateTime.UtcNow >= period.ApprovalDeadlineAt.Value)
                throw new InvalidOperationException("The period approval deadline has passed.");

            var payrolls = await _repo.GetAllPayrollsForUpdateAsync(payrollPeriodId);
            if (payrolls.Count == 0)
            {
                throw new InvalidOperationException("Kỳ lương chưa có phiếu lương.");
            }

            if (payrolls.Any(x => x.Status != "CALCULATED"))
            {
                throw new InvalidOperationException(
                    "Tất cả phiếu lương phải ở trạng thái CALCULATED trước khi gửi duyệt.");
            }

            if (!await _repo.HasCompleteLockedTimesheetsAsync(payrollPeriodId, payrolls.Select(p => p.UserId).ToArray()))
                throw new InvalidOperationException("Payrolls must cover all expected employees with LOCKED timesheets.");

            await ValidateHolidaySnapshotsAsync(payrollPeriodId);
            var admins = await _repo.GetActiveUserIdsByRoleAsync("ADMIN");
            if (admins.Count == 0) throw new InvalidOperationException("Hệ thống chưa có Admin đang hoạt động.");
            Guid? assignedAdminId = admins.Count == 1 ? admins[0] : null;
            var now = DateTime.UtcNow;
            foreach (var payroll in payrolls)
            {
                payroll.Status = "SUBMITTED";
                payroll.SubmittedBy = accountantId;
                payroll.SubmittedAt = now;
                payroll.UpdatedAt = now;
            }

            period.Status = "SUBMITTED";
            period.SubmittedBy = accountantId;
            period.SubmittedAt = now;
            period.UpdatedAt = now;
            _repo.Audit(accountantId, "SUBMIT_PAYROLL_PERIOD", period);
            await _repo.SaveChangesAsync();
            await holidayTransaction.CommitAsync();

            return new PayrollPeriodSubmitResponse
            {
                Message = "Đã gửi kỳ lương cho Admin duyệt.",
                PayrollPeriodId = payrollPeriodId,
                SubmittedPayrollCount = payrolls.Count,
                AssignedAdminId = assignedAdminId,
                Status = period.Status,
                SubmittedAt = now
            };
        }

        public static void ValidateCorrection(Payroll before, Payroll after)
        {
            if (after.PreviousPayrollId != before.PayrollId || after.VersionNo != before.VersionNo + 1
                || after.UserId != before.UserId || after.BranchId != before.BranchId || after.PayrollPeriodId != before.PayrollPeriodId
                || after.TimesheetId != before.TimesheetId || after.TotalWorkHours != before.TotalWorkHours
                || string.IsNullOrWhiteSpace(after.CorrectionReason))
                throw new InvalidOperationException("Correction must preserve locked work and identity and use the next version with a reason.");
            try
            {
                if (JsonNode.Parse(before.WorkInputSnapshot ?? "null") is not JsonObject
                    || !JsonNode.DeepEquals(JsonNode.Parse(before.WorkInputSnapshot!), JsonNode.Parse(after.WorkInputSnapshot ?? "null")))
                    throw new InvalidOperationException("Correction must preserve its frozen work snapshot.");
            }
            catch (System.Text.Json.JsonException ex) { throw new InvalidOperationException("Invalid work snapshot.", ex); }
            if (!HasCalculationChange(before, after)) throw new InvalidOperationException("Correction must change calculation data.");
            ValidateTotals(after);
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

        private async Task<Guid> ResolveAdminIdAsync(Guid? requestedAdminId)
        {
            var activeAdminIds = await _repo.GetActiveUserIdsByRoleAsync("ADMIN");

            if (requestedAdminId.HasValue)
            {
                if (!activeAdminIds.Contains(requestedAdminId.Value))
                {
                    throw new KeyNotFoundException("Không tìm thấy Admin đang hoạt động.");
                }
                return requestedAdminId.Value;
            }

            return activeAdminIds.Count switch
            {
                1 => activeAdminIds[0],
                0 => throw new InvalidOperationException("Hệ thống chưa có Admin đang hoạt động."),
                _ => throw new InvalidOperationException(
                    "Hệ thống có nhiều Admin đang hoạt động; cần xác định X-Admin-Id.")
            };
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

