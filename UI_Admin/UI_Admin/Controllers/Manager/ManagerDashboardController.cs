using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Controllers.Manager;

[Authorize(Roles = "MANAGER")]
public class ManagerDashboardController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
