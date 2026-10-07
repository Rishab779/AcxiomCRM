using System;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        public string? AssignedTo { get; set; }
    }

    public class UpdateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        public string Status { get; set; } = "Active";

        public string? AssignedTo { get; set; }
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
        public int? ConvertedCustomerId { get; set; }
    }

    public class CreateLeadDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [MaxLength(150)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; }

        [MaxLength(100)]
        public string Source { get; set; } = "Website";

        [MaxLength(50)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000, ErrorMessage = "Expected Value must be greater than or equal to 0.")]
        public decimal ExpectedValue { get; set; } = 0;

        public string? AssignedTo { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedAmount { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedToName { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Valid Customer must be selected.")]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Active Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Stage is required.")]
        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }
}
