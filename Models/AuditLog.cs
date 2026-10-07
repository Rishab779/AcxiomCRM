using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(150)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, Convert, Complete, Reschedule, Login, FailedLogin, Lockout, Unlock

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty; // Customer, Lead, Opportunity, FollowUp, Activity, User, Security

        [MaxLength(100)]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; } // Never store passwords or tokens

        public string? NewValue { get; set; } // Never store passwords or tokens

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? IpAddress { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }
    }
}
