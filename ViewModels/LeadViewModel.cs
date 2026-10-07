using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels
{
    public class LeadListViewModel
    {
        public PaginatedList<Lead> Leads { get; set; } = null!;
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? SourceFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class LeadEditViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [MaxLength(150)]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Source { get; set; } = "Website";

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "New";

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000, ErrorMessage = "Expected Value must be greater than or equal to 0.")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; } = 0;

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;

        // Customer Options
        [Display(Name = "Create a New Customer")]
        public bool CreateNewCustomer { get; set; } = true;

        [Display(Name = "Select Existing Customer")]
        public int? ExistingCustomerId { get; set; }

        [MaxLength(150)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        // Opportunity Options
        [Display(Name = "Create Initial Opportunity")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount ($)")]
        [Range(0.01, 1000000000, ErrorMessage = "Active opportunity amount must be greater than 0.")]
        public decimal OpportunityAmount { get; set; } = 5000;

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int OpportunityProbability { get; set; } = 20;

        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        public string Stage { get; set; } = "Qualification";

        public List<SelectListItem> ExistingCustomersList { get; set; } = new();
    }
}
