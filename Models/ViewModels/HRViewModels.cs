using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CMCS.Models
{
    public class EditLecturerViewModel
    {
        public EditLecturerViewModel()
        {
            Id = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Department = string.Empty;
            Specialization = string.Empty;
        }

        [Required]
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

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

        [Required]
        [Display(Name = "Specialization")]
        [StringLength(100)]
        public string Specialization { get; set; }
    }
}
