using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public interface ICustomerService
    {
        Task<PaginatedList<Customer>> GetCustomersAsync(UserScope scope, string? search, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize);
        Task<Customer?> GetCustomerByIdAsync(int customerId, UserScope scope);
        Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int customerId, UserScope scope);
        Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
        Task<List<Customer>> GetActiveCustomersForDropdownAsync(UserScope scope);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Customer> ApplyScope(IQueryable<Customer> query, UserScope scope)
        {
            if (scope.IsAdmin)
            {
                return query;
            }

            if (scope.IsManager)
            {
                return query.Where(c => c.AssignedTo == null ||
                                        c.AssignedTo == scope.UserId ||
                                        scope.ManagedUserIds.Contains(c.AssignedTo));
            }

            // Sales Executive
            return query.Where(c => c.AssignedTo == scope.UserId || c.CreatedBy == scope.UserName);
        }

        public async Task<PaginatedList<Customer>> GetCustomersAsync(UserScope scope, string? search, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize)
        {
            var query = _context.Customers
                .Include(c => c.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyScope(query, scope);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(s) ||
                                         c.Email.ToLower().Contains(s) ||
                                         c.Phone.ToLower().Contains(s) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)) ||
                                         c.CustomerCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(c => c.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(c => c.AssignedTo == assignedToFilter);
            }

            query = sortBy?.ToLower() switch
            {
                "name" => sortDesc ? query.OrderByDescending(c => c.CustomerName) : query.OrderBy(c => c.CustomerName),
                "email" => sortDesc ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
                "company" => sortDesc ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName),
                "status" => sortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
                _ => sortDesc ? query.OrderBy(c => c.CreatedDate) : query.OrderByDescending(c => c.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<Customer>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId, UserScope scope)
        {
            var query = _context.Customers
                .Include(c => c.AssignedUser)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
        {
            var trimmed = email.Trim().ToLower();
            return !await _context.Customers
                .AnyAsync(c => c.Email.ToLower() == trimmed && (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
        {
            var trimmed = phone.Trim();
            return !await _context.Customers
                .AnyAsync(c => c.Phone == trimmed && (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
        }

        public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateCustomerAsync(Customer customer, UserScope scope)
        {
            // Business Validation Rules
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                return (false, "Customer Name is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(customer.Phone))
                return (false, "Phone is required.", null);

            if (!await IsEmailUniqueAsync(customer.Email))
                return (false, "A customer with this email address already exists.", null);

            if (!await IsPhoneUniqueAsync(customer.Phone))
                return (false, "A customer with this phone number already exists.", null);

            // Auto-generate code if empty
            if (string.IsNullOrWhiteSpace(customer.CustomerCode))
            {
                var count = await _context.Customers.CountAsync();
                customer.CustomerCode = $"CUST-{count + 1001}";
            }

            // Sales executive can only assign to themselves if not admin/manager
            if (scope.IsSalesExecutive)
            {
                customer.AssignedTo = scope.UserId;
            }

            customer.CreatedDate = DateTime.UtcNow;
            customer.CreatedBy = scope.UserName;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                JsonSerializer.Serialize(new { customer.CustomerName, customer.Email, customer.Phone, customer.CompanyName }),
                $"Customer '{customer.CustomerName}' created."
            );

            return (true, null, customer);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateCustomerAsync(Customer customer, UserScope scope)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null)
                return (false, "Customer not found.");

            if (!scope.CanAccess(existing.AssignedTo, existing.CreatedBy))
                return (false, "You are not authorized to modify this customer record.");

            if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
                return (false, "Another customer with this email already exists.");

            if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
                return (false, "Another customer with this phone number already exists.");

            var oldValue = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedTo });

            existing.CustomerName = customer.CustomerName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.CompanyName = customer.CompanyName;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.State = customer.State;
            existing.Status = customer.Status;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.UpdatedBy = scope.UserName;

            // Only admin/manager can reassign customers
            if (scope.IsAdmin || scope.IsManager)
            {
                existing.AssignedTo = customer.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.CustomerName, existing.Email, existing.Phone, existing.Status, existing.AssignedTo });

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Update",
                "Customer",
                existing.CustomerId.ToString(),
                oldValue,
                newValue,
                $"Customer '{existing.CustomerName}' updated."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(int customerId, UserScope scope)
        {
            var customer = await _context.Customers
                .Include(c => c.Opportunities)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                return (false, "Customer not found.");

            if (!scope.CanAccess(customer.AssignedTo, customer.CreatedBy))
                return (false, "You are not authorized to delete this customer.");

            // Check if active opportunities exist
            if (customer.Opportunities.Any(o => o.Status == "Open"))
            {
                return (false, "Cannot delete customer with open opportunities. Please close or reassign the opportunities first.");
            }

            // Deactivate customer or remove
            customer.Status = "Inactive";
            customer.UpdatedDate = DateTime.UtcNow;
            customer.UpdatedBy = scope.UserName;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Deactivate",
                "Customer",
                customer.CustomerId.ToString(),
                "Active",
                "Inactive",
                $"Customer '{customer.CustomerName}' deactivated."
            );

            return (true, null);
        }

        public async Task<List<Customer>> GetActiveCustomersForDropdownAsync(UserScope scope)
        {
            var query = _context.Customers.Where(c => c.Status == "Active").AsNoTracking();
            query = ApplyScope(query, scope);
            return await query.OrderBy(c => c.CustomerName).ToListAsync();
        }
    }
}
