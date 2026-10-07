using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels
{
    public class ActivityListViewModel
    {
        public PaginatedList<Activity> Activities { get; set; } = null!;
        public string? Search { get; set; }
        public string? TypeFilter { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class ActivityEditViewModel
    {
        public int ActivityId { get; set; }

        [Required(ErrorMessage = "Activity type is required.")]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity")]
        public int? OpportunityId { get; set; }

        [Required]
        public string Status { get; set; } = "Completed";

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> LeadsList { get; set; } = new();
        public List<SelectListItem> OpportunitiesList { get; set; } = new();
        public List<SelectListItem> UsersList { get; set; } = new();
    }
}
