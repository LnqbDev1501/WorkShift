using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Controllers.Accountant;

[Authorize(Roles = "ACCOUNTANT")]
public class AccountantDashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
