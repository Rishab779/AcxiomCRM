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
    public class OpportunityController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly IUserScopeService _userScopeService;
        private readonly ApplicationDbContext _context;

        public OpportunityController(
            IOpportunityService opportunityService,
            ICustomerService customerService,
            IUserScopeService userScopeService,
            ApplicationDbContext context)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
            _userScopeService = userScopeService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? stageFilter, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc = false, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var opps = await _opportunityService.GetOpportunitiesAsync(scope, search, stageFilter, statusFilter, assignedToFilter, sortBy, sortDesc, page, 10);

            var users = await _userScopeService.GetAssignableUsersAsync(scope);

            var vm = new OpportunityListViewModel
            {
                Opportunities = opps,
                Search = search,
                StageFilter = stageFilter,
                StatusFilter = statusFilter,
                AssignedToFilter = assignedToFilter,
                SortBy = sortBy,
                SortDesc = sortDesc,
                UsersList = users.Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FullName} ({u.Email})" }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, scope);
            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(opp);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var vm = new OpportunityEditViewModel
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(1)
            };

            await PopulateDropdownsAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            // Business Rules Validation
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Notes = model.Notes,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : model.AssignedTo
            };

            var (success, errorMessage, createdOpp) = await _opportunityService.CreateOpportunityAsync(opp, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create opportunity.");
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = createdOpp!.OpportunityId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, scope);
            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new OpportunityEditViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Notes = opp.Notes,
                AssignedTo = opp.AssignedTo
            };

            await PopulateDropdownsAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Stage != "Won" && model.Stage != "Lost" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Active Opportunity Expected Close Date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            var opp = new Opportunity
            {
                OpportunityId = model.OpportunityId,
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Notes = model.Notes,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage) = await _opportunityService.UpdateOpportunityAsync(opp, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update opportunity.");
                await PopulateDropdownsAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = opp.OpportunityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStage(int id, string stage)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _opportunityService.ChangeStageAsync(id, stage, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Opportunity advanced to '{stage}' stage.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _opportunityService.DeleteOpportunityAsync(id, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity marked as Lost.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(OpportunityEditViewModel model, UserScope scope)
        {
            var customers = await _customerService.GetActiveCustomersForDropdownAsync(scope);
            model.CustomersList = customers.Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = $"{c.CustomerName} ({c.Email})"
            }).ToList();

            var users = await _userScopeService.GetAssignableUsersAsync(scope);
            model.UsersList = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            }).ToList();
        }
    }
}
