using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels
{
    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public string? ManagerName { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();
        public string? Search { get; set; }
        public string? RoleFilter { get; set; }
        public string? StatusFilter { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Sales Executive";

        [Display(Name = "Reporting Manager")]
        public string? ManagerId { get; set; }

        public List<SelectListItem> RolesList { get; set; } = new();
        public List<SelectListItem> ManagersList { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Sales Executive";

        [Display(Name = "Reporting Manager")]
        public string? ManagerId { get; set; }

        [Display(Name = "Active Account")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is Locked Out")]
        public bool IsLockedOut { get; set; }

        public List<SelectListItem> RolesList { get; set; } = new();
        public List<SelectListItem> ManagersList { get; set; } = new();
    }

    public class UserResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
