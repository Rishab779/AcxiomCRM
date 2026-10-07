using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Team hierarchy support for Managers
        public string? ManagerId { get; set; }
        public virtual ApplicationUser? Manager { get; set; }
        public virtual ICollection<ApplicationUser> DirectReports { get; set; } = new List<ApplicationUser>();

        // Navigation collections
        public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
        public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
    }
}
