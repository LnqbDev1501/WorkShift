using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using UI_Admin.Models;
using UI_Admin.Models.Admin.UserAdmin;
using UI_Admin.Models.Auth;
using UI_Admin.Services;

namespace UI_Admin.Controllers
{
    [Authorize]
    public class AuthController : Controller
    {
        private readonly AuthApiService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            AuthApiService authService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
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

                if (string.Equals(login.Role, "STAFF", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Employees need to log in via the mobile app.");
                    return View(model);
                }
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
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            return View(new ForgotPasswordViewModel());
        }

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
                "MANAGER" => RedirectToAction("Index", "ManagerDashboard"),
                "ACCOUNTANT" => RedirectToAction("Index", "AccountantDashboard"),
                "STAFF" => RedirectToAction("Staff", "Landing"),
                _ => RedirectToAction(nameof(AccessDenied))
            };
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile(CancellationToken cancellationToken)
        {
            try
            {
                var profile = await _authService.GetMyProfileAsync(cancellationToken);
                if (profile == null)
                {
                    return RedirectToAction(nameof(AccessDenied));
                }
                return View(profile);
            }
            catch (HttpRequestException)
            {
                return RedirectToAction(nameof(AccessDenied));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string fullName,
            string phone,
            string gender,
            DateOnly? dateOfBirth,
            string address,
            CancellationToken cancellationToken)
        {
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(03[2-9]|05[689]|07[06-9]|08[1-689]|09[0134678])\d{7}$");
            if (string.IsNullOrWhiteSpace(phone) || !phoneRegex.IsMatch(phone.Trim()))
            {
                TempData["ProfileError"] = "Phone number must be a valid 10-digit Vietnamese mobile number (032-039, 056, 058, 059, 070, 076-079, 081-086, 088, 089, 090, 091, 093, 094, 096-098).";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _authService.UpdateProfileAsync(fullName, phone, gender, dateOfBirth, address, cancellationToken);
                TempData["ProfileUpdated"] = true;
            }
            catch (InvalidOperationException exception)
            {
                TempData["ProfileError"] = exception.Message;
            }
            catch (HttpRequestException)
            {
                TempData["ProfileError"] = "The authentication service is temporarily unavailable. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(IFormFile? avatarFile, CancellationToken cancellationToken)
        {
            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["AvatarError"] = "Please select an image file.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                await _authService.UpdateAvatarAsync(avatarFile, cancellationToken);
                TempData["AvatarUpdated"] = true;
            }
            catch (InvalidOperationException exception)
            {
                TempData["AvatarError"] = exception.Message;
            }
            catch (HttpRequestException)
            {
                TempData["AvatarError"] = "The authentication service is temporarily unavailable. Please try again.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ChangeEmail(CancellationToken cancellationToken)
        {
            try
            {
                var profile = await _authService.GetMyProfileAsync(cancellationToken);
                if (profile == null)
                {
                    return RedirectToAction(nameof(AccessDenied));
                }
                return View(profile);
            }
            catch (HttpRequestException)
            {
                return RedirectToAction(nameof(AccessDenied));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestEmailChange(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["EmailChangeError"] = "Please enter a new email address.";
                return RedirectToAction(nameof(ChangeEmail));
            }

            try
            {
                await _authService.RequestEmailChangeAsync(email, cancellationToken);
                TempData["EmailChangeSent"] = true;
            }
            catch (InvalidOperationException exception)
            {
                TempData["EmailChangeError"] = exception.Message;
            }
            catch (HttpRequestException)
            {
                TempData["EmailChangeError"] = "The authentication service is temporarily unavailable. Please try again.";
            }

            return RedirectToAction(nameof(ChangeEmail));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(string? token, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(token))
                return View("VerifyEmailFailed", "The verification link is invalid or has expired.");

            try
            {
                var profile = await _authService.ConfirmEmailChangeAsync(token, cancellationToken);
                await RefreshEmailClaimAsync(profile.Email);
                return View("VerifyEmailSuccess", profile.Email);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(exception, "Email verification failed.");
                return View("VerifyEmailFailed", exception.Message);
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError(exception, "Unable to connect to the authentication service.");
                return View("VerifyEmailFailed", "The authentication service is temporarily unavailable. Please try again.");
            }
        }

        private async Task RefreshEmailClaimAsync(string newEmail)
        {
            if (User.Identity?.IsAuthenticated != true)
                return;

            var authResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!authResult.Succeeded || authResult.Principal == null)
                return;

            var identity = new ClaimsIdentity(
                authResult.Principal.Claims.Where(claim => claim.Type != ClaimTypes.Email),
                CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Email, newEmail));

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authResult.Properties ?? new AuthenticationProperties());
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = ModelState.Values
                    .SelectMany(state => state.Errors)
                    .Select(error => error.ErrorMessage)
                    .FirstOrDefault() ?? "The new password is invalid.";
                return RedirectToAction(nameof(ChangePassword));
            }

            try
            {
                await _authService.ChangePasswordAsync(model.CurrentPassword, model.NewPassword, cancellationToken);
                TempData["PasswordChanged"] = true;
            }
            catch (InvalidOperationException exception)
            {
                TempData["PasswordError"] = exception.Message;
            }
            catch (HttpRequestException)
            {
                TempData["PasswordError"] = "The authentication service is temporarily unavailable. Please try again.";
            }

            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SetPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            return View(new SetPasswordViewModel());
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            return View(new SetPasswordViewModel());
        }

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
        [AllowAnonymous]
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

        [HttpGet]
        [AllowAnonymous]
        public IActionResult StaffSetup(string token)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            ViewData["ApiBaseUrl"] = _configuration["WorkShiftApi:BaseUrl"] ?? "https://localhost:7193";

            var model = new StaffSetupViewModel
            {
                Token = token ?? string.Empty
            };
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult StaffSetupSuccess()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToRoleLanding();

            if (!Request.Cookies.ContainsKey("StaffSetupDone"))
            {
                return RedirectToAction(nameof(Login));
            }

            Response.Cookies.Delete("StaffSetupDone");
            return View();
        }
    }
}
