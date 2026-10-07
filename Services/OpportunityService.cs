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
    public class PipelineStageSummary
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public interface IOpportunityService
    {
        Task<PaginatedList<Opportunity>> GetOpportunitiesAsync(UserScope scope, string? search, string? stageFilter, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize);
        Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId, UserScope scope);
        Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int opportunityId, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> ChangeStageAsync(int opportunityId, string newStage, UserScope scope);
        Task<List<PipelineStageSummary>> GetPipelineSummaryAsync(UserScope scope);
    }

    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Opportunity> ApplyScope(IQueryable<Opportunity> query, UserScope scope)
        {
            if (scope.IsAdmin) return query;

            if (scope.IsManager)
            {
                return query.Where(o => o.AssignedTo == null ||
                                        o.AssignedTo == scope.UserId ||
                                        scope.ManagedUserIds.Contains(o.AssignedTo));
            }

            // Sales Executive
            return query.Where(o => o.AssignedTo == scope.UserId || o.CreatedBy == scope.UserName);
        }

        public async Task<PaginatedList<Opportunity>> GetOpportunitiesAsync(UserScope scope, string? search, string? stageFilter, string? statusFilter, string? assignedToFilter, string? sortBy, bool sortDesc, int pageIndex, int pageSize)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyScope(query, scope);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(s) ||
                                         o.Customer.CustomerName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(stageFilter))
            {
                query = query.Where(o => o.Stage == stageFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(o => o.AssignedTo == assignedToFilter);
            }

            query = sortBy?.ToLower() switch
            {
                "name" => sortDesc ? query.OrderByDescending(o => o.OpportunityName) : query.OrderBy(o => o.OpportunityName),
                "customer" => sortDesc ? query.OrderByDescending(o => o.Customer.CustomerName) : query.OrderBy(o => o.Customer.CustomerName),
                "amount" => sortDesc ? query.OrderByDescending(o => (double)o.Amount) : query.OrderBy(o => (double)o.Amount),
                "probability" => sortDesc ? query.OrderByDescending(o => o.Probability) : query.OrderBy(o => o.Probability),
                "stage" => sortDesc ? query.OrderByDescending(o => o.Stage) : query.OrderBy(o => o.Stage),
                "date" => sortDesc ? query.OrderByDescending(o => o.ExpectedCloseDate) : query.OrderBy(o => o.ExpectedCloseDate),
                _ => sortDesc ? query.OrderBy(o => o.CreatedDate) : query.OrderByDescending(o => o.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<Opportunity>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId, UserScope scope)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .Include(o => o.FollowUps)
                .Include(o => o.Activities)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
        }

        public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, UserScope scope)
        {
            // Business Validation
            if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
                return (false, "Opportunity Name is required.", null);

            if (opportunity.CustomerId <= 0)
                return (false, "A valid Customer must be selected.", null);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == opportunity.CustomerId);
            if (customer == null)
                return (false, "Selected Customer does not exist.", null);

            if (!scope.CanAccess(customer.AssignedTo, customer.CreatedBy))
                return (false, "You are not authorized to create an opportunity for this customer.", null);

            if (opportunity.LeadId.HasValue)
            {
                var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == opportunity.LeadId.Value);
                if (lead == null || !scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                    return (false, "You are not authorized to associate this opportunity with the specified lead.", null);
            }

            if (opportunity.Amount <= 0)
                return (false, "Active opportunity Amount must be greater than 0.", null);

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.", null);

            if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                return (false, "Expected Close Date cannot be in the past.", null);

            var validStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            if (!validStages.Contains(opportunity.Stage))
                return (false, "Invalid opportunity Stage specified.", null);

            if (opportunity.Stage == "Won")
            {
                opportunity.Status = "Won";
                opportunity.Probability = 100;
            }
            else if (opportunity.Stage == "Lost")
            {
                opportunity.Status = "Lost";
                opportunity.Probability = 0;
            }
            else
            {
                opportunity.Status = "Open";
            }

            if (scope.IsSalesExecutive)
            {
                opportunity.AssignedTo = scope.UserId;
            }

            opportunity.CreatedDate = DateTime.UtcNow;
            opportunity.CreatedBy = scope.UserName;

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Create",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                null,
                JsonSerializer.Serialize(new { opportunity.OpportunityName, opportunity.Amount, opportunity.Stage, opportunity.Probability }),
                $"Opportunity '{opportunity.OpportunityName}' created."
            );

            return (true, null, opportunity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateOpportunityAsync(Opportunity opportunity, UserScope scope)
        {
            var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
            if (existing == null)
                return (false, "Opportunity not found.");

            if (!scope.CanAccess(existing.AssignedTo, existing.CreatedBy))
                return (false, "You are not authorized to update this opportunity.");

            if (opportunity.Amount < 0)
                return (false, "Opportunity Amount cannot be negative.");

            if (opportunity.Status == "Open" && opportunity.Amount <= 0)
                return (false, "Active opportunity Amount must be greater than 0.");

            if (opportunity.Probability < 0 || opportunity.Probability > 100)
                return (false, "Probability must be between 0 and 100.");

            if (opportunity.Status == "Open" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                return (false, "Active Expected Close Date cannot be in the past.");

            var oldValue = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status });

            existing.OpportunityName = opportunity.OpportunityName;
            existing.CustomerId = opportunity.CustomerId;
            existing.Amount = opportunity.Amount;
            existing.Stage = opportunity.Stage;
            existing.Probability = opportunity.Probability;
            existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
            existing.Notes = opportunity.Notes;
            existing.UpdatedDate = DateTime.UtcNow;
            existing.UpdatedBy = scope.UserName;

            if (opportunity.Stage == "Won")
            {
                existing.Status = "Won";
                existing.Probability = 100;
            }
            else if (opportunity.Stage == "Lost")
            {
                existing.Status = "Lost";
                existing.Probability = 0;
            }
            else
            {
                existing.Status = "Open";
            }

            if (scope.IsAdmin || scope.IsManager)
            {
                existing.AssignedTo = opportunity.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = JsonSerializer.Serialize(new { existing.OpportunityName, existing.Amount, existing.Stage, existing.Probability, existing.Status });

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Update",
                "Opportunity",
                existing.OpportunityId.ToString(),
                oldValue,
                newValue,
                $"Opportunity '{existing.OpportunityName}' updated."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteOpportunityAsync(int opportunityId, UserScope scope)
        {
            var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
            if (opportunity == null) return (false, "Opportunity not found.");

            if (!scope.CanAccess(opportunity.AssignedTo, opportunity.CreatedBy))
                return (false, "You are not authorized to delete this opportunity.");

            opportunity.Status = "Lost";
            opportunity.Stage = "Lost";
            opportunity.Probability = 0;
            opportunity.UpdatedDate = DateTime.UtcNow;
            opportunity.UpdatedBy = scope.UserName;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Deactivate",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                null,
                "Lost",
                $"Opportunity '{opportunity.OpportunityName}' marked as Lost."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> ChangeStageAsync(int opportunityId, string newStage, UserScope scope)
        {
            var opportunity = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
            if (opportunity == null) return (false, "Opportunity not found.");

            if (!scope.CanAccess(opportunity.AssignedTo, opportunity.CreatedBy))
                return (false, "You are not authorized to modify this opportunity.");

            var validStages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            if (!validStages.Contains(newStage))
                return (false, "Invalid stage specified.");

            var oldStage = opportunity.Stage;
            opportunity.Stage = newStage;
            opportunity.UpdatedDate = DateTime.UtcNow;
            opportunity.UpdatedBy = scope.UserName;

            if (newStage == "Won")
            {
                opportunity.Status = "Won";
                opportunity.Probability = 100;
            }
            else if (newStage == "Lost")
            {
                opportunity.Status = "Lost";
                opportunity.Probability = 0;
            }
            else
            {
                opportunity.Status = "Open";
                // Auto adjust probability by stage if appropriate
                if (newStage == "Proposal" && opportunity.Probability < 40) opportunity.Probability = 50;
                if (newStage == "Negotiation" && opportunity.Probability < 70) opportunity.Probability = 75;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "StageChange",
                "Opportunity",
                opportunity.OpportunityId.ToString(),
                oldStage,
                newStage,
                $"Opportunity '{opportunity.OpportunityName}' stage changed to {newStage}."
            );

            return (true, null);
        }

        public async Task<List<PipelineStageSummary>> GetPipelineSummaryAsync(UserScope scope)
        {
            var query = _context.Opportunities.AsNoTracking();
            query = ApplyScope(query, scope);

            var list = await query.ToListAsync();

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };

            return stages.Select(s =>
            {
                var stageOpps = list.Where(o => o.Stage == s).ToList();
                return new PipelineStageSummary
                {
                    Stage = s,
                    Count = stageOpps.Count,
                    TotalAmount = stageOpps.Sum(o => o.Amount),
                    WeightedAmount = stageOpps.Sum(o => o.WeightedAmount)
                };
            }).ToList();
        }
    }
}
