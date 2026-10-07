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
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IUserScopeService _userScopeService;
        private readonly ApplicationDbContext _context;

        public CustomerController(
            ICustomerService customerService,
            IUserScopeService userScopeService,
            ApplicationDbContext context)
        {
            _customerService = customerService;
            _userScopeService = userScopeService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc = false, int page = 1)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var customers = await _customerService.GetCustomersAsync(scope, search, statusFilter, assignedToFilter, sortBy, sortDesc, page, 10);

            var users = await _userScopeService.GetAssignableUsersAsync(scope);

            var vm = new CustomerListViewModel
            {
                Customers = customers,
                Search = search,
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
            var customer = await _customerService.GetCustomerByIdAsync(id, scope);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you do not have permission to view this customer.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var vm = new CustomerEditViewModel();
            await PopulateUsersDropdownAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!await _customerService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                AssignedTo = scope.IsSalesExecutive ? scope.UserId : model.AssignedTo
            };

            var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(customer, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to create customer.");
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = createdCustomer!.CustomerId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var customer = await _customerService.GetCustomerByIdAsync(id, scope);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new CustomerEditViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                AssignedTo = customer.AssignedTo
            };

            await PopulateUsersDropdownAsync(vm, scope);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEditViewModel model)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);

            if (!await _customerService.IsEmailUniqueAsync(model.Email, model.CustomerId))
            {
                ModelState.AddModelError("Email", "Another customer with this email address already exists.");
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            {
                ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            var customer = new Customer
            {
                CustomerId = model.CustomerId,
                CustomerCode = model.CustomerCode,
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                AssignedTo = model.AssignedTo
            };

            var (success, errorMessage) = await _customerService.UpdateCustomerAsync(customer, scope);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "Unable to update customer.");
                await PopulateUsersDropdownAsync(model, scope);
                return View(model);
            }

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var scope = await _userScopeService.GetUserScopeAsync(User);
            var (success, errorMessage) = await _customerService.DeleteCustomerAsync(id, scope);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Customer deactivated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateUsersDropdownAsync(CustomerEditViewModel model, UserScope scope)
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
