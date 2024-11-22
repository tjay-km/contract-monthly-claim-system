using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class EditLecturerViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        public string Department { get; set; }
    }
}
