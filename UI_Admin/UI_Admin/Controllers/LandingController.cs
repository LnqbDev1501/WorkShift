using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Controllers;

[Authorize]
public sealed class LandingController : Controller
{
    [Authorize(Roles = "STAFF")]
    public IActionResult Staff() => View();

    [Authorize(Roles = "ACCOUNTANT")]
    public IActionResult Accountant() => View();

    [Authorize(Roles = "MANAGER")]
    public IActionResult Manager() => View();
}
