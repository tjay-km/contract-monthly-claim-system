using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Role { get; set; }
    }
}
