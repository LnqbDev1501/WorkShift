using Microsoft.EntityFrameworkCore;
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
            var details = await _holidays.Details.AsNoTracking().Where(d => d.Payroll.PayrollPeriodId == periodId).ToListAsync();
            var holidays = await _holidays.Holidays.AsNoTracking().Where(h => h.IsActive).ToListAsync();
            var rules = await _holidays.Rules.AsNoTracking().ToListAsync();
            HolidayPayrollPolicy.Validate(details, holidays, rules);
        }
        private readonly IPayrollApprovalRepo _repo;
        private readonly HolidayConfigurationRepo _holidays;

        private static readonly HashSet<string>
            AllowedPeriodStatuses =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    "SUBMITTED",
                    "REVISION_REQUIRED",
                    "APPROVED"
                };

        public PayrollApprovalService(
            IPayrollApprovalRepo repo, HolidayConfigurationRepo holidays)
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
                int pageSize)
        {
            ValidatePagination(
                pageNumber,
                pageSize);

            if (!string.IsNullOrWhiteSpace(status))
            {
                status =
                    status.Trim().ToUpperInvariant();

                if (!AllowedPeriodStatuses.Contains(status))
                {
                    throw new ArgumentException(
                        "Status chỉ chấp nhận " +
                        "SUBMITTED, REVISION_REQUIRED hoặc APPROVED.");
                }
            }

            var result =
                await _repo.GetPayrollPeriodsAsync(
                    status,
                    regionId,
                    branchId,
                    search,
                    pageNumber,
                    pageSize);

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
                        TotalPayrolls = period.Payrolls.Count,
                        RejectedPayrollCount = period.Payrolls.Count(x => x.Status == "REJECTED"),
                        TotalNetSalary = period.Payrolls.Sum(x => x.NetSalary),
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

            var response =
                new PayrollPeriodDetailResponse
                {
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
                        TotalPayrolls = period.Payrolls.Count,
                        RejectedPayrollCount = period.Payrolls.Count(x => x.Status == "REJECTED"),
                        TotalWorkPay = period.Payrolls.Sum(x => x.WorkPay),
                        TotalReward = period.Payrolls.Sum(x => x.TotalReward),
                        TotalPenalty = period.Payrolls.Sum(x => x.TotalPenalty),
                        TotalAttendanceDeduction = period.Payrolls.Sum(x => x.AttendanceDeduction),
                        TotalNetSalary = period.Payrolls.Sum(x => x.NetSalary)
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
                            Reason = payroll.RejectReason
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
                period.Status != "REVISION_REQUIRED")
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
            payroll.Status = "REJECTED";
            payroll.RejectedBy = resolvedAdminId;
            payroll.RejectedAt = now;
            payroll.RejectReason = reason;

            // Có ít nhất một Payroll sai
            period.Status = "REVISION_REQUIRED";

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
            }

            // Approve Period
            period.Status = "APPROVED";
            period.ApprovedBy = resolvedAdminId;
            period.ApprovedAt = now;

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

            if (period.Status != "REVISION_REQUIRED")
            {
                throw new InvalidOperationException(
                    "Chỉ kỳ lương đang REVISION_REQUIRED " +
                    "mới cho phép gửi lại Payroll.");
            }

            if (payroll.Status != "REJECTED")
            {
                throw new InvalidOperationException(
                    "Chỉ phiếu lương đang REJECTED " +
                    "mới được gửi lại duyệt.");
            }

            var now = DateTime.UtcNow;

            await ValidateHolidaySnapshotsAsync(payrollPeriodId);
            payroll.Status = "SUBMITTED";
            payroll.RejectedBy = null;
            payroll.RejectedAt = null;
            payroll.RejectReason = null;

            var payrolls = await _repo.GetAllPayrollsForUpdateAsync(payrollPeriodId);

            if (payrolls.Any(x => x.Status == "REJECTED"))
            {
                period.Status = "REVISION_REQUIRED";
            }
            else
            {
                period.Status = "SUBMITTED";
                period.SubmittedBy = accountantId;
                period.SubmittedAt = now;
            }

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

            if (period.Status != "CALCULATED")
            {
                throw new InvalidOperationException(
                    "Chỉ kỳ lương đang CALCULATED mới được gửi Admin duyệt lần đầu.");
            }

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

            await ValidateHolidaySnapshotsAsync(payrollPeriodId);
            var assignedAdminId = await ResolveAdminIdAsync(null);
            var now = DateTime.UtcNow;
            foreach (var payroll in payrolls)
            {
                payroll.Status = "SUBMITTED";
            }

            period.Status = "SUBMITTED";
            period.SubmittedBy = accountantId;
            period.SubmittedAt = now;
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
        }
    }
}

