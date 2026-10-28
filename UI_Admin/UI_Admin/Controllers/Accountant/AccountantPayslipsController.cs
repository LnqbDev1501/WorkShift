using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models.PayrollDocuments;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Accountant;
[Authorize(Roles = "ACCOUNTANT"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountantPayslipsController(PayrollDocumentsApiService api, ILogger<AccountantPayslipsController> logger) : PayslipsUiController(api, logger)
{
    [HttpGet] public Task<IActionResult> Index([FromQuery] PayslipListRequest query, CancellationToken ct) => List(query, ct);
    [HttpGet] public Task<IActionResult> Details(int id, CancellationToken ct) => Detail(id, ct);
    [HttpGet] public Task<IActionResult> Versions(int id, CancellationToken ct) => History(id, ct);
}
