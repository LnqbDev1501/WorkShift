using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Controllers;

public class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
