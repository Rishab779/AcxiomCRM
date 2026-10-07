using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                await _auditService.LogAsync(null, model.Email, "FailedLogin", "Auth", null, null, null, $"Failed login attempt for non-existent email: {model.Email}");
                ModelState.AddModelError(string.Empty, "Invalid login credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditService.LogAsync(user.Id, user.Email, "DeactivatedLoginAttempt", "Auth", user.Id, null, null, $"Deactivated account attempted login: {user.Email}");
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact your system administrator.");
                return View(model);
            }

            // SignIn with lockout enabled
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Login", "Auth", user.Id, null, null, $"User '{user.Email}' logged in successfully.");

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Lockout", "Auth", user.Id, null, null, $"Account locked out due to multiple failed login attempts: {user.Email}");
                ModelState.AddModelError(string.Empty, "This account is currently locked due to multiple failed login attempts. Please wait or contact an administrator.");
                return View(model);
            }

            // Failed login
            await _auditService.LogAsync(user.Id, user.Email, "FailedLogin", "Auth", user.Id, null, null, $"Invalid password entered for user: {user.Email}");
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Department = model.Department,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Assign role (Sales Executive by default for self-registration)
                var role = string.IsNullOrWhiteSpace(model.Role) ? "Sales Executive" : model.Role;
                await _userManager.AddToRoleAsync(user, role);

                await _auditService.LogAsync(user.Id, user.Email, "Register", "Auth", user.Id, null, null, $"New user registered: {user.Email} with role {role}");

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.Email, "Logout", "Auth", user.Id, null, null, $"User '{user.Email}' logged out.");
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
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
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                await _auditService.LogAsync(user.Id, user.Email, "ChangePassword", "Auth", user.Id, null, null, "User successfully changed password.");
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
