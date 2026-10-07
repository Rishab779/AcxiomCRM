using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpListViewModel
    {
        public PaginatedList<FollowUp> FollowUps { get; set; } = null!;
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? TimeframeFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class FollowUpEditViewModel
    {
        public int FollowUpId { get; set; }

        [Display(Name = "Related Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Related Lead")]
        public int? LeadId { get; set; }

        [Display(Name = "Related Opportunity")]
        public int? OpportunityId { get; set; }

        [Required(ErrorMessage = "Follow-up date and time are required.")]
        [Display(Name = "Follow-Up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [Required]
        [Display(Name = "Follow-Up Type")]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Remarks are required.")]
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        public string Status { get; set; } = "Planned";

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public List<SelectListItem> CustomersList { get; set; } = new();
        public List<SelectListItem> LeadsList { get; set; } = new();
        public List<SelectListItem> OpportunitiesList { get; set; } = new();
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class RescheduleFollowUpViewModel
    {
        public int FollowUpId { get; set; }

        [Required(ErrorMessage = "New date is required.")]
        [Display(Name = "New Follow-Up Date")]
        public DateTime NewFollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

        [MaxLength(500)]
        [Display(Name = "Reason for Rescheduling")]
        public string? Remarks { get; set; }
    }

    public class CompleteFollowUpViewModel
    {
        public int FollowUpId { get; set; }

        [Required(ErrorMessage = "Please provide outcome notes or discussion summary.")]
        [MaxLength(1000)]
        [Display(Name = "Outcome & Notes")]
        public string OutcomeNotes { get; set; } = string.Empty;
    }
}
