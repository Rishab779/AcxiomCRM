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
    public class ConvertLeadModel
    {
        public int LeadId { get; set; }
        public bool CreateNewCustomer { get; set; } = true;
        public int? ExistingCustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        // Opportunity details
        public bool CreateOpportunity { get; set; } = true;
        public string? OpportunityName { get; set; }
        public decimal OpportunityAmount { get; set; }
        public int OpportunityProbability { get; set; } = 20;
        public DateTime? ExpectedCloseDate { get; set; }
        public string Stage { get; set; } = "Qualification";
    }

    public interface ILeadService
    {
        Task<PaginatedList<Lead>> GetLeadsAsync(UserScope scope, string? search, string? statusFilter, string? sourceFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize);
        Task<Lead?> GetLeadByIdAsync(int leadId, UserScope scope);
        Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int leadId, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> ChangeStatusAsync(int leadId, string newStatus, UserScope scope);
        Task<(bool Success, string? ErrorMessage, int? CreatedCustomerId, int? CreatedOpportunityId)> ConvertLeadAsync(ConvertLeadModel model, UserScope scope);
        bool IsValidStatusTransition(string currentStatus, string newStatus);
    }

    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public LeadService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Lead> ApplyScope(IQueryable<Lead> query, UserScope scope)
        {
            if (scope.IsAdmin) return query;

            if (scope.IsManager)
            {
                return query.Where(l => l.AssignedTo == null ||
                                        l.AssignedTo == scope.UserId ||
                                        scope.ManagedUserIds.Contains(l.AssignedTo));
            }

            // Sales Executive
            return query.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
        }

        public async Task<PaginatedList<Lead>> GetLeadsAsync(UserScope scope, string? search, string? statusFilter, string? sourceFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyScope(query, scope);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(s) ||
                                         l.Email.ToLower().Contains(s) ||
                                         l.Phone.ToLower().Contains(s) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)) ||
                                         l.LeadCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(l => l.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(sourceFilter))
            {
                query = query.Where(l => l.Source == sourceFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(l => l.AssignedTo == assignedToFilter);
            }

            query = sortBy?.ToLower() switch
            {
                "name" => sortDesc ? query.OrderByDescending(l => l.LeadName) : query.OrderBy(l => l.LeadName),
                "company" => sortDesc ? query.OrderByDescending(l => l.CompanyName) : query.OrderBy(l => l.CompanyName),
                "status" => sortDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
                "value" => sortDesc ? query.OrderByDescending(l => (double)l.ExpectedValue) : query.OrderBy(l => (double)l.ExpectedValue),
                _ => sortDesc ? query.OrderBy(l => l.CreatedDate) : query.OrderByDescending(l => l.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<Lead>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Lead?> GetLeadByIdAsync(int leadId, UserScope scope)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.FirstOrDefaultAsync(l => l.LeadId == leadId);
        }

        public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateLeadAsync(Lead lead, UserScope scope)
        {
            if (string.IsNullOrWhiteSpace(lead.LeadName))
                return (false, "Lead Name is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Email))
                return (false, "Email is required.", null);

            if (string.IsNullOrWhiteSpace(lead.Phone))
                return (false, "Phone is required.", null);

            if (lead.ExpectedValue < 0)
                return (false, "Expected Value cannot be negative.", null);

            if (string.IsNullOrWhiteSpace(lead.LeadCode))
            {
                var count = await _context.Leads.CountAsync();
                lead.LeadCode = $"LEAD-{count + 1001}";
            }

            if (scope.IsSalesExecutive)
            {
                lead.AssignedTo = scope.UserId;
            }

            lead.CreatedDate = DateTime.UtcNow;
            lead.CreatedBy = scope.UserName;
            lead.Status = "New";

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                JsonSerializer.Serialize(new { lead.LeadName, lead.Email, lead.Phone, lead.Status, lead.AssignedTo }),
                $"Lead '{lead.LeadName}' created."
            );

            return (true, null, lead);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateLeadAsync(Lead lead, UserScope scope)
        {
            var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
            if (existing == null)
                return (false, "Lead not found.");

            if (!scope.CanAccess(existing.AssignedTo, existing.CreatedBy))
                return (false, "You are not authorized to edit this lead.");

            if (existing.Status == "Converted")
                return (false, "Converted leads cannot be modified.");

            if (lead.Status != existing.Status && !IsValidStatusTransition(existing.Status, lead.Status))
            {
                return (false, $"Invalid status transition from '{existing.Status}' to '{lead.Status}'.");
            }

            var oldValue = JsonSerializer.Serialize(new { existing.LeadName, existing.Status, existing.AssignedTo, existing.ExpectedValue });

            existing.LeadName = lead.LeadName;
            existing.Email = lead.Email;
            existing.Phone = lead.Phone;
            existing.CompanyName = lead.CompanyName;
            existing.Source = lead.Source;
            existing.Priority = lead.Priority;
            existing.ExpectedValue = lead.ExpectedValue;
            existing.Status = lead.Status;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.UpdatedBy = scope.UserName;

            if (scope.IsAdmin || scope.IsManager)
            {
                existing.AssignedTo = lead.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.LeadName, existing.Status, existing.AssignedTo, existing.ExpectedValue });

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Update",
                "Lead",
                existing.LeadId.ToString(),
                oldValue,
                newValue,
                $"Lead '{existing.LeadName}' updated."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteLeadAsync(int leadId, UserScope scope)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null)
                return (false, "Lead not found.");

            if (!scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                return (false, "You are not authorized to delete this lead.");

            if (lead.Status == "Converted")
                return (false, "Converted leads cannot be deleted.");

            lead.Status = "Lost";
            lead.UpdatedDate = DateTime.UtcNow;
            lead.UpdatedBy = scope.UserName;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Deactivate",
                "Lead",
                lead.LeadId.ToString(),
                null,
                "Lost",
                $"Lead '{lead.LeadName}' marked as Lost/Deactivated."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ChangeStatusAsync(int leadId, string newStatus, UserScope scope)
        {
            var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == leadId);
            if (lead == null) return (false, "Lead not found.");

            if (!scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                return (false, "You are not authorized to update this lead.");

            if (newStatus == "Converted")
                return (false, "Please use the official 'Convert Lead' workflow to convert this lead.");

            if (!IsValidStatusTransition(lead.Status, newStatus))
                return (false, $"Cannot transition lead from '{lead.Status}' to '{newStatus}'.");

            var oldStatus = lead.Status;
            lead.Status = newStatus;
            lead.UpdatedDate = DateTime.UtcNow;
            lead.UpdatedBy = scope.UserName;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "StatusChange",
                "Lead",
                lead.LeadId.ToString(),
                oldStatus,
                newStatus,
                $"Lead '{lead.LeadName}' status transitioned to {newStatus}."
            );

            return (true, null);
        }

        public bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            if (currentStatus == newStatus) return true;
            if (currentStatus == "Converted") return false; // Converted is final

            return currentStatus switch
            {
                "New" => newStatus is "Contacted" or "Unqualified" or "Lost",
                "Contacted" => newStatus is "Qualified" or "Unqualified" or "Lost",
                "Qualified" => newStatus is "Converted" or "Unqualified" or "Lost",
                "Unqualified" => newStatus is "New" or "Contacted" or "Lost",
                "Lost" => newStatus is "New" or "Contacted",
                _ => false
            };
        }

        public async Task<(bool Success, string? ErrorMessage, int? CreatedCustomerId, int? CreatedOpportunityId)> ConvertLeadAsync(ConvertLeadModel model, UserScope scope)
        {
            var lead = await _context.Leads
                .Include(l => l.FollowUps)
                .Include(l => l.Activities)
                .FirstOrDefaultAsync(l => l.LeadId == model.LeadId);

            if (lead == null)
                return (false, "Lead not found.", null, null);

            if (!scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                return (false, "You are not authorized to convert this lead.", null, null);

            if (lead.Status == "Converted")
                return (false, "This lead has already been converted.", null, null);

            // Execute Transactionally
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Customer customer;

                if (model.CreateNewCustomer)
                {
                    var email = string.IsNullOrWhiteSpace(model.Email) ? lead.Email : model.Email.Trim();
                    var phone = string.IsNullOrWhiteSpace(model.Phone) ? lead.Phone : model.Phone.Trim();

                    // Check duplicate email / phone
                    if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == email.ToLower()))
                    {
                        return (false, $"A customer with email '{email}' already exists. Please choose the existing customer option.", null, null);
                    }

                    if (await _context.Customers.AnyAsync(c => c.Phone == phone))
                    {
                        return (false, $"A customer with phone '{phone}' already exists.", null, null);
                    }

                    var custCount = await _context.Customers.CountAsync();
                    customer = new Customer
                    {
                        CustomerCode = $"CUST-{custCount + 1001}",
                        CustomerName = string.IsNullOrWhiteSpace(model.CustomerName) ? lead.LeadName : model.CustomerName.Trim(),
                        Email = email,
                        Phone = phone,
                        CompanyName = string.IsNullOrWhiteSpace(model.CompanyName) ? lead.CompanyName : model.CompanyName.Trim(),
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = scope.UserName,
                        AssignedTo = lead.AssignedTo ?? scope.UserId
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    if (!model.ExistingCustomerId.HasValue)
                        return (false, "Please select an existing customer.", null, null);

                    var existing = await _context.Customers.FindAsync(model.ExistingCustomerId.Value);
                    if (existing == null)
                        return (false, "Selected customer does not exist.", null, null);

                    if (!scope.CanAccess(existing.AssignedTo, existing.CreatedBy))
                        return (false, "You are not authorized to associate this lead with the selected customer.", null, null);

                    customer = existing;
                }

                Opportunity? opportunity = null;
                if (model.CreateOpportunity)
                {
                    if (model.OpportunityAmount <= 0)
                        return (false, "Active opportunity amount must be greater than zero.", null, null);

                    if (model.OpportunityProbability < 0 || model.OpportunityProbability > 100)
                        return (false, "Probability must be between 0 and 100.", null, null);

                    var closeDate = model.ExpectedCloseDate ?? DateTime.UtcNow.AddMonths(1);
                    if (closeDate.Date < DateTime.UtcNow.Date)
                        return (false, "Expected Close Date cannot be in the past.", null, null);

                    opportunity = new Opportunity
                    {
                        OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{customer.CustomerName} - Initial Deal" : model.OpportunityName.Trim(),
                        CustomerId = customer.CustomerId,
                        LeadId = lead.LeadId,
                        Amount = model.OpportunityAmount,
                        Stage = string.IsNullOrWhiteSpace(model.Stage) ? "Qualification" : model.Stage,
                        Probability = model.OpportunityProbability,
                        ExpectedCloseDate = closeDate,
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow,
                        CreatedBy = scope.UserName,
                        AssignedTo = lead.AssignedTo ?? scope.UserId
                    };

                    _context.Opportunities.Add(opportunity);
                    await _context.SaveChangesAsync();
                }

                // Update Lead Status to Converted
                lead.Status = "Converted";
                lead.ConvertedCustomerId = customer.CustomerId;
                lead.ConvertedOpportunityId = opportunity?.OpportunityId;
                lead.ConvertedDate = DateTime.UtcNow;
                lead.UpdatedDate = DateTime.UtcNow;
                lead.UpdatedBy = scope.UserName;

                // Re-associate any existing Lead FollowUps and Activities with Customer/Opportunity
                foreach (var f in lead.FollowUps)
                {
                    f.CustomerId = customer.CustomerId;
                    if (opportunity != null) f.OpportunityId = opportunity.OpportunityId;
                }

                foreach (var a in lead.Activities)
                {
                    a.CustomerId = customer.CustomerId;
                    if (opportunity != null) a.OpportunityId = opportunity.OpportunityId;
                }

                await _context.SaveChangesAsync();

                // Audit Conversion
                await _auditService.LogAsync(
                    scope.UserId,
                    scope.UserName,
                    "Convert",
                    "Lead",
                    lead.LeadId.ToString(),
                    "Qualified",
                    "Converted",
                    $"Lead '{lead.LeadName}' converted to Customer ID #{customer.CustomerId} and Opportunity ID #{opportunity?.OpportunityId}."
                );

                await transaction.CommitAsync();

                return (true, null, customer.CustomerId, opportunity?.OpportunityId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Conversion error: {ex.Message}", null, null);
            }
        }
    }
}
