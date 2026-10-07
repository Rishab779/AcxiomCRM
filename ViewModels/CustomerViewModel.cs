using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.ViewModels
{
    public class CustomerListViewModel
    {
        public PaginatedList<Customer> Customers { get; set; } = null!;
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public string? AssignedToFilter { get; set; }
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }
        public List<SelectListItem> UsersList { get; set; } = new();
    }

    public class CustomerEditViewModel
    {
        public int CustomerId { get; set; }

        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(150, ErrorMessage = "Customer Name cannot exceed 150 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

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

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        public List<SelectListItem> UsersList { get; set; } = new();
    }
}
