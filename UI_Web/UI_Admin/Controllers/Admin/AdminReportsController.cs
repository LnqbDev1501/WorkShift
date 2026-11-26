using Microsoft.AspNetCore.Authorization;
using UI_Admin.Services;

namespace UI_Admin.Controllers.Admin;

[Authorize(Roles = "ADMIN")]
public class AdminReportsController(AccountantOrderApiService api, BranchApiService branchApi)
    : AccountantReportsController(api, branchApi);
