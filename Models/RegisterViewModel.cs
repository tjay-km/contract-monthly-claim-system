using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class RegisterViewModel
    {
        public RegisterViewModel()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Email = string.Empty;
            UserName = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            Department = string.Empty;
        }

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        [Required]
        [Display(Name = "Department")]
        public string Department { get; set; }
    }
}
