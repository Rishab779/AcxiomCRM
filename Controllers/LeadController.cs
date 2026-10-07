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
    public class LeadController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly ICustomerService _customerService;
        private readonly IUserScopeService _userScopeService;
        private readonly ApplicationDbContext _context;

        public LeadController(
            ILeadService leadService,
            ICustomerService customerService,
            IUserScopeService userScopeService,
            ApplicationDbContext context)
        {
            _leadService = leadService;
            _customerService = customerService;
            _userScopeService = userScopeService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? statusFilter, string? sourceFilter, string? assignedToFilter, string? sortBy, bool sortDesc = false, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var leads = await _leadService.GetLeadsAsync(scope, search, statusFilter, sourceFilter, assignedToFilter, sortBy, sortDesc, page, 10);

            var users = await _userScopeService.GetAssignableUsersAsync(scope);

            var vm = new LeadListViewModel
            {
                Leads = leads,
                Search = search,
                StatusFilter = statusFilter,
                SourceFilter = sourceFilter,
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
            var lead = await _leadService.GetLeadByIdAsync(id, scope);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var vm = new LeadEditViewModel();
            await PopulateUsersDropdownAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            var lead = new Lead
            {
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : model.AssignedTo
            };

            var (success, errorMessage, createdLead) = await _leadService.CreateLeadAsync(lead, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create lead.");
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = createdLead!.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var lead = await _leadService.GetLeadByIdAsync(id, scope);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "Converted leads cannot be modified.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            var vm = new LeadEditViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                Status = lead.Status,
                AssignedTo = lead.AssignedTo
            };

            await PopulateUsersDropdownAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            var lead = new Lead
            {
                LeadId = model.LeadId,
                LeadCode = model.LeadCode,
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage) = await _leadService.UpdateLeadAsync(lead, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update lead.");
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = lead.LeadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string newStatus)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _leadService.ChangeStatusAsync(id, newStatus, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"Lead status updated to '{newStatus}'.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var lead = await _leadService.GetLeadByIdAsync(id, scope);
            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.Status == "Converted")
            {
                TempData["ErrorMessage"] = "This lead is already converted.";
                return RedirectToAction(nameof(Details), new { id = lead.LeadId });
            }

            var activeCustomers = await _customerService.GetActiveCustomersForDropdownAsync(scope);

            var vm = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                OpportunityName = $"{lead.LeadName} - Project Acquisition",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000,
                OpportunityProbability = 20,
                ExpectedCloseDate = DateTime.UtcNow.AddMonths(1),
                Stage = "Qualification",
                ExistingCustomersList = activeCustomers.Select(c => new SelectListItem
                {
                    Value = c.CustomerId.ToString(),
                    Text = $"{c.CustomerName} ({c.Email})"
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (model.CreateOpportunity && model.OpportunityAmount <= 0)
            {
                ModelState.AddModelError("OpportunityAmount", "Opportunity amount must be greater than zero.");
            }

            if (model.CreateOpportunity && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past.");
            }

            if (!ModelState.IsValid)
            {
                var activeCustomers = await _customerService.GetActiveCustomersForDropdownAsync(scope);
                model.ExistingCustomersList = activeCustomers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.Email})" }).ToList();
                return View(model);
            }

            var convertModel = new ConvertLeadModel
            {
                LeadId = model.LeadId,
                CreateNewCustomer = model.CreateNewCustomer,
                ExistingCustomerId = model.ExistingCustomerId,
                CustomerName = model.CustomerName,
                Email = model.Email,
                Phone = model.Phone,
                CompanyName = model.CompanyName,
                Address = model.Address,
                City = model.City,
                State = model.State,
                CreateOpportunity = model.CreateOpportunity,
                OpportunityName = model.OpportunityName,
                OpportunityAmount = model.OpportunityAmount,
                OpportunityProbability = model.OpportunityProbability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Stage = model.Stage
            };

            var (success, errorMessage, createdCustomerId, createdOpportunityId) = await _leadService.ConvertLeadAsync(convertModel, scope);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Failed to convert lead.");
                var activeCustomers = await _customerService.GetActiveCustomersForDropdownAsync(scope);
                model.ExistingCustomersList = activeCustomers.Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.CustomerName} ({c.Email})" }).ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = "Lead successfully converted to Customer and Sales Opportunity!";
            return RedirectToAction("Details", "Customer", new { id = createdCustomerId!.Value });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _leadService.DeleteLeadAsync(id, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Lead status marked as Lost.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(LeadEditViewModel model, UserScope scope)
        {
            var users = await _userScopeService.GetAssignableUsersAsync(scope);
            model.UsersList = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FullName} ({u.Email})"
            }).ToList();
        }
    }
}
