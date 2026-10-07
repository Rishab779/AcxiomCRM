using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class PipelineOwnerSummary
    {
        public string OwnerName { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportViewModel
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public double ConversionRate => TotalLeads > 0 ? (ConvertedLeads / (double)TotalLeads) * 100 : 0;
        public decimal TotalConvertedOpportunityValue { get; set; }
        public List<Lead> LeadDetails { get; set; } = new();
    }

    public class UserActivityReportItem
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int FollowUpsPlanned { get; set; }
        public int FollowUpsCompleted { get; set; }
        public int OpportunitiesCreated { get; set; }
    }

    public interface IReportService
    {
        Task<PaginatedList<Customer>> GetCustomerReportAsync(UserScope scope, string? status, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<PaginatedList<Lead>> GetLeadReportAsync(UserScope scope, string? status, string? source, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<PaginatedList<FollowUp>> GetFollowUpReportAsync(UserScope scope, string? status, string? type, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<PaginatedList<Opportunity>> GetOpportunityReportAsync(UserScope scope, string? stage, string? status, DateTime? startDate, DateTime? endDate, int page, int pageSize);
        Task<(List<PipelineStageSummary> StageSummary, List<PipelineOwnerSummary> OwnerSummary, decimal TotalWeightedPipeline)> GetPipelineReportAsync(UserScope scope);
        Task<ConversionReportViewModel> GetConversionReportAsync(UserScope scope, DateTime? startDate, DateTime? endDate);
        Task<List<UserActivityReportItem>> GetUserActivityReportAsync(UserScope scope);
        Task<PaginatedList<AuditLog>> GetAuditReportAsync(UserScope scope, string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    }

    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedList<Customer>> GetCustomerReportAsync(UserScope scope, string? status, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.Customers.Include(c => c.AssignedUser).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(c => c.AssignedTo == null || c.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(c.AssignedTo));
                else
                    query = query.Where(c => c.AssignedTo == scope.UserId || c.CreatedBy == scope.UserName);
            }

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
            if (startDate.HasValue) query = query.Where(c => c.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(c => c.CreatedDate <= endDate.Value.AddDays(1).AddTicks(-1));

            query = query.OrderByDescending(c => c.CreatedDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<Customer>(items, total, page, pageSize);
        }

        public async Task<PaginatedList<Lead>> GetLeadReportAsync(UserScope scope, string? status, string? source, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(l => l.AssignedTo == null || l.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(l.AssignedTo));
                else
                    query = query.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
            }

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
            if (!string.IsNullOrWhiteSpace(source)) query = query.Where(l => l.Source == source);
            if (startDate.HasValue) query = query.Where(l => l.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(l => l.CreatedDate <= endDate.Value.AddDays(1).AddTicks(-1));

            query = query.OrderByDescending(l => l.CreatedDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<Lead>(items, total, page, pageSize);
        }

        public async Task<PaginatedList<FollowUp>> GetFollowUpReportAsync(UserScope scope, string? status, string? type, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(f => f.AssignedTo == null || f.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(f.AssignedTo));
                else
                    query = query.Where(f => f.AssignedTo == scope.UserId || f.CreatedBy == scope.UserName);
            }

            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(f => f.FollowUpType == type);
            if (startDate.HasValue) query = query.Where(f => f.FollowUpDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(f => f.FollowUpDate <= endDate.Value.AddDays(1).AddTicks(-1));

            query = query.OrderByDescending(f => f.FollowUpDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<FollowUp>(items, total, page, pageSize);
        }

        public async Task<PaginatedList<Opportunity>> GetOpportunityReportAsync(UserScope scope, string? stage, string? status, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(o => o.AssignedTo == null || o.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(o.AssignedTo));
                else
                    query = query.Where(o => o.AssignedTo == scope.UserId || o.CreatedBy == scope.UserName);
            }

            if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
            if (startDate.HasValue) query = query.Where(o => o.ExpectedCloseDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(o => o.ExpectedCloseDate <= endDate.Value.AddDays(1).AddTicks(-1));

            query = query.OrderByDescending(o => (double)o.Amount);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<Opportunity>(items, total, page, pageSize);
        }

        public async Task<(List<PipelineStageSummary> StageSummary, List<PipelineOwnerSummary> OwnerSummary, decimal TotalWeightedPipeline)> GetPipelineReportAsync(UserScope scope)
        {
            var query = _context.Opportunities.Include(o => o.AssignedUser).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(o => o.AssignedTo == null || o.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(o.AssignedTo));
                else
                    query = query.Where(o => o.AssignedTo == scope.UserId || o.CreatedBy == scope.UserName);
            }

            var opps = await query.ToListAsync();

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var stageSummary = stages.Select(s =>
            {
                var list = opps.Where(o => o.Stage == s).ToList();
                return new PipelineStageSummary
                {
                    Stage = s,
                    Count = list.Count,
                    TotalAmount = list.Sum(o => o.Amount),
                    WeightedAmount = list.Sum(o => o.WeightedAmount)
                };
            }).ToList();

            var ownerSummary = opps
                .GroupBy(o => o.AssignedUser?.FullName ?? o.AssignedUser?.UserName ?? "Unassigned")
                .Select(g => new PipelineOwnerSummary
                {
                    OwnerName = g.Key,
                    OpportunityCount = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    WeightedAmount = g.Sum(o => o.WeightedAmount)
                })
                .OrderByDescending(o => o.TotalAmount)
                .ToList();

            var totalWeighted = opps.Where(o => o.Status == "Open").Sum(o => o.WeightedAmount);

            return (stageSummary, ownerSummary, totalWeighted);
        }

        public async Task<ConversionReportViewModel> GetConversionReportAsync(UserScope scope, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer).AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    query = query.Where(l => l.AssignedTo == null || l.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(l.AssignedTo));
                else
                    query = query.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
            }

            if (startDate.HasValue) query = query.Where(l => l.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(l => l.CreatedDate <= endDate.Value.AddDays(1).AddTicks(-1));

            var leads = await query.ToListAsync();

            var convertedLeadIds = leads.Where(l => l.Status == "Converted").Select(l => l.LeadId).ToList();
            var convertedAmounts = await _context.Opportunities
                .Where(o => o.LeadId.HasValue && convertedLeadIds.Contains(o.LeadId.Value))
                .Select(o => o.Amount)
                .ToListAsync();
            var convertedOppsValue = convertedAmounts.Sum();

            return new ConversionReportViewModel
            {
                TotalLeads = leads.Count,
                ConvertedLeads = leads.Count(l => l.Status == "Converted"),
                LostLeads = leads.Count(l => l.Status == "Lost" || l.Status == "Unqualified"),
                ActiveLeads = leads.Count(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified"),
                TotalConvertedOpportunityValue = convertedOppsValue,
                LeadDetails = leads.OrderByDescending(l => l.CreatedDate).Take(100).ToList()
            };
        }

        public async Task<List<UserActivityReportItem>> GetUserActivityReportAsync(UserScope scope)
        {
            var usersQuery = _context.Users.AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                    usersQuery = usersQuery.Where(u => u.Id == scope.UserId || scope.ManagedUserIds.Contains(u.Id));
                else
                    usersQuery = usersQuery.Where(u => u.Id == scope.UserId);
            }

            var users = await usersQuery.ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            var activities = await _context.Activities
                .Where(a => a.AssignedTo != null && userIds.Contains(a.AssignedTo))
                .AsNoTracking()
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Where(f => f.AssignedTo != null && userIds.Contains(f.AssignedTo))
                .AsNoTracking()
                .ToListAsync();

            var opportunities = await _context.Opportunities
                .Where(o => o.AssignedTo != null && userIds.Contains(o.AssignedTo))
                .AsNoTracking()
                .ToListAsync();

            var report = new List<UserActivityReportItem>();
            foreach (var user in users)
            {
                var userActs = activities.Where(a => a.AssignedTo == user.Id).ToList();
                var userFollows = followUps.Where(f => f.AssignedTo == user.Id).ToList();
                var userOpps = opportunities.Where(o => o.AssignedTo == user.Id).ToList();

                report.Add(new UserActivityReportItem
                {
                    UserId = user.Id,
                    UserName = user.FullName ?? user.UserName ?? "User",
                    Role = user.Department ?? "Sales",
                    CallsCount = userActs.Count(a => a.ActivityType == "Call"),
                    MeetingsCount = userActs.Count(a => a.ActivityType == "Meeting"),
                    EmailsCount = userActs.Count(a => a.ActivityType == "Email"),
                    TasksCount = userActs.Count(a => a.ActivityType == "Task"),
                    FollowUpsPlanned = userFollows.Count(f => f.Status == "Planned"),
                    FollowUpsCompleted = userFollows.Count(f => f.Status == "Completed"),
                    OpportunitiesCreated = userOpps.Count
                });
            }

            return report.OrderByDescending(r => r.FollowUpsCompleted + r.CallsCount).ToList();
        }

        public async Task<PaginatedList<AuditLog>> GetAuditReportAsync(UserScope scope, string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page, int pageSize)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!scope.IsAdmin)
            {
                // Manager only sees team or non-security audit logs
                if (scope.IsManager)
                {
                    query = query.Where(a => a.EntityName != "Security" && a.EntityName != "User");
                }
                else
                {
                    // Sales Executive not permitted to view audit logs
                    return new PaginatedList<AuditLog>(new List<AuditLog>(), 0, page, pageSize);
                }
            }

            if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
            if (startDate.HasValue) query = query.Where(a => a.CreatedDate >= startDate.Value);
            if (endDate.HasValue) query = query.Where(a => a.CreatedDate <= endDate.Value.AddDays(1).AddTicks(-1));

            query = query.OrderByDescending(a => a.CreatedDate);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<AuditLog>(items, total, page, pageSize);
        }
    }
}
