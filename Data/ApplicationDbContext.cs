using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer indexes and constraints
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.AssignedTo);
                entity.HasIndex(c => c.CustomerCode).IsUnique();

                entity.HasOne(c => c.AssignedUser)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Lead indexes and constraints
            builder.Entity<Lead>(entity =>
            {
                entity.HasIndex(l => l.Email);
                entity.HasIndex(l => l.Phone);
                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => l.AssignedTo);
                entity.HasIndex(l => l.LeadCode).IsUnique();

                entity.HasOne(l => l.AssignedUser)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(l => l.ConvertedCustomer)
                    .WithMany()
                    .HasForeignKey(l => l.ConvertedCustomerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Opportunity indexes and constraints
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasIndex(o => o.CustomerId);
                entity.HasIndex(o => o.Stage);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.AssignedTo);
                entity.HasIndex(o => o.ExpectedCloseDate);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany()
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.AssignedUser)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // FollowUp indexes and constraints
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasIndex(f => f.FollowUpDate);
                entity.HasIndex(f => f.Status);
                entity.HasIndex(f => f.AssignedTo);
                entity.HasIndex(f => f.CustomerId);
                entity.HasIndex(f => f.LeadId);

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Opportunity)
                    .WithMany(o => o.FollowUps)
                    .HasForeignKey(f => f.OpportunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.AssignedUser)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Activity indexes and constraints
            builder.Entity<Activity>(entity =>
            {
                entity.HasIndex(a => a.ActivityDate);
                entity.HasIndex(a => a.ActivityType);
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => a.AssignedTo);

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Opportunity)
                    .WithMany(o => o.Activities)
                    .HasForeignKey(a => a.OpportunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AssignedUser)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // AuditLog indexes
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.Action);
            });

            // User Manager Hierarchy
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(u => u.Manager)
                    .WithMany(m => m.DirectReports)
                    .HasForeignKey(u => u.ManagerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
