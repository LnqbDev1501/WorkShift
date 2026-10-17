using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkShiftManagement.DAL.Data;
using WorkShiftManagement.DAL.Version.Payroll;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using WorkShiftManagement.BLL.Services.Accountant;
using WorkShiftManagement.BLL.Interfaces.Accountant;
using WorkShiftManagement.BLL.DTOs.Accountant.PayrollFeedback;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Accountant;
using Recalculate = WorkShiftManagement.BLL.DTOs.Accountant.PayrollCalculation.PayrollCorrectionRequest;

var count = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception("FAILED: " + label); Console.WriteLine($"PASS {++count}: {label}"); }
async Task Block(Func<Task> work, string label)
{
    try { await work(); throw new Exception("FAILED: expected rejection: " + label); }
    catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or KeyNotFoundException or ArgumentException) { Check(true, label); }
}
var ct = CancellationToken.None;
var f = new Fixture(); var svc = f.Reviews; var acct = f.Accountant; var staff = f.Staff; var manager = f.Manager; var admin = f.Admin;
await Block(() => f.Calculation.CalculateEmployeeAsync(acct, 1, staff, ct), "No individual first calculation");
f.State.Sources.Salaries.Clear();
await Block(() => f.Calculation.CalculatePeriodAsync(acct, 1, ct), "Missing salary blocks the entire first batch");
Check(f.State.Payrolls.Count == 0 && f.State.Period.Status == "OPEN", "No partial payrolls after source failure");
f = new Fixture(); svc = f.Reviews;
f.FailNextSave = true;
await Block(() => f.Calculation.CalculatePeriodAsync(acct, 1, ct), "Storage failure aborts first batch");
Check(f.State.Payrolls.Count == 0 && f.State.Period.Status == "OPEN", "Persistence rollback leaves no payrolls");
var batch = await f.Calculation.CalculatePeriodAsync(acct, 1, ct);
Check(batch.Payrolls.Count == 2 && f.State.Period.Status == "CALCULATED", "Whole period calculated atomically");
Check(batch.Payrolls.All(p => p.NetSalary == 240000m), "INACTIVE salary still applies to past work dates");
var oldId = batch.Payrolls.Single(p => p.UserId == staff).PayrollId;
var retry = await f.Calculation.CalculatePeriodAsync(acct, 1, ct);
Check(retry.Payrolls.Count == 2 && f.State.Payrolls.Count == 2, "Calculate retry reuses saved versions");
await Block(() => svc.SendPreviewsAsync(manager, 1, ct), "Manager cannot send payroll previews");
var previews = await svc.SendPreviewsAsync(acct, 1, ct); var round = previews.Single(p => p.EmployeeId == staff);
Check(round.RoundNo == 1 && round.Status == null && round.ReviewStatus == "PENDING", "Sending preview creates pending round, not submitted feedback");
Check(round.ReviewDeadlineAt == new DateTime(2026,10,15,17,0,0,DateTimeKind.Utc), "Initial cutoff is exclusive midnight UTC+7");
Check((await svc.SendPreviewsAsync(acct, 1, ct)).Count == 2 && f.State.Feedback.Count == 2, "Send preview retry does not duplicate rounds");
await Block(() => svc.GetAsync(f.OtherStaff, round.FeedbackId, ct), "Staff cannot inspect another employee's preview");
await Block(() => svc.SubmitPeriodAsync(acct, 1, ct), "Open staff reviews block submission");
f.Clock.Utc = new(2026,10,15,16,59,0,DateTimeKind.Utc);
await svc.SubmitAsync(staff, round.FeedbackId, new() { Description = "Missing approved reward" }, ct);
await Block(() => svc.SubmitAsync(staff, round.FeedbackId, new() { Description = "Duplicate" }, ct), "Only one reply to each review round");
f.Clock.Utc = new(2026,10,16,1,0,0,DateTimeKind.Utc);
var accepted = await svc.ApproveAsync(acct, round.FeedbackId, new() { ResolutionNote = "Please verify source" }, ct);
Check(accepted.Status == "APPROVED" && accepted.ReviewStatus == "FEEDBACK_PENDING", "Accepted feedback stays open while Manager works");
Check(f.State.Period.Status == "REVISION_REQUIRED" && f.State.Payrolls.Single(p => p.PayrollId == oldId).Status == "REJECTED", "Correction request rejects only target payroll and flags period");
await Block(() => svc.CreateIssueAsync(acct, batch.Payrolls.Single(p => p.UserId == f.OtherStaff).PayrollId, new() { Reason = "New late case" }, ct), "No new Accountant case after initial cutoff");
var issue = (await svc.IssuesAsync(acct, 1, ct)).Single();
await Block(() => svc.RespondAsync(f.OtherManager, issue.CorrectionRequestId, new() { Action = "EXPLAIN", Note = "Out of branch" }, ct), "Manager scope enforced");
await Block(() => svc.RespondAsync(manager, issue.CorrectionRequestId, new() { Action = "CORRECT", Note = "Only a note" }, ct), "CORRECT requires an actual valid source change");
await svc.RespondAsync(manager, issue.CorrectionRequestId, new() { Action = "EXPLAIN", Note = "Reward belongs to next period" }, ct);
await svc.ReviewIssueAsync(acct, issue.CorrectionRequestId, new() { Decision = "RETURN", Note = "Please check again" }, ct);
var issues = await svc.IssuesAsync(acct, 1, ct); var active = issues.Single(i => i.Status == "OPEN");
Check(issues.Count == 2 && active.PreviousRequestId == issue.CorrectionRequestId && issues.First().ResolvedPayrollId == null, "Returned explanation preserves history and creates one next request");
await svc.RespondAsync(manager, active.CorrectionRequestId, new() { Action = "EXPLAIN", Note = "Verified original source" }, ct);
await svc.ReviewIssueAsync(acct, active.CorrectionRequestId, new() { Decision = "ACCEPT_EXPLANATION", Note = "Accepted explanation" }, ct);
var next = (await svc.ListAsync(staff, new(), ct)).Items.Single(i => i.ReviewStatus == "PENDING");
Check(next.PayrollId == oldId && next.RoundNo == 2 && next.PreviousFeedbackId == round.FeedbackId, "Explanation sends new round for same payroll version");
Check(next.ReviewDeadlineAt == new DateTime(2026,10,16,17,0,0,DateTimeKind.Utc), "Open case gets a new review window after initial cutoff");
await svc.SubmitAsync(staff, next.FeedbackId, new() { Description = "Reward was approved for this period" }, ct);
await svc.ApproveAsync(acct, next.FeedbackId, new() { ResolutionNote = "Correct source" }, ct);
var correction = (await svc.IssuesAsync(acct, 1, ct)).Single(i => i.Status == "OPEN");
f.State.Sources.Adjustments.Add(new() { RewardPenaltyId = 55, UserId = staff, PayrollPeriodId = 1, AdjustmentType = "REWARD", ApprovalStatus = "APPROVED", AdjustmentName = "Reward", Reason = "Performance", Amount = 50000, CreatedAt = f.Clock.Utc });
await svc.RespondAsync(manager, correction.CorrectionRequestId, new() { Action = "CORRECT", Note = "Reward now approved" }, ct);
var version = await svc.RecalculateAsync(acct, oldId, new Recalculate { CorrectionRequestId = correction.CorrectionRequestId }, ct);
Check(version.VersionNo == 2 && version.PreviousPayrollId == oldId && version.NetSalary == 290000m, "Recalculation creates successor from live approved sources");
Check(f.State.Payrolls.Count == 3 && f.State.Payrolls.Single(p => p.PayrollId == oldId).Status == "REPLACED", "Old version preserved and unrelated employee not recalculated");
var v2review = (await svc.ListAsync(staff, new(), ct)).Items.Single(i => i.ReviewStatus == "PENDING");
Check(v2review.PayrollId == version.PayrollId && v2review.RoundNo == 3 && v2review.PreviousFeedbackId == next.FeedbackId, "Review round number is distinct from payroll version number");
await Block(() => svc.RecalculateAsync(acct, oldId, new Recalculate { CorrectionRequestId = correction.CorrectionRequestId }, ct), "Cannot branch or repeat an old version");
await svc.ConfirmAsync(staff, v2review.FeedbackId, ct);
await Block(() => svc.SubmitAsync(staff, v2review.FeedbackId, new() { Description = "Reopen" }, ct), "Confirmed review cannot be reopened");
await svc.ExpireDueAsync(ct);
Check(f.State.Feedback.Single(r => r.EmployeeId == f.OtherStaff).ReviewStatus == "EXPIRED", "Silence expires without recording staff confirmation");
await svc.SubmitPeriodAsync(acct, 1, ct);
Check(f.State.Period.Status == "SUBMITTED" && f.State.Payrolls.Where(p => p.Status != "REPLACED").All(p => p.Status == "SUBMITTED"), "Whole period submits only after reviews and cases finish");
await Block(() => svc.CreateIssueAsync(acct, version.PayrollId, new() { Reason = "After submit" }, ct), "Submitted period cannot receive new correction");
await svc.ApprovePeriodAsync(admin, 1, ct);
Check(f.State.Period.Status == "APPROVED" && f.State.Payrolls.Count(p => p.Status == "APPROVED") == 2, "Admin approves whole period");
Check(f.State.Sources.Timesheets.All(t => t.Status == "LOCKED" && t.LockedBy == admin), "Approval locks reviewed timesheets with Admin metadata");
Check(f.State.Payslips.Count == 2 && f.State.Payslips.All(p => p.Status == "PUBLISHED" && p.PayslipWorkDetails.Count == 1), "Official payslips publish immediately from snapshots");
await svc.ApprovePeriodAsync(admin, 1, ct);
Check(f.State.Payslips.Count == 2, "Approval retry cannot duplicate official slips");
await Block(() => svc.RecalculateAsync(acct, version.PayrollId, new Recalculate { CorrectionRequestId = correction.CorrectionRequestId }, ct), "No correction after approval/publication");
Check(PayslipPolicy.FeedbackBlocked(f.State.Payslips.First(), true, false, f.Clock.Utc) != null, "Official payslip feedback is disabled");
// An explicit rejection also returns a new review window for the same case.
f = new Fixture(); svc = f.Reviews; await f.Calculation.CalculatePeriodAsync(acct,1,ct);
round = (await svc.SendPreviewsAsync(acct,1,ct)).Single(p=>p.EmployeeId==staff);
await svc.SubmitAsync(staff,round.FeedbackId,new(){Description="Question"},ct);
f.Clock.Utc = new(2026,10,16,8,0,0,DateTimeKind.Utc);
await Block(()=>svc.RejectAsync(acct,round.FeedbackId,new(){ResolutionNote=" "},ct),"Rejection requires explanation");
await svc.RejectAsync(acct,round.FeedbackId,new(){ResolutionNote="Source is correct"},ct);
next=(await svc.ListAsync(staff,new(),ct)).Items.Single(p=>p.ReviewStatus=="PENDING");
Check(next.RoundNo==2 && next.PayrollId==round.PayrollId,"Rejected feedback returns same version with result note");
await svc.SubmitAsync(staff,next.FeedbackId,new(){Description="Follow-up on existing issue"},ct);
Check(f.State.Feedback.Single(r=>r.FeedbackId==next.FeedbackId).Status=="SUBMITTED","Valid existing case may continue after initial cutoff");
// A source change after review blocks submission rather than silently overwriting saved payroll.
f=new Fixture();svc=f.Reviews;await f.Calculation.CalculatePeriodAsync(acct,1,ct);previews=await svc.SendPreviewsAsync(acct,1,ct);
foreach(var preview in previews)await svc.ConfirmAsync(preview.EmployeeId,preview.FeedbackId,ct);
f.State.Sources.Adjustments.Add(new(){RewardPenaltyId=66,UserId=staff,PayrollPeriodId=1,AdjustmentType="REWARD",ApprovalStatus="PENDING",AdjustmentName="Unapproved",Reason="Pending",Amount=1000});
await Block(()=>svc.SubmitPeriodAsync(acct,1,ct),"Pending reward/penalty blocks submission");
Check(f.State.Period.Status=="CALCULATED","Failed submit leaves original state");
// Settlement is allowed after the pre-approval review flow completes, without the obsolete Thursday wait.
f=new Fixture();svc=f.Reviews;
f.State.Sources.Adjustments.Add(new(){RewardPenaltyId=77,UserId=staff,PayrollPeriodId=1,AdjustmentType="PENALTY",ApprovalStatus="APPROVED",AdjustmentName="Penalty",Reason="Approved deduction",Amount=320000,CreatedAt=f.Clock.Utc});
await f.Calculation.CalculatePeriodAsync(acct,1,ct);
previews=await svc.SendPreviewsAsync(acct,1,ct);
foreach(var preview in previews)await svc.ConfirmAsync(preview.EmployeeId,preview.FeedbackId,ct);
await svc.SubmitPeriodAsync(acct,1,ct);await svc.ApprovePeriodAsync(admin,1,ct);
var settlement=new PayrollSettlementService(f,f,new NoopNotifier(),f.Clock);
var summary=await settlement.SummaryAsync(acct,1,ct);
Check(summary.CanConfirmSettlement,"Settlement readiness follows completed preview review, not a Thursday publication timer");
await Block(()=>settlement.ConfirmAsync(acct,1,new(){ConfirmedExternalSettlement=false},ct),"External payment confirmation is mandatory");
var paid=await settlement.ConfirmAsync(acct,1,new(){ConfirmedExternalSettlement=true,PaymentReference="BANK-001"},ct);
Check(f.State.Payrolls.All(p=>p.Status=="PAID")&&f.State.Period.Status=="CLOSED","Settlement marks current payrolls PAID and auto-closes whole period");
Check(f.State.Carries.Count==1&&f.State.Carries[0].RemainingAmount==80000m,"Unapplied new deduction creates the exact carry-forward balance");
paid=await settlement.ConfirmAsync(acct,1,new(){ConfirmedExternalSettlement=true},ct);
Check(paid.AlreadySettled&&f.State.Carries.Count==1,"Settlement retry cannot duplicate carry-forward debt");
await Block(()=>svc.SendPreviewsAsync(acct,1,ct),"Closed period is read-only");
// Manager's work correction may legitimately change hours before Admin's final lock.
f=new Fixture();svc=f.Reviews;batch=await f.Calculation.CalculatePeriodAsync(acct,1,ct);
oldId=batch.Payrolls.Single(p=>p.UserId==staff).PayrollId;
issue=await svc.CreateIssueAsync(acct,oldId,new(){Reason="Check payable minutes"},ct);
f.State.Sources.Assignments.Single(a=>a.UserId==staff).Attendance!.ActualWorkingMinutes=420;
f.State.Sources.Timesheets.Single(t=>t.UserId==staff).TotalWorkMinutes=420;
await svc.RespondAsync(manager,issue.CorrectionRequestId,new(){Action="CORRECT",Note="Work minutes corrected through timesheet workflow"},ct);
version=await svc.RecalculateAsync(acct,oldId,new(){CorrectionRequestId=issue.CorrectionRequestId},ct);
Check(version.TotalWorkHours==7m&&version.NetSalary==210000m,"Before final lock a valid work-source correction produces a new work snapshot");
Check(f.State.Payrolls.Single(p=>p.PayrollId==oldId).TotalWorkHours==8m,"Old work snapshot remains unchanged");
// Pending sources are visible but excluded from preview totals; they block only submission/approval.
f=new Fixture();svc=f.Reviews;
f.State.Sources.Adjustments.Add(new(){RewardPenaltyId=88,UserId=staff,PayrollPeriodId=1,AdjustmentType="REWARD",ApprovalStatus="PENDING",AdjustmentName="Pending bonus",Amount=10000,Reason="Pending approval"});
await f.Calculation.CalculatePeriodAsync(acct,1,ct);previews=await svc.SendPreviewsAsync(acct,1,ct);
var pendingDetail=await svc.GetAsync(staff,previews.Single(p=>p.EmployeeId==staff).FeedbackId,ct);
Check(pendingDetail.PendingSourceAdjustments.Count==1&&pendingDetail.OriginalPayroll.TotalReward==0,"Staff sees pending reward separately without including it in payroll");
foreach(var preview in previews)await svc.ConfirmAsync(preview.EmployeeId,preview.FeedbackId,ct);
await Block(()=>svc.SubmitPeriodAsync(acct,1,ct),"Preview is allowed with pending sources but Submit is blocked");
// A source approved after Staff confirmation can still be raised as a NEW Accountant case before the cutoff.
f.State.Sources.Adjustments[0].ApprovalStatus="APPROVED";
issue=await svc.CreateIssueAsync(acct,pendingDetail.OriginalPayroll.PayrollId,new(){Reason="Reward approved after preview confirmation"},ct);
Check(issue.Status=="OPEN"&&f.State.Feedback.Count(fb=>fb.EmployeeId==staff&&fb.PreviousFeedbackId==null)==2,"Accountant may open a distinct new issue before cutoff after an earlier case closed");
// Reminders use the responsible role/status and are deduplicated per scheduled slot.
f=new Fixture();var reminders=new PayrollSystemNotificationService(f,f,f,f.Clock);
await reminders.QueueRemindersAsync(ct);await reminders.QueueRemindersAsync(ct);
Check(f.State.Events.Count==1,"Timesheet REVIEWED handoff notification is durable and not repeated");
await f.Calculation.CalculatePeriodAsync(acct,1,ct);await f.Reviews.SendPreviewsAsync(acct,1,ct);
f.Clock.Utc=new(2026,10,15,2,0,0,DateTimeKind.Utc);var eventCount=f.State.Events.Count;
await reminders.QueueRemindersAsync(ct);await reminders.QueueRemindersAsync(ct);
Check(f.State.Events.Count==eventCount+2,"Each pending Staff review receives one reminder per slot");
// Exercise the real EF metadata guard without opening a database connection.
using(var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused").Options))
{
    var period=new PayrollPeriod{PayrollPeriodId=77,Status="SUBMITTED"};
    var payroll=new Payroll{PayrollId=77,PayrollPeriodId=77,Status="SUBMITTED"};
    db.Attach(period);db.Attach(payroll);period.Status="APPROVED";payroll.Status="APPROVED";payroll.ApprovedBy=admin;payroll.ApprovedAt=DateTime.UtcNow;
    db.ChangeTracker.DetectChanges();
    Check(PayrollInputFreezePolicy.IsFinalApproval(db.ChangeTracker,db.Entry(payroll),77,"Status","ApprovedBy","ApprovedAt","UpdatedAt"),"Freeze permits only the final approval metadata transition");
    payroll.NetSalary=999;db.ChangeTracker.DetectChanges();
    Check(!PayrollInputFreezePolicy.IsFinalApproval(db.ChangeTracker,db.Entry(payroll),77,"Status","ApprovedBy","ApprovedAt","UpdatedAt"),"Approval cannot smuggle changes to frozen salary amounts");
}
Console.WriteLine($"ALL {count} PAYROLL CHECKS PASSED");
await ApiChecks.RunAsync();

public sealed class Clock : TimeProvider { public DateTime Utc {get;set;}=new(2026,10,15,8,0,0,DateTimeKind.Utc);public override DateTimeOffset GetUtcNow()=>new(Utc); }
public sealed class State
{
    public PayrollPeriod Period {get;set;}=null!;
    public PayrollCalculationInputs Sources {get;set;}=null!;
    public List<Payroll> Payrolls {get;set;}=[];
    public List<PayrollFeedbackRequest> Feedback {get;set;}=[];
    public List<PayrollCorrectionRequest> Issues {get;set;}=[];
    public List<Payslip> Payslips {get;set;}=[];
    public List<PayrollCarryForwardDeduction> Carries {get;set;}=[];
    public List<PayrollSystemEvent> Events {get;set;}=[];
    public HashSet<string> ReminderSlots {get;set;}=[];
}
public sealed class Fixture : IPayrollCalculationRepo, IPayrollFeedbackRepo, IPayrollSettlementRepo, IPayrollSystemEventRepo
{
    public Guid Accountant {get;}=Guid.Parse("10000000-0000-0000-0000-000000000001");
    public Guid Admin {get;}=Guid.Parse("10000000-0000-0000-0000-000000000002");
    public Guid Staff {get;}=Guid.Parse("10000000-0000-0000-0000-000000000003");
    public Guid Manager {get;}=Guid.Parse("10000000-0000-0000-0000-000000000004");
    public Guid OtherStaff {get;}=Guid.Parse("10000000-0000-0000-0000-000000000005");
    public Guid OtherManager {get;}=Guid.Parse("10000000-0000-0000-0000-000000000006");
    public State State {get;set;}
    public Clock Clock {get;}=new();
    public bool FailNextSave {get;set;}
    public PayrollFeedbackService Reviews => new(this,this,Clock,NullLogger<PayrollFeedbackService>.Instance);
    public PayrollCalculationService Calculation => new(this);
    public Fixture()
    {
        var period=new PayrollPeriod{PayrollPeriodId=1,BranchId=1,BranchNameSnapshot="Branch",PeriodCode="PAY-1",PeriodName="Week",ScheduleWeekId=1,Status="OPEN",StartDate=new(2026,10,5),EndDate=new(2026,10,11)};
        var sheets=new List<Timesheet>();var assignments=new List<ShiftAssignment>();var salaries=new List<StaffSalaryHistory>();
        foreach(var id in new[]{Staff,OtherStaff})
        {
            var index=sheets.Count+1;var user=new User{UserId=id,FullName="Employee "+index,BranchId=1};
            sheets.Add(new(){TimesheetId=index,UserId=id,User=user,PayrollPeriodId=1,Status="REVIEWED",ReviewedAt=Clock.Utc.AddDays(-1),TotalAssignedShifts=1,TotalWorkMinutes=480});
            var start=new DateTime(2026,10,5,1,0,0,DateTimeKind.Utc);
            assignments.Add(new(){AssignmentId=index,UserId=id,User=user,Shift=new(){ShiftId=index,ScheduleWeekId=1,ShiftNameSnapshot="Morning",ScheduledStartAt=start,ScheduledEndAt=start.AddHours(8)},Attendance=new(){AttendanceId=index,ActualWorkingMinutes=480,Status="PRESENT",ShiftStartAtSnapshot=start,ShiftEndAtSnapshot=start.AddHours(8)}});
            salaries.Add(new(){SalaryHistoryId=index,UserId=id,HourlyRate=30000,Status="INACTIVE",EffectiveFrom=new(2026,1,1),EffectiveTo=new(2026,10,11),Contract=new(){ContractId=index,UserId=id,BranchId=1,ApprovedAt=Clock.Utc.AddMonths(-1),StartDate=new(2026,1,1),EmployeePositionSnapshot="Staff"}});
        }
        State=new(){Period=period,Sources=new(){Period=period,EmployeeIds=[Staff,OtherStaff],Timesheets=sheets,Assignments=assignments,Salaries=salaries}};
    }
    public Task<User?> GetActorAsync(Guid id,CancellationToken ct)=>Task.FromResult<User?>(new(){UserId=id,Status="ACTIVE",BranchId=id==OtherManager?2:1,Role=new(){Status="ACTIVE",RoleName=id==Accountant?"ACCOUNTANT":id==Admin?"ADMIN":id==Manager||id==OtherManager?"MANAGER":"STAFF"}});
    public Task<PayrollPeriod?> GetPeriodAsync(int id,int? branch,CancellationToken ct)=>Task.FromResult<PayrollPeriod?>(id==1&&(!branch.HasValue||branch==1)?State.Period:null);
    public Task<PayrollCalculationInputs> GetInputsAsync(PayrollPeriod p,CancellationToken ct)=>Task.FromResult(new PayrollCalculationInputs{Period=State.Period,EmployeeIds=State.Sources.EmployeeIds,Timesheets=State.Sources.Timesheets,Assignments=State.Sources.Assignments,Salaries=State.Sources.Salaries,Adjustments=State.Sources.Adjustments,Holidays=State.Sources.Holidays,HolidayRules=State.Sources.HolidayRules,CarryForwards=State.Sources.CarryForwards,Payrolls=State.Payrolls});
    public Task<Payroll?> GetPayrollAsync(int id,int? branch,CancellationToken ct)=>Task.FromResult(State.Payrolls.SingleOrDefault(p=>p.PayrollId==id));
    public Task<(List<Payroll> Items,int Count)> GetPayrollsAsync(int id,string? key,Guid? user,string? status,int page,int size,CancellationToken ct)=>Task.FromResult((State.Payrolls,State.Payrolls.Count));
    public void Add(Payroll p){p.PayrollId=State.Payrolls.Count+1;p.PayrollPeriod=State.Period;p.Timesheet=State.Sources.Timesheets.Single(t=>t.UserId==p.UserId);foreach(var d in p.PayrollDetails){d.PayrollId=p.PayrollId;d.PayrollDetailId=p.PayrollId*100+d.DisplayOrder;}State.Payrolls.Add(p);}
    public void Audit(Guid actor,Payroll p){}
    public Task SaveAsync(CancellationToken ct){if(FailNextSave){FailNextSave=false;throw new InvalidOperationException("Injected storage failure");}return Task.CompletedTask;}
    public Task<IDbContextTransaction> BeginCalculationAsync(CancellationToken ct)=>Task.FromResult<IDbContextTransaction>(new Tx(this));
    public Task<PayrollReviewData?> LoadAsync(int id,CancellationToken ct)=>Task.FromResult<PayrollReviewData?>(new(){Period=State.Period,Payrolls=State.Payrolls.ToList(),Reviews=State.Feedback.ToList(),Corrections=State.Issues.ToList()});
    public Task<int?> PeriodForReviewAsync(int id,CancellationToken ct)=>Task.FromResult<int?>(State.Feedback.Any(f=>f.FeedbackId==id)?1:null);
    public Task<int?> PeriodForCorrectionAsync(int id,CancellationToken ct)=>Task.FromResult<int?>(State.Issues.Any(f=>f.CorrectionRequestId==id)?1:null);
    public Task<int?> PeriodForPayrollAsync(int id,CancellationToken ct)=>Task.FromResult<int?>(State.Payrolls.Any(f=>f.PayrollId==id)?1:null);
    public Task<List<int>> ReviewPeriodIdsAsync(int? branch,Guid? employee,CancellationToken ct)=>Task.FromResult(new List<int>{1});
    public Task<List<int>> DueReviewPeriodsAsync(DateTime now,CancellationToken ct)=>Task.FromResult(new List<int>{1});
    public void Add(PayrollFeedbackRequest row){row.FeedbackId=State.Feedback.Count+1;State.Feedback.Add(row);}
    public void Add(PayrollCorrectionRequest row){row.CorrectionRequestId=State.Issues.Count+1;State.Issues.Add(row);}
    public void Add(Payslip row){row.PayslipId=State.Payslips.Count+1;row.Payroll.Payslip=row;State.Payslips.Add(row);}
    public void Audit(Guid? actor,string action,string table,int id,object payload,DateTime now){if(action=="PAYROLL_SYSTEM_EVENT")State.Events.Add(new(){EventId=State.Events.Count+1,CreatedAt=now,Payload=JsonSerializer.Serialize(payload)});}
    public Task<List<PayrollSystemEvent>> ListAsync(string role,int? branch,Guid user,int page,int size,CancellationToken ct)=>Task.FromResult(State.Events);
    public Task<List<PayrollSystemEvent>> PendingAsync(CancellationToken ct)=>Task.FromResult(State.Events);
    public Task<bool> IsDeliveredAsync(long id,CancellationToken ct)=>Task.FromResult(false);
    public void Delivered(long id,DateTime now){}
    public Task<List<int>> ActivePeriodIdsAsync(CancellationToken ct)=>Task.FromResult(new List<int>{1});
    public Task<bool> HasSlotAsync(string slot,CancellationToken ct)=>Task.FromResult(State.ReminderSlots.Contains(slot));
    public void RecordSlot(string slot,DateTime now)=>State.ReminderSlots.Add(slot);
    public Task<PayrollSettlementData?> LoadAsync(int id,int? branch,bool tracking,CancellationToken ct)=>Task.FromResult<PayrollSettlementData?>(new(){Period=State.Period,Payrolls=State.Payrolls,ExpectedEmployees=State.Sources.EmployeeIds.ToHashSet(),CarryForwards=State.Carries,UnresolvedFeedbackCount=State.Feedback.Count(f=>f.ReviewStatus is "PENDING" or "FEEDBACK_PENDING")+State.Issues.Count(i=>i.Status is "OPEN" or "RESPONDED")});
    public Task<(List<PayrollCarryForwardDeduction> Items,int Count)> ListCarryAsync(int branch,CarryForwardSearch q,CancellationToken ct)=>Task.FromResult((State.Carries,State.Carries.Count));
    public Task<PayrollCarryForwardDeduction?> GetCarryAsync(int id,int branch,CancellationToken ct)=>Task.FromResult(State.Carries.SingleOrDefault(c=>c.CarryForwardId==id));
    public void Add(PayrollCarryForwardDeduction row){row.CarryForwardId=State.Carries.Count+1;State.Carries.Add(row);}
    public void Add(PayrollCarryForwardApplication row)=>row.CarryForward.PayrollCarryForwardApplications.Add(row);
    public void Audit(Guid? actor,string action,PayrollPeriod p,DateTime now,object payload){}
    public Task<List<int>> PendingClosureIdsAsync(CancellationToken ct)=>Task.FromResult(new List<int>());
    public Task<long?> PendingClosureEventAsync(int id,CancellationToken ct)=>Task.FromResult<long?>(null);
    private sealed class Tx : IDbContextTransaction
    {
        readonly Fixture f;readonly State snapshot;bool committed;
        public Tx(Fixture fixture){f=fixture;var options=new JsonSerializerOptions{ReferenceHandler=ReferenceHandler.Preserve};snapshot=JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(f.State,options),options)!;}
        public Guid TransactionId {get;}=Guid.NewGuid();public bool SupportsSavepoints=>false;
        public void Commit()=>committed=true;public Task CommitAsync(CancellationToken ct=default){Commit();return Task.CompletedTask;}
        public void Rollback()=>f.State=snapshot;public Task RollbackAsync(CancellationToken ct=default){Rollback();return Task.CompletedTask;}
        public void Dispose(){if(!committed)Rollback();}public ValueTask DisposeAsync(){Dispose();return ValueTask.CompletedTask;}
        public void CreateSavepoint(string n)=>throw new NotSupportedException();public Task CreateSavepointAsync(string n,CancellationToken ct=default)=>throw new NotSupportedException();
        public void RollbackToSavepoint(string n)=>throw new NotSupportedException();public Task RollbackToSavepointAsync(string n,CancellationToken ct=default)=>throw new NotSupportedException();
        public void ReleaseSavepoint(string n)=>throw new NotSupportedException();public Task ReleaseSavepointAsync(string n,CancellationToken ct=default)=>throw new NotSupportedException();
    }
}

public sealed class NoopNotifier : IPayrollClosureNotifier { public Task NotifyAsync(int periodId)=>Task.CompletedTask; }
