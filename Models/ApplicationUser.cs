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
        public ApplicationUser()
        {
            Name = string.Empty;
            Role = string.Empty;
        }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Role { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? Specialization { get; set; }

        public UserType UserType { get; set; }

        public bool IsActive { get; set; } = true;

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
