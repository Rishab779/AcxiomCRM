using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }

        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Demo

        [Required]
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public DateTime? CompletedDate { get; set; }

        [MaxLength(1000)]
        public string? OutcomeNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Assignment & Ownership
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }
    }
}
