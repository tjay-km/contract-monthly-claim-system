using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public enum UserType
    {
        Admin,
        HR,
        Lecturer,
        HOD
    }

    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base()
        {
            FirstName = string.Empty;
            LastName = string.Empty;
            Department = string.Empty;
            Specialization = string.Empty;
            Name = string.Empty;
            Role = string.Empty;
            UserType = UserType.Lecturer;
            IsActive = true;
            UserName = string.Empty; // Ensure UserName is initialized
            Email = string.Empty; // Ensure Email is initialized
        }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Role { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string Department { get; set; }

        [StringLength(100)]
        public string Specialization { get; set; }

        public UserType UserType { get; set; }

        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public virtual ICollection<ClaimReport> ReviewedClaims { get; set; } = new List<ClaimReport>();

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
