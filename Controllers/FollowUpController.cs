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
    public class FollowUpController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly IUserScopeService _userScopeService;
        private readonly ApplicationDbContext _context;

        public FollowUpController(
            IFollowUpService followUpService,
            IUserScopeService userScopeService,
            ApplicationDbContext context)
        {
            _followUpService = followUpService;
            _userScopeService = userScopeService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? statusFilter, string? typeFilter, string? timeframeFilter, string? assignedToFilter, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var followUps = await _followUpService.GetFollowUpsAsync(scope, search, statusFilter, typeFilter, timeframeFilter, assignedToFilter, page, 10);

            var users = await _userScopeService.GetAssignableUsersAsync(scope);

            var vm = new FollowUpListViewModel
            {
                FollowUps = followUps,
                Search = search,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                TimeframeFilter = timeframeFilter,
                AssignedToFilter = assignedToFilter,
                UsersList = users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FullName} ({u.Email})" }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, scope);
            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(followUp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var vm = new FollowUpEditViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(1)
            };

            await PopulateDropdownsAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            // Mandatory Business Rule: Follow-up date cannot be earlier than today
            if (model.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today.");
            }

            if (!model.CustomerId.HasValue && !model.LeadId.HasValue && !model.OpportunityId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Follow-up must be linked to a Customer, Lead, or Opportunity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            var followUp = new FollowUp
            {
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Remarks = model.Remarks.Trim(),
                Status = "Planned",
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : model.AssignedTo
            };

            var (success, errorMessage, createdFollowUp) = await _followUpService.CreateFollowUpAsync(followUp, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create follow-up.");
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up scheduled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(CompleteFollowUpViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please provide outcome notes before completing.";
                return RedirectToAction(nameof(Details), new { id = model.FollowUpId });
            }

            var (success, errorMessage) = await _followUpService.CompleteFollowUpAsync(model.FollowUpId, model.OutcomeNotes, scope);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up marked as Completed.";
            }

            return RedirectToAction(nameof(Details), new { id = model.FollowUpId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(RescheduleFollowUpViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (model.NewFollowUpDate.Date < DateTime.UtcNow.Date)
            {
                TempData["ErrorMessage"] = "Rescheduled follow-up date cannot be earlier than today.";
                return RedirectToAction(nameof(Details), new { id = model.FollowUpId });
            }

            var (success, errorMessage) = await _followUpService.RescheduleFollowUpAsync(model.FollowUpId, model.NewFollowUpDate, model.Remarks, scope);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Follow-up successfully rescheduled to {model.NewFollowUpDate:yyyy-MM-dd HH:mm}.";
            }

            return RedirectToAction(nameof(Details), new { id = model.FollowUpId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _followUpService.CancelFollowUpAsync(id, reason, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up cancelled.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(FollowUpEditViewModel model, UserScope scope)
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

            var leadQuery = _context.Leads.Where(l => l.Status != "Converted" && l.Status != "Lost");
            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    leadQuery = leadQuery.Where(l => l.AssignedTo == null || l.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(l.AssignedTo));
                else
                    leadQuery = leadQuery.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
            }
            var leads = await leadQuery.OrderBy(l => l.LeadName).ToListAsync();
            model.LeadsList = leads.Select(l => new SelectListItem { Value = l.LeadId.ToString(), Text = $"{l.LeadName} ({l.CompanyName ?? "Individual"})" }).ToList();

            var oppQuery = _context.Opportunities.Where(o => o.Status == "Open");
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
