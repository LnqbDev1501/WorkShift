using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthApiService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult SetPassword() => View(new SetPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _authService.SetPasswordAsync(model.AccessToken, model.Password);
                return View("SetPasswordSuccess");
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Không thể thiết lập mật khẩu từ link mời");
                ModelState.AddModelError(string.Empty, exception.Message);
                return View(model);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "Không thể kết nối Supabase Auth");
                ModelState.AddModelError(string.Empty,
                    "Dịch vụ xác thực tạm thời không khả dụng. Vui lòng thử lại.");
                return View(model);
            }
        }
    }
}
