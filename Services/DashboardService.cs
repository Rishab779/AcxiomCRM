using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services
{
    public class DashboardViewModel
    {
        // Date filter selection
        public string DateFilter { get; set; } = "month"; // today, week, month, custom
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }

        // Role title for header
        public string RoleBadge { get; set; } = string.Empty;

        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }

        // Chart 1: Lead Status Breakdown
        public Dictionary<string, int> LeadStatusCounts { get; set; } = new();

        // Chart 2: Opportunity Pipeline Stage Breakdown
        public Dictionary<string, decimal> PipelineStageValues { get; set; } = new();
        public Dictionary<string, int> PipelineStageCounts { get; set; } = new();

        // Chart 3: Monthly Sales / Won deals
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlyWonValues { get; set; } = new();
        public List<decimal> MonthlyPipelineValues { get; set; } = new();

        // Feed lists
        public List<FollowUp> UpcomingFollowUps { get; set; } = new();
        public List<FollowUp> OverdueFollowUps { get; set; } = new();
        public List<Activity> RecentActivities { get; set; } = new();
    }

    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(UserScope scope, string? dateFilter, DateTime? customStartDate, DateTime? customEndDate);
    }

    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(UserScope scope, string? dateFilter, DateTime? customStartDate, DateTime? customEndDate)
        {
            var filter = string.IsNullOrWhiteSpace(dateFilter) ? "month" : dateFilter.ToLower();

            // Calculate date bounds
            DateTime? startDate = null;
            DateTime? endDate = null;
            var now = DateTime.UtcNow;

            switch (filter)
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    break;
                case "week":
                    // Start of week (Monday)
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = now.Date.AddDays(-1 * diff);
                    endDate = startDate.Value.AddDays(7).AddTicks(-1);
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "custom":
                    if (customStartDate.HasValue)
                        startDate = DateTime.SpecifyKind(customStartDate.Value.Date, DateTimeKind.Utc);
                    if (customEndDate.HasValue)
                        endDate = DateTime.SpecifyKind(customEndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                    break;
            }

            var vm = new DashboardViewModel
            {
                DateFilter = filter,
                CustomStartDate = customStartDate,
                CustomEndDate = customEndDate,
                RoleBadge = scope.IsAdmin ? "System Administrator (Global View)" :
                            scope.IsManager ? "Sales Manager (Team View)" : "Sales Executive (My Records)"
            };

            // Query Scoped Entities
            var customersQuery = _context.Customers.AsNoTracking();
            var leadsQuery = _context.Leads.AsNoTracking();
            var oppsQuery = _context.Opportunities.AsNoTracking();
            var followUpsQuery = _context.FollowUps.AsNoTracking();
            var activitiesQuery = _context.Activities.AsNoTracking();

            if (!scope.IsAdmin)
            {
                if (scope.IsManager)
                {
                    customersQuery = customersQuery.Where(c => c.AssignedTo == null || c.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(c.AssignedTo));
                    leadsQuery = leadsQuery.Where(l => l.AssignedTo == null || l.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(l.AssignedTo));
                    oppsQuery = oppsQuery.Where(o => o.AssignedTo == null || o.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(o.AssignedTo));
                    followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == null || f.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(f.AssignedTo));
                    activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == null || a.AssignedTo == scope.UserId || scope.ManagedUserIds.Contains(a.AssignedTo));
                }
                else
                {
                    // Sales Executive
                    customersQuery = customersQuery.Where(c => c.AssignedTo == scope.UserId || c.CreatedBy == scope.UserName);
                    leadsQuery = leadsQuery.Where(l => l.AssignedTo == scope.UserId || l.CreatedBy == scope.UserName);
                    oppsQuery = oppsQuery.Where(o => o.AssignedTo == scope.UserId || o.CreatedBy == scope.UserName);
                    followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == scope.UserId || f.CreatedBy == scope.UserName);
                    activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == scope.UserId || a.CreatedBy == scope.UserName);
                }
            }

            // Apply date filters where applicable
            if (startDate.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate >= startDate.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startDate.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                customersQuery = customersQuery.Where(c => c.CreatedDate <= endDate.Value);
                leadsQuery = leadsQuery.Where(l => l.CreatedDate <= endDate.Value);
                oppsQuery = oppsQuery.Where(o => o.CreatedDate <= endDate.Value);
            }

            // Materialize metrics
            vm.TotalCustomers = await customersQuery.CountAsync();
            vm.TotalLeads = await leadsQuery.CountAsync();
            vm.OpenLeads = await leadsQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified");

            var allOpps = await oppsQuery.ToListAsync();
            vm.TotalOpportunities = allOpps.Count;
            vm.OpenOpportunities = allOpps.Count(o => o.Status == "Open");
            vm.WonOpportunities = allOpps.Count(o => o.Status == "Won");
            vm.LostOpportunities = allOpps.Count(o => o.Status == "Lost");
            vm.TotalPipelineValue = allOpps.Where(o => o.Status == "Open").Sum(o => o.Amount);
            vm.WeightedPipelineValue = allOpps.Where(o => o.Status == "Open").Sum(o => o.WeightedAmount);

            // Chart 1: Lead Status Breakdown
            var leadStatuses = new[] { "New", "Contacted", "Qualified", "Converted", "Lost" };
            var leadsList = await leadsQuery.ToListAsync();
            foreach (var st in leadStatuses)
            {
                vm.LeadStatusCounts[st] = leadsList.Count(l => l.Status == st);
            }

            // Chart 2: Pipeline Stages
            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            foreach (var st in stages)
            {
                var stageOpps = allOpps.Where(o => o.Stage == st).ToList();
                vm.PipelineStageValues[st] = stageOpps.Sum(o => o.Amount);
                vm.PipelineStageCounts[st] = stageOpps.Count;
            }

            // Chart 3: Monthly Sales (Last 6 Months)
            var currentMonth = new DateTime(now.Year, now.Month, 1);
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = currentMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var label = monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture);

                var wonSum = allOpps
                    .Where(o => o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Sum(o => o.Amount);

                var pipeSum = allOpps
                    .Where(o => o.Status == "Open" && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Sum(o => o.Amount);

                vm.MonthlyLabels.Add(label);
                vm.MonthlyWonValues.Add(wonSum);
                vm.MonthlyPipelineValues.Add(pipeSum);
            }

            // Feeds
            vm.UpcomingFollowUps = await followUpsQuery
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= now)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.OverdueFollowUps = await followUpsQuery
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Where(f => f.Status == "Planned" && f.FollowUpDate < now)
                .OrderBy(f => f.FollowUpDate)
                .Take(5)
                .ToListAsync();

            vm.RecentActivities = await activitiesQuery
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();

            return vm;
        }
    }
}
