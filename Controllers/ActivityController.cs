using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ActivityController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly IUserScopeService _userScopeService;
        private readonly ApplicationDbContext _context;

        public ActivityController(
            IActivityService activityService,
            IUserScopeService userScopeService,
            ApplicationDbContext context)
        {
            _activityService = activityService;
            _userScopeService = userScopeService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? typeFilter, string? statusFilter, string? assignedToFilter, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var activities = await _activityService.GetActivitiesAsync(scope, search, typeFilter, statusFilter, assignedToFilter, page, 10);

            var users = await _userScopeService.GetAssignableUsersAsync(scope);

            var vm = new ActivityListViewModel
            {
                Activities = activities,
                Search = search,
                TypeFilter = typeFilter,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
                UsersList = users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FullName} ({u.Email})" }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var activity = await _activityService.GetActivityByIdAsync(id, scope);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(activity);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var vm = new ActivityEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = DateTime.UtcNow
            };

            await PopulateDropdownsAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                Status = model.Status,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : model.AssignedTo
            };

            var (success, errorMessage, createdActivity) = await _activityService.CreateActivityAsync(activity, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create activity.");
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' recorded.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var activity = await _activityService.GetActivityByIdAsync(id, scope);
            if (activity == null)
            {
                TempData["ErrorMessage"] = "Activity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new ActivityEditViewModel
            {
                ActivityId = activity.ActivityId,
                ActivityType = activity.ActivityType,
                Subject = activity.Subject,
                Description = activity.Description,
                ActivityDate = activity.ActivityDate,
                CustomerId = activity.CustomerId,
                LeadId = activity.LeadId,
                OpportunityId = activity.OpportunityId,
                Status = activity.Status,
                AssignedTo = activity.AssignedTo
            };

            await PopulateDropdownsAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ActivityEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            var activity = new Activity
            {
                ActivityId = model.ActivityId,
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage) = await _activityService.UpdateActivityAsync(activity, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update activity.");
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = "Activity updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _activityService.DeleteActivityAsync(id, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Activity deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(ActivityEditViewModel model, UserScope scope)
        {
            var custQuery = _context.Customers.Where(c => c.Status == "Active");
            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    custQuery = custQuery.Where(c => c.AssignedTo == null || c.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(c.AssignedTo));
                else
                    custQuery = custQuery.Where(c => c.AssignedTo == scope.UserId || c.CreatedBy == scope.UserName);
            }
            var customers = await custQuery.OrderBy(c => c.CustomerName).ToListAsync();
            model.CustomersList = customers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.Email})" }).ToList();

            var leadQuery = _context.Leads.Where(l => l.Status != "Converted");
            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    leadQuery = leadQuery.Where(l => l.AssignedTo == null || l.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(l.AssignedTo));
                else
                    leadQuery = leadQuery.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
            }
            var leads = await leadQuery.OrderBy(l => l.LeadName).ToListAsync();
            model.LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.CompanyName ?? "Individual"})" }).ToList();

            var oppQuery = _context.Opportunities.AsQueryable();
            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    oppQuery = oppQuery.Where(o => o.AssignedTo == null || o.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(o.AssignedTo));
                else
                    oppQuery = oppQuery.Where(o => o.AssignedTo == scope.UserId || o.CreatedBy == scope.UserName);
            }
            var opps = await oppQuery.OrderBy(o => o.OpportunityName).ToListAsync();
            model.OpportunitiesList = opps.Select(o => new SelectListItem { Value = o.OpportunityId.ToString(), Text = $"{o.OpportunityName} (${o.Amount:N0})" }).ToList();

            var users = await _userScopeService.GetAssignableUsersAsync(scope);
            model.UsersList = users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FullName} ({u.Email})" }).ToList();
        }
    }
}
