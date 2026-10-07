using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityListViewModel
    {
        public PaginatedList<Opportunity> Opportunities { get; set; } = null!;
        public string? Search { get; set; }
        public string? StageFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class OpportunityEditViewModel
    {
        public int OpportunityId { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [MaxLength(150)]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid customer.")]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount ($)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Win Probability (%)")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddMonths(1);

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> UsersList { get; set; } = new();
    }
}
