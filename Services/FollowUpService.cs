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
    public interface IFollowUpService
    {
        Task<PaginatedList<FollowUp>> GetFollowUpsAsync(UserScope scope, string? search, string? statusFilter, string? typeFilter, string? timeframeFilter, string? assignedToFilter, int pageIndex, int pageSize);
        Task<FollowUp?> GetFollowUpByIdAsync(int followUpId, UserScope scope);
        Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int followUpId, string? outcomeNotes, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> CancelFollowUpAsync(int followUpId, string? reason, UserScope scope);
        Task<List<FollowUp>> GetUpcomingFollowUpsAsync(UserScope scope, int count);
        Task<List<FollowUp>> GetOverdueFollowUpsAsync(UserScope scope, int count);
        Task CheckAndMarkMissedFollowUpsAsync();
    }

    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<FollowUp> ApplyScope(IQueryable<FollowUp> query, UserScope scope)
        {
            if (scope.IsAdmin) return query;

            if (scope.IsManager)
            {
                return query.Where(f => f.AssignedTo == null ||
                                        f.AssignedTo == scope.UserId ||
                                        scope.ManagedUserIds.Contains(f.AssignedTo));
            }

            // Sales Executive
            return query.Where(f => f.AssignedTo == scope.UserId || f.CreatedBy == scope.UserName);
        }

        public async Task<PaginatedList<FollowUp>> GetFollowUpsAsync(UserScope scope, string? search, string? statusFilter, string? typeFilter, string? timeframeFilter, string? assignedToFilter, int pageIndex, int pageSize)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyScope(query, scope);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(f => f.Remarks.ToLower().Contains(s) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(s)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(f => f.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(f => f.FollowUpType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(f => f.AssignedTo == assignedToFilter);
            }

            var now = DateTime.UtcNow;
            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);

            if (timeframeFilter == "today")
            {
                query = query.Where(f => f.FollowUpDate >= todayStart && f.FollowUpDate <= todayEnd);
            }
            else if (timeframeFilter == "upcoming")
            {
                query = query.Where(f => f.FollowUpDate > now && f.Status == "Planned");
            }
            else if (timeframeFilter == "overdue")
            {
                query = query.Where(f => f.FollowUpDate < now && f.Status == "Planned");
            }

            query = query.OrderBy(f => f.FollowUpDate);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<FollowUp>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int followUpId, UserScope scope)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
        }

        public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, UserScope scope)
        {
            if (string.IsNullOrWhiteSpace(followUp.Remarks))
                return (false, "Remarks are required for the follow-up.", null);

            // Mandatory Business Rule: A new/planned follow-up cannot have a date earlier than today
            if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            {
                return (false, "Follow-up date cannot be earlier than today.", null);
            }

            if (!followUp.CustomerId.HasValue && !followUp.LeadId.HasValue && !followUp.OpportunityId.HasValue)
            {
                return (false, "Follow-up must be associated with a Customer, Lead, or Opportunity.", null);
            }

            if (!scope.IsAdmin)
            {
                if (followUp.CustomerId.HasValue)
                {
                    var cust = await _context.Customers.FindAsync(followUp.CustomerId.Value);
                    if (cust == null || !scope.CanAccess(cust.AssignedTo, cust.CreatedBy))
                        return (false, "You are not authorized to associate this follow-up with the specified customer.", null);
                }
                if (followUp.LeadId.HasValue)
                {
                    var lead = await _context.Leads.FindAsync(followUp.LeadId.Value);
                    if (lead == null || !scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                        return (false, "You are not authorized to associate this follow-up with the specified lead.", null);
                }
                if (followUp.OpportunityId.HasValue)
                {
                    var opp = await _context.Opportunities.FindAsync(followUp.OpportunityId.Value);
                    if (opp == null || !scope.CanAccess(opp.AssignedTo, opp.CreatedBy))
                        return (false, "You are not authorized to associate this follow-up with the specified opportunity.", null);
                }
            }

            if (scope.IsSalesExecutive)
            {
                followUp.AssignedTo = scope.UserId;
            }

            followUp.CreatedDate = DateTime.UtcNow;
            followUp.CreatedBy = scope.UserName;
            followUp.Status = "Planned";

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                JsonSerializer.Serialize(new { followUp.FollowUpDate, followUp.FollowUpType, followUp.Remarks, followUp.AssignedTo }),
                $"Follow-up scheduled for {followUp.FollowUpDate:yyyy-MM-dd HH:mm}."
            );

            return (true, null, followUp);
        }

        public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int followUpId, string? outcomeNotes, UserScope scope)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
            if (followUp == null) return (false, "Follow-up not found.");

            if (!scope.CanAccess(followUp.AssignedTo, followUp.CreatedBy))
                return (false, "You are not authorized to complete this follow-up.");

            var oldStatus = followUp.Status;
            followUp.Status = "Completed";
            followUp.CompletedDate = DateTime.UtcNow;
            followUp.OutcomeNotes = outcomeNotes;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Complete",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                oldStatus,
                "Completed",
                $"Follow-up completed with notes: {outcomeNotes}"
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? remarks, UserScope scope)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
            if (followUp == null) return (false, "Follow-up not found.");

            if (!scope.CanAccess(followUp.AssignedTo, followUp.CreatedBy))
                return (false, "You are not authorized to reschedule this follow-up.");

            if (newDate.Date < DateTime.UtcNow.Date)
                return (false, "Rescheduled follow-up date cannot be earlier than today.");

            var oldDate = followUp.FollowUpDate;
            followUp.FollowUpDate = newDate;
            followUp.Status = "Planned";
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = $"{followUp.Remarks} [Rescheduled: {remarks}]";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Reschedule",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                oldDate.ToString("o"),
                newDate.ToString("o"),
                $"Follow-up rescheduled from {oldDate:yyyy-MM-dd HH:mm} to {newDate:yyyy-MM-dd HH:mm}."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> CancelFollowUpAsync(int followUpId, string? reason, UserScope scope)
        {
            var followUp = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
            if (followUp == null) return (false, "Follow-up not found.");

            if (!scope.CanAccess(followUp.AssignedTo, followUp.CreatedBy))
                return (false, "You are not authorized to cancel this follow-up.");

            followUp.Status = "Cancelled";
            if (!string.IsNullOrWhiteSpace(reason))
            {
                followUp.OutcomeNotes = $"Cancelled: {reason}";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Cancel",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                "Planned",
                "Cancelled",
                $"Follow-up cancelled. Reason: {reason}"
            );

            return (true, null);
        }

        public async Task<List<FollowUp>> GetUpcomingFollowUpsAsync(UserScope scope, int count)
        {
            var now = DateTime.UtcNow;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= now)
                .OrderBy(f => f.FollowUpDate)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.Take(count).ToListAsync();
        }

        public async Task<List<FollowUp>> GetOverdueFollowUpsAsync(UserScope scope, int count)
        {
            var now = DateTime.UtcNow;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == "Planned" && f.FollowUpDate < now)
                .OrderBy(f => f.FollowUpDate)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.Take(count).ToListAsync();
        }

        public async Task CheckAndMarkMissedFollowUpsAsync()
        {
            // Optionally marks planned follow-ups older than 24 hours as Missed
            var threshold = DateTime.UtcNow.AddDays(-1);
            var overdue = await _context.FollowUps
                .Where(f => f.Status == "Planned" && f.FollowUpDate < threshold)
                .ToListAsync();

            if (overdue.Any())
            {
                foreach (var f in overdue)
                {
                    f.Status = "Missed";
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
