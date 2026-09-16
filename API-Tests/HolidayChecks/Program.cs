using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models.Holidays;
using UI_Admin.Services;
using WorkShiftManagement.BLL.Services.Admin;
using WorkShiftManagement.DAL.Entities;
using WorkShiftManagement.DAL.Interfaces.Admin;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
}
static CreateHolidaysViewModel Range(DateOnly start, DateOnly end) => new()
{
    Holidays = [new() { HolidayName = "Test holiday", FromDate = start, ToDate = end, PayMultiplier = 3 }]
};
static bool Valid(CreateHolidaysViewModel model) =>
    Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);

var first = new DateOnly(2028, 2, 28);
var three = Range(first, first.AddDays(2));
Check(Valid(three) && three.TotalDays == 3, "Inclusive range across leap day");
Check(Valid(Range(first, first)), "Single day range");
Check(!Valid(Range(first, first.AddDays(-1))), "Reject reversed range");
Check(Valid(Range(first, first.AddDays(365))), "Accept 366 days");
Check(!Valid(Range(first, first.AddDays(366))), "Reject 367 days");
var overlap = Range(first, first.AddDays(2));
overlap.Holidays.Add(new() { FromDate = first.AddDays(2), ToDate = first.AddDays(3) });
Check(!Valid(overlap), "Reject overlapping ranges");
Check(Valid(Range(DateOnly.MaxValue, DateOnly.MaxValue)), "Accept final supported calendar day");

var handler = new CaptureHandler();
var api = new HolidayApiService(new HttpClient(handler),
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["WorkShiftApi:BaseUrl"] = "http://test.invalid" }).Build());
await api.CreateAsync(three);
using (var document = JsonDocument.Parse(handler.Body!))
{
    var entries = document.RootElement.GetProperty("holidays");
    Check(entries.GetArrayLength() == 3 &&
        entries[1].GetProperty("holidayDate").GetString() == "2028-02-29" &&
        entries[2].GetProperty("holidayDate").GetString() == "2028-03-01",
        "Create sends three dated items in one batch");
}
await api.RestoreAsync(9);
Check(handler.Method == HttpMethod.Patch && handler.Path == "/api/public-holidays/9/restore",
    "UI sends PATCH to restore endpoint");

var repo = DispatchProxy.Create<IPublicHolidayRepo, RepoProxy>();
var fake = (RepoProxy)(object)repo;
var service = new PublicHolidayService(repo);
fake.Item = new PublicHolidayConfig
{
    PublicHolidayId = 9, HolidayName = "Existing", HolidayDate = first,
    PayMultiplier = 3, IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1)
};
var created = fake.Item.CreatedAt;
var restored = await service.RestoreAsync(9, Guid.NewGuid());
Check(restored.IsActive && restored.HolidayDate == first && restored.PayMultiplier == 3 &&
    fake.Item.CreatedAt == created && fake.Saves == 1 && fake.Audits == 1,
    "Restore preserves identity and terms, saves and audits");
await service.RestoreAsync(9, Guid.NewGuid());
Check(fake.Saves == 1 && fake.Audits == 1, "Repeated restore is idempotent");
try { await service.RestoreAsync(0, Guid.Empty); throw new Exception("Invalid ID accepted"); }
catch (ArgumentException e) { Check((string?)e.Data["code"] == "INVALID_ID", "Reject invalid restore ID"); }
fake.Item = null;
try { await service.RestoreAsync(99, Guid.Empty); throw new Exception("Missing ID accepted"); }
catch (KeyNotFoundException e) { Check((string?)e.Data["code"] == "HOLIDAY_NOT_FOUND", "Reject missing holiday"); }

public class RepoProxy : DispatchProxy
{
    public PublicHolidayConfig? Item;
    public int Saves;
    public int Audits;
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        switch (method!.Name)
        {
            case "ExecuteInTransactionAsync": return ((Delegate)args![0]!).DynamicInvoke();
            case "GetForUpdateAsync": return Task.FromResult(Item);
            case "SaveChangesAsync": Saves++; return Task.FromResult(1);
            case "WriteAuditAsync": Audits++; return Task.CompletedTask;
            default: throw new NotSupportedException(method.Name);
        }
    }
}
public class CaptureHandler : HttpMessageHandler
{
    public string? Body;
    public string? Path;
    public HttpMethod? Method;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        Path = request.RequestUri!.AbsolutePath;
        Method = request.Method;
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}
