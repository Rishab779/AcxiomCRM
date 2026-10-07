using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            var roles = new[] { "Admin", "Manager", "Sales Executive" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Users
            ApplicationUser adminUser;
            var adminEmail = "admin@acxiomcrm.local";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    Department = "Executive / IT",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-6)
                };
                await userManager.CreateAsync(adminUser, "Admin@12345");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                adminUser = existingAdmin;
            }

            ApplicationUser managerUser;
            var managerEmail = "manager@acxiomcrm.local";
            var existingManager = await userManager.FindByEmailAsync(managerEmail);
            if (existingManager == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = managerEmail,
                    Email = managerEmail,
                    FullName = "Sarah Connor (Sales Manager)",
                    Department = "Sales Leadership",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow.AddMonths(-5)
                };
                await userManager.CreateAsync(managerUser, "Manager@12345");
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }
            else
            {
                managerUser = existingManager;
            }

            ApplicationUser sales1User;
            var sales1Email = "sales@acxiomcrm.local";
            var existingSales1 = await userManager.FindByEmailAsync(sales1Email);
            if (existingSales1 == null)
            {
                sales1User = new ApplicationUser
                {
                    UserName = sales1Email,
                    Email = sales1Email,
                    FullName = "Alex Miller (Sales Exec)",
                    Department = "Enterprise Sales",
                    EmailConfirmed = true,
                    IsActive = true,
                    ManagerId = managerUser.Id,
                    CreatedDate = DateTime.UtcNow.AddMonths(-4)
                };
                await userManager.CreateAsync(sales1User, "Sales@12345");
                await userManager.AddToRoleAsync(sales1User, "Sales Executive");
            }
            else
            {
                sales1User = existingSales1;
            }

            ApplicationUser sales2User;
            var sales2Email = "sales2@acxiomcrm.local";
            var existingSales2 = await userManager.FindByEmailAsync(sales2Email);
            if (existingSales2 == null)
            {
                sales2User = new ApplicationUser
                {
                    UserName = sales2Email,
                    Email = sales2Email,
                    FullName = "Jordan Lee (Sales Exec)",
                    Department = "Commercial Accounts",
                    EmailConfirmed = true,
                    IsActive = true,
                    ManagerId = managerUser.Id,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                };
                await userManager.CreateAsync(sales2User, "Sales@12345");
                await userManager.AddToRoleAsync(sales2User, "Sales Executive");
            }
            else
            {
                sales2User = existingSales2;
            }

            // 3. Seed Customers
            if (!await context.Customers.AnyAsync())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        CustomerCode = "CUST-1001",
                        CustomerName = "Nexus Global Technologies",
                        Email = "contact@nexusglobal.com",
                        Phone = "+1-555-010-4491",
                        CompanyName = "Nexus Global",
                        Address = "100 Innovation Way, Suite 400",
                        City = "San Jose",
                        State = "CA",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-3),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1002",
                        CustomerName = "Apex Logistics International",
                        Email = "procurement@apexlogistics.com",
                        Phone = "+1-555-019-8234",
                        CompanyName = "Apex Logistics",
                        Address = "450 Freight Harbor Blvd",
                        City = "Chicago",
                        State = "IL",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1003",
                        CustomerName = "BioHealth Diagnostics",
                        Email = "info@biohealthdiag.org",
                        Phone = "+1-555-014-9921",
                        CompanyName = "BioHealth Corp",
                        Address = "72 Biotech Parkway",
                        City = "Boston",
                        State = "MA",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1004",
                        CustomerName = "Vertex Financial Partners",
                        Email = "ops@vertexfp.com",
                        Phone = "+1-555-012-3847",
                        CompanyName = "Vertex Financial",
                        Address = "88 Wall Street, 12th Floor",
                        City = "New York",
                        State = "NY",
                        Status = "Active",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedBy = "System",
                        AssignedTo = managerUser.Id
                    },
                    new Customer
                    {
                        CustomerCode = "CUST-1005",
                        CustomerName = "Summit Retail Dynamics",
                        Email = "sales@summitretail.com",
                        Phone = "+1-555-017-6632",
                        CompanyName = "Summit Retail",
                        Address = "300 Commerce Way",
                        City = "Dallas",
                        State = "TX",
                        Status = "Inactive",
                        CreatedDate = DateTime.UtcNow.AddMonths(-4),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    }
                };

                context.Customers.AddRange(customers);
                await context.SaveChangesAsync();
            }

            var cust1 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1001");
            var cust2 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1002");
            var cust3 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1003");
            var cust4 = await context.Customers.FirstAsync(c => c.CustomerCode == "CUST-1004");

            // 4. Seed Leads
            if (!await context.Leads.AnyAsync())
            {
                var leads = new List<Lead>
                {
                    new Lead
                    {
                        LeadCode = "LEAD-1001",
                        LeadName = "David Sterling",
                        Email = "david.sterling@cloudwave.io",
                        Phone = "+1-555-018-9102",
                        CompanyName = "CloudWave Systems",
                        Source = "Website",
                        Status = "Qualified",
                        Priority = "High",
                        ExpectedValue = 45000,
                        CreatedDate = DateTime.UtcNow.AddDays(-18),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1002",
                        LeadName = "Emily Watson",
                        Email = "e.watson@horizonmed.com",
                        Phone = "+1-555-013-5541",
                        CompanyName = "Horizon Medical Supplies",
                        Source = "Referral",
                        Status = "Contacted",
                        Priority = "Medium",
                        ExpectedValue = 28000,
                        CreatedDate = DateTime.UtcNow.AddDays(-12),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1003",
                        LeadName = "Marcus Vance",
                        Email = "mvance@vanceindustrial.com",
                        Phone = "+1-555-011-8899",
                        CompanyName = "Vance Industrial Automation",
                        Source = "Campaign",
                        Status = "New",
                        Priority = "High",
                        ExpectedValue = 85000,
                        CreatedDate = DateTime.UtcNow.AddDays(-5),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1004",
                        LeadName = "Claire Bennett",
                        Email = "claire@greenfuturepower.org",
                        Phone = "+1-555-016-2234",
                        CompanyName = "GreenFuture Energy",
                        Source = "Trade Show",
                        Status = "Lost",
                        Priority = "Low",
                        ExpectedValue = 15000,
                        CreatedDate = DateTime.UtcNow.AddDays(-30),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    },
                    new Lead
                    {
                        LeadCode = "LEAD-1005",
                        LeadName = "Robert Chen",
                        Email = "robert@innovativedesign.com",
                        Phone = "+1-555-015-7788",
                        CompanyName = "Innovative Design Lab",
                        Source = "Website",
                        Status = "Converted",
                        Priority = "High",
                        ExpectedValue = 60000,
                        CreatedDate = DateTime.UtcNow.AddDays(-40),
                        ConvertedCustomerId = cust1.CustomerId,
                        ConvertedDate = DateTime.UtcNow.AddDays(-20),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    }
                };

                context.Leads.AddRange(leads);
                await context.SaveChangesAsync();
            }

            // 5. Seed Opportunities
            if (!await context.Opportunities.AnyAsync())
            {
                var opps = new List<Opportunity>
                {
                    new Opportunity
                    {
                        OpportunityName = "Nexus Cloud Migration Enterprise License",
                        CustomerId = cust1.CustomerId,
                        Amount = 65000,
                        Stage = "Negotiation",
                        Probability = 75,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(20),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddMonths(-1),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id,
                        Notes = "Finalizing terms on SLA and enterprise user tiers."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Apex Fleet Tracking Software Rollout",
                        CustomerId = cust2.CustomerId,
                        Amount = 42000,
                        Stage = "Proposal",
                        Probability = 50,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(35),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-25),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id,
                        Notes = "Proposal submitted to Procurement director."
                    },
                    new Opportunity
                    {
                        OpportunityName = "BioHealth LIMS Integration Platform",
                        CustomerId = cust3.CustomerId,
                        Amount = 95000,
                        Stage = "Won",
                        Probability = 100,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-10),
                        Status = "Won",
                        CreatedDate = DateTime.UtcNow.AddMonths(-2),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id,
                        Notes = "Contract signed. Onboarding scheduled."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Vertex Compliance Audit Module",
                        CustomerId = cust4.CustomerId,
                        Amount = 30000,
                        Stage = "Qualification",
                        Probability = 25,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow.AddDays(-8),
                        CreatedBy = "System",
                        AssignedTo = managerUser.Id,
                        Notes = "Initial requirements gathering with IT security team."
                    },
                    new Opportunity
                    {
                        OpportunityName = "Summit POS Integration Package",
                        CustomerId = cust1.CustomerId,
                        Amount = 18000,
                        Stage = "Lost",
                        Probability = 0,
                        ExpectedCloseDate = DateTime.UtcNow.AddDays(-15),
                        Status = "Lost",
                        CreatedDate = DateTime.UtcNow.AddDays(-50),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id,
                        Notes = "Competitor offered discount on existing hardware."
                    }
                };

                context.Opportunities.AddRange(opps);
                await context.SaveChangesAsync();
            }

            var opp1 = await context.Opportunities.FirstAsync(o => o.Stage == "Negotiation");
            var opp2 = await context.Opportunities.FirstAsync(o => o.Stage == "Proposal");

            // 6. Seed Follow-ups
            if (!await context.FollowUps.AnyAsync())
            {
                var followUps = new List<FollowUp>
                {
                    new FollowUp
                    {
                        CustomerId = cust1.CustomerId,
                        OpportunityId = opp1.OpportunityId,
                        FollowUpDate = DateTime.UtcNow.AddDays(2),
                        FollowUpType = "Meeting",
                        Remarks = "Contract negotiation review with CTO and Legal counsel.",
                        Status = "Planned",
                        CreatedDate = DateTime.UtcNow.AddDays(-3),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new FollowUp
                    {
                        CustomerId = cust2.CustomerId,
                        OpportunityId = opp2.OpportunityId,
                        FollowUpDate = DateTime.UtcNow.Date.AddHours(14), // Due today
                        FollowUpType = "Call",
                        Remarks = "Discuss questions on proposal pricing schedule.",
                        Status = "Planned",
                        CreatedDate = DateTime.UtcNow.AddDays(-4),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new FollowUp
                    {
                        CustomerId = cust3.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(-2),
                        FollowUpType = "Call",
                        Remarks = "Check in on customer onboarding satisfaction.",
                        Status = "Completed",
                        CompletedDate = DateTime.UtcNow.AddDays(-2),
                        OutcomeNotes = "Client confirmed onboarding is progressing smoothly.",
                        CreatedDate = DateTime.UtcNow.AddDays(-6),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    },
                    new FollowUp
                    {
                        CustomerId = cust4.CustomerId,
                        FollowUpDate = DateTime.UtcNow.AddDays(4),
                        FollowUpType = "Meeting",
                        Remarks = "Architecture overview demo for compliance team.",
                        Status = "Planned",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        CreatedBy = "System",
                        AssignedTo = managerUser.Id
                    }
                };

                context.FollowUps.AddRange(followUps);
                await context.SaveChangesAsync();
            }

            // 7. Seed Activities
            if (!await context.Activities.AnyAsync())
            {
                var activities = new List<Activity>
                {
                    new Activity
                    {
                        ActivityType = "Meeting",
                        Subject = "Executive Discovery Presentation",
                        Description = "Walked through system architecture and core sales value proposition with VP of Engineering.",
                        ActivityDate = DateTime.UtcNow.AddDays(-7),
                        CustomerId = cust1.CustomerId,
                        OpportunityId = opp1.OpportunityId,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-7),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Activity
                    {
                        ActivityType = "Email",
                        Subject = "Sent Formal Commercial Proposal",
                        Description = "Delivered PDF quotation with 3-year term pricing options.",
                        ActivityDate = DateTime.UtcNow.AddDays(-4),
                        CustomerId = cust2.CustomerId,
                        OpportunityId = opp2.OpportunityId,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-4),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    },
                    new Activity
                    {
                        ActivityType = "Call",
                        Subject = "Technical Clarification Call",
                        Description = "Addressed cloud security, ISO certification, and compliance questions.",
                        ActivityDate = DateTime.UtcNow.AddDays(-2),
                        CustomerId = cust3.CustomerId,
                        Status = "Completed",
                        CreatedDate = DateTime.UtcNow.AddDays(-2),
                        CreatedBy = "System",
                        AssignedTo = sales2User.Id
                    },
                    new Activity
                    {
                        ActivityType = "Task",
                        Subject = "Draft MSA Custom Terms",
                        Description = "Prepare custom confidentiality and uptime clauses.",
                        ActivityDate = DateTime.UtcNow.AddDays(1),
                        CustomerId = cust1.CustomerId,
                        Status = "Planned",
                        CreatedDate = DateTime.UtcNow.AddDays(-1),
                        CreatedBy = "System",
                        AssignedTo = sales1User.Id
                    }
                };

                context.Activities.AddRange(activities);
                await context.SaveChangesAsync();
            }

            // 8. Seed Initial Audit Logs
            if (!await context.AuditLogs.AnyAsync())
            {
                var auditLogs = new List<AuditLog>
                {
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Action = "SystemInitialize",
                        EntityName = "Security",
                        RecordId = "0",
                        Details = "AcxiomCRM database initialized and default roles established.",
                        CreatedDate = DateTime.UtcNow.AddMonths(-6)
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Action = "UserCreate",
                        EntityName = "User",
                        RecordId = managerUser.Id,
                        Details = $"User '{managerUser.Email}' assigned to role 'Manager'.",
                        CreatedDate = DateTime.UtcNow.AddMonths(-5)
                    },
                    new AuditLog
                    {
                        UserId = adminUser.Id,
                        UserName = adminUser.UserName,
                        Action = "UserCreate",
                        EntityName = "User",
                        RecordId = sales1User.Id,
                        Details = $"User '{sales1User.Email}' assigned to role 'Sales Executive'.",
                        CreatedDate = DateTime.UtcNow.AddMonths(-4)
                    }
                };

                context.AuditLogs.AddRange(auditLogs);
                await context.SaveChangesAsync();
            }
        }
    }
}
