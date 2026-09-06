using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AuthApiService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult SetPassword()
        {
            return View(new SetPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(
            SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _authService.SetPasswordAsync(
                    model.AccessToken,
                    model.Password);

                return View("SetPasswordSuccess");
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Unable to set the password using the invitation link.");

                ModelState.AddModelError(
                    string.Empty,
                    exception.Message);

                return View(model);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to connect to the authentication service.");

                ModelState.AddModelError(
                    string.Empty,
                    "The authentication service is temporarily unavailable. Please try again.");

                return View(model);
            }
        }
    }
}