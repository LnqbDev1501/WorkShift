using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UI_Admin.Models;
using UI_Admin.Services;

namespace UI_Admin.Controllers
{
    [Authorize]
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
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var login = await _authService.LoginAsync(model.Email, model.Password);
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, login.FullName),
                    new Claim(ClaimTypes.Email, login.Email),
                    new Claim(ClaimTypes.Role, login.Role)
                };
                var now = DateTimeOffset.UtcNow;
                var properties = new AuthenticationProperties
                {
                    IssuedUtc = now,
                    ExpiresUtc = now.AddDays(7)
                };
                properties.StoreTokens(new[]
                {
                    new AuthenticationToken { Name = "access_token", Value = login.AccessToken },
                    new AuthenticationToken { Name = "refresh_token", Value = login.RefreshToken },
                    new AuthenticationToken
                    {
                        Name = "access_token_expires_at",
                        Value = now.AddSeconds(login.ExpiresIn).ToString("O")
                    }
                });

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                    properties);

                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);

                return RedirectToRoleLanding(login.Role);
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "The authentication service is temporarily unavailable. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _authService.RequestPasswordRecoveryAsync(model.Email);
                model.Submitted = true;
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, "The authentication service is temporarily unavailable. Please try again.");
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToRoleLanding(string? role = null)
        {
            role ??= User.FindFirstValue(ClaimTypes.Role);
            return role?.ToUpperInvariant() switch
            {
                "ADMIN" => RedirectToAction("Index", "Dashboard"),
                "STAFF" => RedirectToAction("Staff", "Landing"),
                "ACCOUNTANT" => RedirectToAction("Accountant", "Landing"),
                "MANAGER" => RedirectToAction("Manager", "Landing"),
                _ => RedirectToAction(nameof(AccessDenied))
            };
        }

        [HttpGet]
        public IActionResult SetPassword()
        {
            return View(new SetPasswordViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword() => View(new SetPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _authService.SetPasswordAsync(model.AccessToken, model.Password);
                return View("ResetPasswordSuccess");
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Unable to reset the password using the recovery link.");
                ModelState.AddModelError(string.Empty, exception.Message);
                return View(model);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "Unable to connect to the authentication service.");
                ModelState.AddModelError(string.Empty, "The authentication service is temporarily unavailable. Please try again.");
                return View(model);
            }
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
