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
    public interface IActivityService
    {
        Task<PaginatedList<Activity>> GetActivitiesAsync(UserScope scope, string? search, string? typeFilter, string? statusFilter, string? assignedToFilter, int pageIndex, int pageSize);
        Task<Activity?> GetActivityByIdAsync(int activityId, UserScope scope);
        Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, UserScope scope);
        Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int activityId, UserScope scope);
        Task<List<Activity>> GetRecentActivitiesAsync(UserScope scope, int count);
    }

    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ActivityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Activity> ApplyScope(IQueryable<Activity> query, UserScope scope)
        {
            if (scope.IsAdmin) return query;

            if (scope.IsManager)
            {
                return query.Where(a => a.AssignedTo == null ||
                                        a.AssignedTo == scope.UserId ||
                                        scope.ManagedUserIds.Contains(a.AssignedTo));
            }

            // Sales Executive
            return query.Where(a => a.AssignedTo == scope.UserId || a.CreatedBy == scope.UserName);
        }

        public async Task<PaginatedList<Activity>> GetActivitiesAsync(UserScope scope, string? search, string? typeFilter, string? statusFilter, string? assignedToFilter, int pageIndex, int pageSize)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsNoTracking()
                .AsQueryable();

            query = ApplyScope(query, scope);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(a => a.Subject.ToLower().Contains(s) ||
                                         (a.Description != null && a.Description.ToLower().Contains(s)) ||
                                         (a.Customer != null && a.Customer.CustomerName.ToLower().Contains(s)) ||
                                         (a.Lead != null && a.Lead.LeadName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                query = query.Where(a => a.ActivityType == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(a => a.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(assignedToFilter))
            {
                query = query.Where(a => a.AssignedTo == assignedToFilter);
            }

            query = query.OrderByDescending(a => a.ActivityDate);

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<Activity>(items, totalCount, pageIndex, pageSize);
        }

        public async Task<Activity?> GetActivityByIdAsync(int activityId, UserScope scope)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.FirstOrDefaultAsync(a => a.ActivityId == activityId);
        }

        public async Task<(bool Success, string? ErrorMessage, Activity? Activity)> CreateActivityAsync(Activity activity, UserScope scope)
        {
            if (string.IsNullOrWhiteSpace(activity.Subject))
                return (false, "Subject is required for the activity.", null);

            var validTypes = new[] { "Call", "Meeting", "Email", "Task" };
            if (!validTypes.Contains(activity.ActivityType))
                return (false, "Invalid activity type.", null);

            if (!scope.IsAdmin)
            {
                if (activity.CustomerId.HasValue)
                {
                    var cust = await _context.Customers.FindAsync(activity.CustomerId.Value);
                    if (cust == null || !scope.CanAccess(cust.AssignedTo, cust.CreatedBy))
                        return (false, "You are not authorized to associate this activity with the specified customer.", null);
                }
                if (activity.LeadId.HasValue)
                {
                    var lead = await _context.Leads.FindAsync(activity.LeadId.Value);
                    if (lead == null || !scope.CanAccess(lead.AssignedTo, lead.CreatedBy))
                        return (false, "You are not authorized to associate this activity with the specified lead.", null);
                }
                if (activity.OpportunityId.HasValue)
                {
                    var opp = await _context.Opportunities.FindAsync(activity.OpportunityId.Value);
                    if (opp == null || !scope.CanAccess(opp.AssignedTo, opp.CreatedBy))
                        return (false, "You are not authorized to associate this activity with the specified opportunity.", null);
                }
            }

            if (scope.IsSalesExecutive)
            {
                activity.AssignedTo = scope.UserId;
            }

            activity.CreatedDate = DateTime.UtcNow;
            activity.CreatedBy = scope.UserName;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                null,
                JsonSerializer.Serialize(new { activity.ActivityType, activity.Subject, activity.ActivityDate, activity.Status }),
                $"Activity '{activity.Subject}' recorded."
            );

            return (true, null, activity);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateActivityAsync(Activity activity, UserScope scope)
        {
            var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
            if (existing == null) return (false, "Activity not found.");

            if (!scope.CanAccess(existing.AssignedTo, existing.CreatedBy))
                return (false, "You are not authorized to update this activity.");

            existing.Subject = activity.Subject;
            existing.ActivityType = activity.ActivityType;
            existing.Description = activity.Description;
            existing.ActivityDate = activity.ActivityDate;
            existing.Status = activity.Status;

            if (scope.IsAdmin || scope.IsManager)
            {
                existing.AssignedTo = activity.AssignedTo;
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Update",
                "Activity",
                existing.ActivityId.ToString(),
                null,
                JsonSerializer.Serialize(new { existing.Subject, existing.Status, existing.ActivityDate }),
                $"Activity '{existing.Subject}' updated."
            );

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteActivityAsync(int activityId, UserScope scope)
        {
            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activityId);
            if (activity == null) return (false, "Activity not found.");

            if (!scope.CanAccess(activity.AssignedTo, activity.CreatedBy))
                return (false, "You are not authorized to delete this activity.");

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                scope.UserId,
                scope.UserName,
                "Delete",
                "Activity",
                activityId.ToString(),
                null,
                null,
                $"Activity ID #{activityId} deleted."
            );

            return (true, null);
        }

        public async Task<List<Activity>> GetRecentActivitiesAsync(UserScope scope, int count)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .OrderByDescending(a => a.ActivityDate)
                .AsNoTracking();

            query = ApplyScope(query, scope);
            return await query.Take(count).ToListAsync();
        }
    }
}
