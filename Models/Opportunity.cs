using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000000)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

        [Range(0, 100)]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedDate { get; set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; set; }

        // Assignment & Ownership
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [NotMapped]
        public decimal WeightedAmount => Amount * Probability / 100m;

        // Navigations
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
