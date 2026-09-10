using Microsoft.AspNetCore.Mvc;

namespace UI_Admin.Controllers
{
    public class UserController : Controller
    {
        public IActionResult List()
        {
            return View();
        }
    }
}
