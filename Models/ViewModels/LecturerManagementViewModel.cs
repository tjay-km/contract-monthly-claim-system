using System.ComponentModel.DataAnnotations;

namespace CMCS.Models.ViewModels
{
    public class LecturerManagementViewModel
    {
        public LecturerManagementViewModel()
        {
            Id = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            PhoneNumber = string.Empty;
            Department = string.Empty;
            Specialization = string.Empty;
            IsActive = true;
            TotalClaims = 0;
            TotalHours = 0;
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

        // Additional properties for display
        public string FullName => $"{FirstName} {LastName}".Trim();
        public int TotalClaims { get; set; }
        public decimal TotalHours { get; set; }
    }
}
