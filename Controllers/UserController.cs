using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? roleFilter, string? statusFilter)
        {
            var query = _context.Users.Include(u => u.Manager).AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(s) || (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter == "active") query = query.Where(u => u.IsActive);
                else if (statusFilter == "inactive") query = query.Where(u => !u.IsActive);
                else if (statusFilter == "locked") query = query.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();
            var userViewModels = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "No Role";

                if (!string.IsNullOrWhiteSpace(roleFilter) && roleName != roleFilter)
                {
                    continue;
                }

                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Department = user.Department,
                    Role = roleName,
                    IsActive = user.IsActive,
                    IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                    LockoutEnd = user.LockoutEnd?.UtcDateTime,
                    ManagerName = user.Manager?.FullName,
                    CreatedDate = user.CreatedDate
                });
            }

            var vm = new UserListViewModel
            {
                Users = userViewModels,
                Search = search,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new UserCreateViewModel();
            await PopulateUserDropdownsAsync(vm.RolesList, vm.ManagersList);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUserDropdownsAsync(model.RolesList, model.ManagersList);
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "A user with this email address already exists.");
                await PopulateUserDropdownsAsync(model.RolesList, model.ManagersList);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                FullName = model.FullName.Trim(),
                Department = model.Department?.Trim(),
                ManagerId = string.IsNullOrEmpty(model.ManagerId) ? null : model.ManagerId,
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                await PopulateUserDropdownsAsync(model.RolesList, model.ManagersList);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            var adminUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                adminUser?.Id,
                adminUser?.UserName,
                "UserCreate",
                "User",
                user.Id,
                null,
                $"Email: {user.Email}, Role: {model.Role}",
                $"Created user '{user.Email}' with role '{model.Role}'."
            );

            TempData["SuccessMessage"] = $"User '{user.Email}' created successfully with role '{model.Role}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? "Sales Executive",
                ManagerId = user.ManagerId,
                IsActive = user.IsActive,
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            };

            await PopulateUserDropdownsAsync(vm.RolesList, vm.ManagersList, user.Id);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUserDropdownsAsync(model.RolesList, model.ManagersList, model.Id);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var oldRole = currentRoles.FirstOrDefault() ?? "None";

            user.FullName = model.FullName.Trim();
            user.Department = model.Department?.Trim();
            user.ManagerId = string.IsNullOrEmpty(model.ManagerId) ? null : model.ManagerId;
            user.IsActive = model.IsActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                await PopulateUserDropdownsAsync(model.RolesList, model.ManagersList, model.Id);
                return View(model);
            }

            if (oldRole != model.Role)
            {
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            var adminUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                adminUser?.Id,
                adminUser?.UserName,
                "UserUpdate",
                "User",
                user.Id,
                oldRole,
                model.Role,
                $"Updated user profile and role for '{user.Email}'."
            );

            TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            var adminUser = await _userManager.GetUserAsync(User);
            await _auditService.LogAsync(
                adminUser?.Id,
                adminUser?.UserName,
                "Unlock",
                "Security",
                user.Id,
                "Locked",
                "Unlocked",
                $"Admin manually unlocked user account '{user.Email}'."
            );

            TempData["SuccessMessage"] = $"User '{user.Email}' has been unlocked.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent admin deactivating self
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser?.Id == user.Id)
            {
                TempData["ErrorMessage"] = "You cannot deactivate your own administrative account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(
                adminUser?.Id,
                adminUser?.UserName,
                user.IsActive ? "ActivateUser" : "DeactivateUser",
                "User",
                user.Id,
                null,
                user.IsActive ? "Active" : "Inactive",
                $"User '{user.Email}' status changed to {(user.IsActive ? "Active" : "Inactive")}."
            );

            TempData["SuccessMessage"] = $"User account '{(user.IsActive ? "activated" : "deactivated")}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new UserResetPasswordViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                var adminUser = await _userManager.GetUserAsync(User);
                await _auditService.LogAsync(
                    adminUser?.Id,
                    adminUser?.UserName,
                    "AdminResetPassword",
                    "Security",
                    user.Id,
                    null,
                    null,
                    $"Admin reset password for user '{user.Email}'."
                );

                TempData["SuccessMessage"] = $"Password for user '{user.Email}' was reset successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(model);
        }

        private async Task PopulateUserDropdownsAsync(List<SelectListItem> rolesList, List<SelectListItem> managersList, string? excludeUserId = null)
        {
            var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
            rolesList.AddRange(roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }));

            var managers = await _userManager.GetUsersInRoleAsync("Manager");
            managersList.AddRange(managers
                .Where(m => excludeUserId == null || m.Id != excludeUserId)
                .Select(m => new SelectListItem { Value = m.Id, Text = $"{m.FullName} ({m.Email})" }));
        }
    }
}
