using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeadCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Campaign, Trade Show

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = "Medium"; // Low, Medium, High

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 1000000000)]
        public decimal ExpectedValue { get; set; } = 0;

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

        // Lead Conversion linkage
        public int? ConvertedCustomerId { get; set; }

        [ForeignKey("ConvertedCustomerId")]
        public virtual Customer? ConvertedCustomer { get; set; }

        public int? ConvertedOpportunityId { get; set; }

        public DateTime? ConvertedDate { get; set; }

        // Navigations
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
