using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Repositories.Staff;
using WorkShiftManagement.BLL.Services.Manager;

var count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine($"PASS {++count}: {name}");
}
void Block(Action action, string name)
{
    var rejected = false;
    try { action(); }
    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { rejected = true; }
    Check(rejected, name);
}

var rule = new RequestLimitRule { RequestType = "SALARY_INCREASE", MinimumAdvanceDays = 0,
    MaxRequestsPerMonth = 2, SubmissionDeadlineDayOfWeek = 2 };
SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 10, 26));
SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 10, 27));
Check(true, "Published salary rule accepts submissions through its configured weekday");
Block(() => SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 10, 28)), "Weekly cutoff rejects later days");
SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 11, 2));
Check(true, "Next VN week permits salary requests again");
rule.SubmissionDeadlineDayOfWeek = null;
SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 10, 29));
Check(true, "No weekday cutoff permits submission on any day");
rule.MinimumAdvanceDays = 1;
Block(() => SalaryIncreaseRequestPolicy.ValidateSubmissionRule(rule, new(2026, 10, 29)), "Staff request cannot use advance days before an effective date is proposed");

var today = new DateOnly(2026, 10, 29);
var userId = Guid.NewGuid();
var branch = new Branch { BranchId = 1, Status = "ACTIVE" };
var staff = new User { UserId = userId, Status = "ACTIVE", BranchId = 1, Branch = branch,
    Role = new Role { RoleName = "STAFF", Status = "ACTIVE" } };
var contract = new EmploymentContract { ContractId = 10, UserId = userId, BranchId = 1, Status = "ACTIVE",
    ApprovedAt = DateTime.UtcNow, StaffAcceptedAt = DateTime.UtcNow, StartDate = new(2026, 1, 1) };
var salary = new StaffSalaryHistory { SalaryHistoryId = 20, UserId = userId, ContractId = 10, Contract = contract,
    Status = "ACTIVE", HourlyRate = 30000, EffectiveFrom = new(2026, 1, 1) };
var request = new EmployeeRequest { RequestId = 30, UserId = userId, User = staff, BranchId = 1, Branch = branch,
    RequestType = "SALARY_INCREASE", Status = "PENDING", SalaryIncreaseRequestDetail = new SalaryIncreaseRequestDetail {
        RequestId = 30, CurrentContractId = 10, CurrentContract = contract, CurrentHourlyRate = 30000, ProposedHourlyRate = 35000 } };
SalaryIncreaseRequestPolicy.ValidateSource(staff, 1, salary, 35000, today);
SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary]);
Check(salary.HourlyRate == 30000 && request.Status == "PENDING", "Manager validation preserves source rate and does not activate a raise");
salary.Status = "INACTIVE";
SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary]);
Check(true, "Effective date range still identifies a valid historical rate");
salary.HourlyRate = 32000;
Block(() => SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary]), "Manager cannot approve a stale salary snapshot");
salary.HourlyRate = 30000;
Block(() => SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary, salary]), "Overlapping effective salaries block Manager approval");
Block(() => SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, []), "Missing effective salary blocks Manager approval");
request.SalaryIncreaseRequestDetail.ProposedHourlyRate = 30000;
Block(() => SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary]), "Salary increase must exceed current rate");
request.SalaryIncreaseRequestDetail.ProposedHourlyRate = 35000;
request.Status = "APPROVED";
Block(() => SalaryIncreaseRequestPolicy.ValidateManagerApproval(request, today, [salary]), "Processed Staff requests cannot be approved again");

var proposal = new SalaryChangeRequest { ProposedEffectiveFrom = new(2026, 11, 2) };
Check(SalaryChangePolicy.ApprovalDate(proposal, today, null, null) == proposal.ProposedEffectiveFrom,
    "Admin keeps the proposed future Monday");
Block(() => SalaryChangePolicy.ApprovalDate(proposal, today, new(2026, 11, 9), "Move date"), "Admin cannot replace a future proposed date");
Check(SalaryChangePolicy.ApprovalDate(proposal, new(2026, 11, 2), new(2026, 11, 9), "Approved late") == new DateOnly(2026, 11, 9),
    "Late Admin approval selects a new future Monday");
Console.WriteLine($"ALL {count} SALARY CHECKS PASSED");
