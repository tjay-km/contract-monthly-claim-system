using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace CMCS.Models.ViewModels
{
    public class LecturerManagementViewModel
    {
        public LecturerManagementViewModel()
        {
            Lecturers = new List<ApplicationUser>();
            NewLecturer = new ApplicationUser();
            Departments = new List<Department>();
            SelectedDepartmentId = string.Empty;
            SearchString = string.Empty;
            Id = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
            Department = string.Empty;
        }

        public string Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid Phone Number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [StringLength(100)]
        public string Department { get; set; }

        [StringLength(100)]
        public string Specialization { get; set; }

        public bool IsActive { get; set; }

        public List<ApplicationUser> Lecturers { get; set; }
        public ApplicationUser NewLecturer { get; set; }
        public List<Department> Departments { get; set; }
        public string SelectedDepartmentId { get; set; }
        public string SearchString { get; set; }

        // Additional properties for display
        public string FullName => $"{FirstName} {LastName}".Trim();
        public int TotalClaims { get; set; }
        public decimal TotalHours { get; set; }
    }
}
